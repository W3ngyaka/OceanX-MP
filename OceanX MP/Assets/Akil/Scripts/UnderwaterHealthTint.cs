using UnityEngine;
using OceanX.BoidsGPU.Ecosystem;
using StylizedWater3.UnderwaterRendering;

/// <summary>
/// Drives the underwater look from ecosystem health: brown and murky at 0, back to the scene's
/// authored "healthy" look by <see cref="healthyAt"/> (default 85%).
///
/// Reads <see cref="EcosystemSimulationGPU.EcoHealth01"/> DIRECTLY (host-side, no netcode), the same
/// way <c>EnvironmentHealthReveal</c> does.
///
/// HOW THE UNDERWATER LOOK IS BUILT (Stylized Water 3), because it dictates the design here:
///  • The fog FLOATS (density, start distance, brightness, absorption, caustics) live on
///    UnderwaterArea.shadingSettings and are pushed to shader globals every frame by the render
///    feature. Writing them at runtime is free — nothing is persisted.
///  • The fog COLOUR is NOT a global. It is the water material's _BaseColor (see UnderwaterFog.hlsl:
///    fog rgb = deep.rgb * brightness), and the render feature re-copies _BaseColor/_ShallowColor
///    from `area.waterMaterial` into its own render material EVERY frame. So the only way to tint
///    the fog is through `area.waterMaterial` itself.
///
/// That material is a SHARED PROJECT ASSET (and lives in the vendor folder). Writing to it would
/// permanently modify the .mat on disk. So on enable we CLONE it, point both the UnderwaterArea and
/// the water surface renderer at the clone, and only ever tint the clone. The asset is never touched;
/// OnDisable restores the original reference and destroys the clone.
///
/// ── PLAY MODE ONLY. DO NOT ADD [ExecuteAlways]. ────────────────────────────────────────────────────
/// An edit-mode preview was tried and reverted, because it corrupted the scene. `waterMaterial` is a
/// SERIALIZED field and the clone is HideAndDontSave, so any edit-mode serialization of the scene while
/// the clone is installed writes a NULL material reference and bakes the murky shadingSettings in.
///
/// Guarding scene saves is not sufficient. Entering play mode serialises the edit-mode scene through a
/// SEPARATE path, and during the ExitingEditMode callback `Application.isPlaying` is still false — so an
/// editor-tick driven preview re-installs the clone immediately after any uninstall, right before Unity
/// takes that snapshot. Exiting play then restores the corrupted snapshot over the real scene.
///
/// If a preview is ever revisited, it must survive BOTH serialization paths (scene save AND the play-mode
/// snapshot), and must latch itself off for the whole edit→play transition rather than relying on
/// `Application.isPlaying`. Use `debugOverride` + the slider in Play instead; it costs nothing.
/// ───────────────────────────────────────────────────────────────────────────────────────────────────
/// </summary>
[DisallowMultipleComponent]
public class UnderwaterHealthTint : MonoBehaviour
{
    [Header("Source (auto-found if left empty)")]
    [SerializeField] private UnderwaterArea area;
    [Tooltip("Renderer using the water material. Repointed at the runtime clone so the surface matches the fog.")]
    [SerializeField] private Renderer waterSurface;
    [SerializeField] private EcosystemSimulationGPU sim;

    [Header("Master toggle")]
    [Tooltip("ON  = water starts brown/murky and clears as health climbs.\n" +
             "OFF = effect disabled; water left exactly as authored.")]
    [SerializeField] private bool effectEnabled = true;

    [Header("Response")]
    [Tooltip("Health at which the water reaches its FULL healthy look. At/above this, nothing more changes.")]
    [Range(0.05f, 1f)][SerializeField] private float healthyAt = 0.85f;
    [Tooltip("Shapes the murky→healthy blend across the 0..healthyAt band.\n" +
             "Default is ease-in (~t²): holds the murk through the low-health range, then clears quickly near " +
             "the top. Straight linear clears the brown by ~40% health, because blending in linear colour space " +
             "lets the healthy blue dominate early.")]
    [SerializeField] private AnimationCurve response =
        new AnimationCurve(new Keyframe(0f, 0f, 0f, 0f), new Keyframe(1f, 1f, 2f, 2f));
    [Tooltip("How fast the internal health value eases toward the live value.")]
    [SerializeField] private float smoothSpeed = 4f;

    [Header("Murky endpoint (health = 0)")]
    [Tooltip("Deep colour — THIS is the underwater fog colour. The single biggest lever on the murk.\n" +
             "Keep it fairly BRIGHT: silty water is muddy-tea coloured. Going dark here reads as night, not dirt.")]
    [ColorUsage(false)][SerializeField] private Color murkyBaseColor = new Color(0.30f, 0.21f, 0.11f);
    [Tooltip("Shallow/surface colour. Silty brown.")]
    [ColorUsage(false)][SerializeField] private Color murkyShallowColor = new Color(0.45f, 0.34f, 0.18f);
    [Range(0.01f, 3f)][SerializeField] private float murkyFogDensity = 0.75f;
    [Tooltip("Metres before fog starts. Dropping this is what sells 'can't see your hand in front of you'.")]
    [Min(0f)][SerializeField] private float murkyFogStartDistance = 4f;
    [Range(0f, 2f)][SerializeField] private float murkyFogBrightness = 1.0f;
    [Range(0.01f, 1f)][SerializeField] private float murkyColorAbsorption = 0.25f;
    [Tooltip("Dim the dappled light on the seabed — dirty water lets less through.")]
    [Min(0f)][SerializeField] private float murkyCausticsBrightness = 0.25f;

    [Header("Debug (Play mode)")]
    [Tooltip("Ignore the live sim and drive the tint from the slider below. Press Play, then drag it to " +
             "scrub murky→healthy and tune the values above.\n\n" +
             "Remember to turn this OFF when you want the sim to drive the tint.")]
    [SerializeField] private bool debugOverride = false;
    [Range(0f, 1f)][SerializeField] private float debugHealth = 0f;
    [SerializeField] private bool logDebug = false;

    private const string CloneSuffix = " (Health Tint, runtime)";
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int ShallowColorID = Shader.PropertyToID("_ShallowColor");

    // --- runtime state ---
    private Material _originalMat;   // the shared asset — restored on disable, never written to
    private Material _clone;         // the only thing we tint
    private bool _installed;
    private float _health;

    // Healthy baseline, captured from the scene as authored. Copied field-by-field: ShadingSettings is a
    // CLASS, so holding a reference would alias the live object and the baseline would follow our writes.
    private Color _hBaseColor, _hShallowColor;
    private float _hFogDensity, _hFogStartDistance, _hFogBrightness, _hColorAbsorption, _hCausticsBrightness;

    private void OnEnable()
    {
        if (!Application.isPlaying) return; // play mode only — see the class summary

        if (area == null) area = GetComponent<UnderwaterArea>();
        if (area == null) area = FindFirstObjectByType<UnderwaterArea>();
        if (sim == null) sim = FindFirstObjectByType<EcosystemSimulationGPU>();

        if (area == null)
        {
            Debug.LogWarning("[UnderwaterHealthTint] No UnderwaterArea found — tint disabled.", this);
            enabled = false;
            return;
        }

        _health = ReadTargetHealth();
        Sync();
    }

    private void OnDisable() => Uninstall();

    private void Update()
    {
        if (!Application.isPlaying) return;

        Sync();
        if (!_installed) return;

        if (sim == null && !debugOverride) sim = FindFirstObjectByType<EcosystemSimulationGPU>();

        float target = ReadTargetHealth();
        if (smoothSpeed > 0f)
        {
            float k = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
            _health = Mathf.Lerp(_health, target, k);
        }
        else _health = target;

        Apply(ResponseAt(_health));
    }

    // -------------------------------------------------------------------------------------------------
    // Install / uninstall — the clone only ever exists between these two
    // -------------------------------------------------------------------------------------------------

    private void Sync()
    {
        bool shouldDrive = effectEnabled && Application.isPlaying;
        if (shouldDrive && !_installed) Install();
        else if (!shouldDrive && _installed) Uninstall();
    }

    private void Install()
    {
        if (area == null) return;

        Material src = area.waterMaterial;
        if (src == null)
        {
            Debug.LogWarning("[UnderwaterHealthTint] UnderwaterArea has no water material — nothing to tint.", this);
            return;
        }

        // A leftover clone means a previous teardown was missed; cloning it would bake the murk in as the
        // new "healthy" baseline. Bail loudly instead — reloading the scene restores the real reference.
        if (src.name.EndsWith(CloneSuffix))
        {
            Debug.LogError("[UnderwaterHealthTint] Water material is already a tint clone — refusing to re-clone " +
                           "(this would corrupt the healthy baseline). Reload the scene to recover.", this);
            return;
        }

        CaptureHealthyBaseline(src);

        _originalMat = src;
        _clone = new Material(src)
        {
            name = src.name + CloneSuffix,
            hideFlags = HideFlags.HideAndDontSave
        };

        area.waterMaterial = _clone;
        if (waterSurface != null) waterSurface.sharedMaterial = _clone;

        _installed = true;
        _health = ReadTargetHealth();
        Apply(ResponseAt(_health));

        if (logDebug)
            Debug.Log($"[UnderwaterHealthTint] Installed. Cloned '{src.name}'; healthy at {healthyAt:P0}.", this);
    }

    private void Uninstall()
    {
        if (!_installed) return;

        if (area != null)
        {
            if (_originalMat != null) area.waterMaterial = _originalMat;
            RestoreHealthyShading();
        }
        if (waterSurface != null && _originalMat != null) waterSurface.sharedMaterial = _originalMat;

        if (_clone != null)
        {
            if (Application.isPlaying) Destroy(_clone); else DestroyImmediate(_clone);
            _clone = null;
        }

        _originalMat = null;
        _installed = false;

        if (logDebug) Debug.Log("[UnderwaterHealthTint] Uninstalled — original material and settings restored.", this);
    }

    // -------------------------------------------------------------------------------------------------
    // The blend
    // -------------------------------------------------------------------------------------------------

    /// <summary>0 = fully murky, 1 = fully healthy. Reaches 1 at <see cref="healthyAt"/>.</summary>
    private float ResponseAt(float health01)
    {
        float t = Mathf.Clamp01(health01 / Mathf.Max(0.0001f, healthyAt));
        return response != null && response.length > 0 ? response.Evaluate(t) : t;
    }

    private void Apply(float t)
    {
        if (_clone == null || area == null) return;

        // Material colours are stored LINEAR (project is Linear color space) but the Inspector authors
        // sRGB, so convert the murky end before blending against the material-read baseline.
        Color murkyBase = ToMaterialSpace(murkyBaseColor);
        Color murkyShallow = ToMaterialSpace(murkyShallowColor);

        // Blend RGB only — alpha on these drives water surface transparency, which isn't ours to touch.
        Color baseCol = Color.Lerp(murkyBase, _hBaseColor, t);
        Color shallowCol = Color.Lerp(murkyShallow, _hShallowColor, t);
        baseCol.a = _hBaseColor.a;
        shallowCol.a = _hShallowColor.a;

        _clone.SetColor(BaseColorID, baseCol);
        _clone.SetColor(ShallowColorID, shallowCol);

        UnderwaterArea.ShadingSettings s = area.shadingSettings;
        if (s == null) return;

        s.fogDensity = Mathf.Lerp(murkyFogDensity, _hFogDensity, t);
        s.fogStartDistance = Mathf.Lerp(murkyFogStartDistance, _hFogStartDistance, t);
        s.fogBrightness = Mathf.Lerp(murkyFogBrightness, _hFogBrightness, t);
        s.colorAbsorption = Mathf.Lerp(murkyColorAbsorption, _hColorAbsorption, t);
        s.causticsBrightness = Mathf.Lerp(murkyCausticsBrightness, _hCausticsBrightness, t);
    }

    private void CaptureHealthyBaseline(Material src)
    {
        _hBaseColor = src.GetColor(BaseColorID);
        _hShallowColor = src.GetColor(ShallowColorID);

        UnderwaterArea.ShadingSettings s = area.shadingSettings;
        if (s == null) return;
        _hFogDensity = s.fogDensity;
        _hFogStartDistance = s.fogStartDistance;
        _hFogBrightness = s.fogBrightness;
        _hColorAbsorption = s.colorAbsorption;
        _hCausticsBrightness = s.causticsBrightness;
    }

    private void RestoreHealthyShading()
    {
        UnderwaterArea.ShadingSettings s = area.shadingSettings;
        if (s == null) return;
        s.fogDensity = _hFogDensity;
        s.fogStartDistance = _hFogStartDistance;
        s.fogBrightness = _hFogBrightness;
        s.colorAbsorption = _hColorAbsorption;
        s.causticsBrightness = _hCausticsBrightness;
    }

    private float ReadTargetHealth()
    {
        if (debugOverride) return Mathf.Clamp01(debugHealth);
        return sim != null ? Mathf.Clamp01(sim.EcoHealth01) : 0f;
    }

    private static Color ToMaterialSpace(Color c) =>
        QualitySettings.activeColorSpace == ColorSpace.Linear ? c.linear : c;
}
