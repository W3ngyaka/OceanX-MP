using System.Collections.Generic;
using UnityEngine;
using OceanX.BoidsGPU.Ecosystem;

/// <summary>
/// Reveals the reef environment (corals, rockwork, flora) as ecosystem health climbs.
///
/// Reads <see cref="EcosystemSimulationGPU.EcoHealth01"/> DIRECTLY (host-side, no netcode — the sim and
/// the corals only exist on the big-screen host scene, exactly like <c>HealthBarBinder</c>). Each
/// <see cref="RevealGroup"/> points at a labelled parent Transform; its child renderers are auto-collected.
/// The corals sit flat under one "Corals" parent, so the intended setup is ONE group on that parent in
/// <see cref="AppearMode.Proportional"/> mode.
///
/// THREE reveal modes per group:
///  • Proportional — visible count tracks health: visible = round(health01 * childCount). 100 corals → 1 per 1%,
///    300 → 3 per 1%. Corals pop in / retract one-by-one as the bar moves. No manual grouping.
///  • AllAtOnce   — the whole group appears the moment health crosses `threshold`.
///  • Staggered   — like AllAtOnce but members pop in one-by-one, `staggerInterval` apart, after the threshold.
///
/// FADE MODE = SCALE POP-IN (placeholder). Corals grow from zero to their authored scale with an optional
/// overshoot pop. All visual writes go through <see cref="ApplyReveal"/> / <see cref="ForceVisible"/>.
///
/// PREVIEW: scale is serialized, so by default this only drives scale in PLAY mode. Turn on
/// <c>previewInEditor</c> to also drive it in edit mode (drag Debug Health to watch it) — ⚠ turn it OFF
/// before saving the scene, or corals get saved at whatever scale they're previewing at.
/// </summary>
[ExecuteAlways]
public class EnvironmentHealthReveal : MonoBehaviour
{
    public enum AppearMode
    {
        Proportional, // visible count tracks health (round(health01 * childCount)); one-by-one as the bar moves
        AllAtOnce,    // whole group pops in together once health crosses `threshold`
        Staggered     // members pop in one-by-one, staggerInterval apart, after `threshold` is crossed
    }

    [System.Serializable]
    public class RevealGroup
    {
        [Tooltip("Label only — for your own reference in the Inspector.")]
        public string name = "Corals";

        [Tooltip("Labelled parent GameObject. Every Renderer under it (incl. inactive) is auto-collected.")]
        public Transform parent;

        public AppearMode appearMode = AppearMode.Proportional;

        [Header("Proportional mode")]
        [Tooltip("Health at which the FIRST coral appears (0..1).")]
        [Range(0f, 1f)] public float startHealth = 0f;
        [Tooltip("Health at which the LAST coral appears (all present at/above this).")]
        [Range(0f, 1f)] public float endHealth = 1f;

        [Header("AllAtOnce / Staggered mode")]
        [Tooltip("Health (0..1) at which this group starts appearing.")]
        [Range(0f, 1f)] public float threshold = 0.25f;
        [Tooltip("Only shrinks back out once health drops below (threshold - margin). Stops flicker on the line.")]
        [Range(0f, 0.5f)] public float hysteresisMargin = 0.05f;
        [Tooltip("Staggered mode: seconds between each member starting its pop-in.")]
        [Min(0f)] public float staggerInterval = 0.15f;

        [Header("Common")]
        [Tooltip("Reveal corals in a random order instead of hierarchy order (Proportional + Staggered).")]
        public bool randomOrder = false;
        [Tooltip("Seconds for one member to grow from zero to full scale.")]
        [Min(0.01f)] public float growDuration = 0.6f;
        [Tooltip("Add a little overshoot so corals 'pop' past full scale then settle.")]
        public bool overshoot = true;
        [Tooltip("Overshoot strength (0 = none). ~1.7 gives a ~10% pop.")]
        [Min(0f)] public float overshootStrength = 1.70158f;

        // --- runtime state (not authored) ---
        [System.NonSerialized] public List<Item> items;
        [System.NonSerialized] public bool shown;       // latched on/off band (AllAtOnce / Staggered)
        [System.NonSerialized] public float showTimer;  // seconds this group has been in the "shown" state
        [System.NonSerialized] public int lastVisible;  // for debug logging
    }

    /// <summary>One collected renderer + the cached info needed to drive its scale.</summary>
    public class Item
    {
        public Transform target;      // the coral object's transform
        public Vector3 originalScale; // authored scale, restored on ForceVisible
        public float reveal;          // current 0..1 progress
        public int revealOrder;       // rank in the reveal sequence (hierarchy order, or shuffled)
    }

    [Header("Source (auto-found if left empty)")]
    [SerializeField] private EcosystemSimulationGPU sim;

    [Header("Debug / preview")]
    [Tooltip("Ignore the live sim and drive the reveal from the slider below.")]
    [SerializeField] private bool debugOverride = false;
    [Range(0f, 1f)][SerializeField] private float debugHealth = 1f;
    [Tooltip("How fast the internal health value eases toward the live/target value (Play mode).")]
    [SerializeField] private float smoothSpeed = 4f;
    [Tooltip("Drive coral scale in EDIT mode too, so you can preview by dragging Debug Health without Play.\n" +
             "⚠ Turn OFF before saving the scene — otherwise corals get saved at their previewed scale.")]
    [SerializeField] private bool previewInEditor = false;
    [Tooltip("Log collected coral counts + live health/visible to the Console.")]
    [SerializeField] private bool logDebug = false;

    [Header("Master toggle")]
    [Tooltip("ON  = corals start hidden (scale 0) and pop in with health.\n" +
             "OFF = effect disabled; all corals forced to full scale (normal scene).")]
    [SerializeField] private bool effectEnabled = true;
    [Tooltip("Once a coral has appeared it stays visible even if health later drops.")]
    [SerializeField] private bool revealOnly = false;

    [Header("Reveal groups (one per labelled parent)")]
    [SerializeField] private List<RevealGroup> groups = new List<RevealGroup>();

    private float _health;     // smoothed 0..1
    private bool _wasDisabled; // tracks effectEnabled edge so we restore visuals once on turn-off

    private void OnEnable()
    {
        if (sim == null) sim = FindFirstObjectByType<EcosystemSimulationGPU>();
        CollectItems();
        _health = ReadTargetHealth();
    }

    private void OnDisable()
    {
        ForceVisible(); // never leave the reef shrunk when switched off / stops previewing
    }

    private void Update()
    {
        if (sim == null && !debugOverride) sim = FindFirstObjectByType<EcosystemSimulationGPU>();
        if (!ItemsValid()) CollectItems();

        // Master OFF → force full scale once, then idle.
        if (!effectEnabled)
        {
            if (!_wasDisabled) { ForceVisible(); _wasDisabled = true; }
            return;
        }
        _wasDisabled = false;

        bool driving = Application.isPlaying || previewInEditor;
        if (!driving)
        {
            ForceVisible(); // edit mode, preview off → keep corals at authored scale (don't save them shrunk)
            return;
        }

        // Health value → smoothed in play, snapped in edit-preview.
        float target = ReadTargetHealth();
        if (Application.isPlaying && smoothSpeed > 0f)
        {
            float k = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
            _health = Mathf.Lerp(_health, target, k);
        }
        else _health = target;

        float dt = Application.isPlaying ? Time.deltaTime : 1f; // snap in edit-preview

        foreach (RevealGroup g in groups)
        {
            if (g == null || g.items == null) continue;
            if (g.appearMode == AppearMode.Proportional) UpdateProportional(g, dt);
            else UpdateThresholded(g, dt);
        }
    }

    // ---------------------------------------------------------------------------------------------
    // Reveal modes
    // ---------------------------------------------------------------------------------------------

    private void UpdateProportional(RevealGroup g, float dt)
    {
        int count = g.items.Count;
        float denom = Mathf.Max(0.0001f, g.endHealth - g.startHealth);
        float span01 = Mathf.Clamp01((_health - g.startHealth) / denom);
        int visible = Mathf.RoundToInt(span01 * count);
        if (revealOnly) visible = Mathf.Max(visible, CountRevealed(g));

        if (logDebug && visible != g.lastVisible)
        {
            Debug.Log($"[EnvReveal] '{g.name}': health={_health:0.00} → {visible}/{count} corals visible", this);
            g.lastVisible = visible;
        }

        for (int i = 0; i < count; i++)
        {
            Item it = g.items[i];
            if (it == null || it.target == null) continue;
            float itemTarget = it.revealOrder < visible ? 1f : 0f;
            StepReveal(it, itemTarget, g, dt);
        }
    }

    private void UpdateThresholded(RevealGroup g, float dt)
    {
        float onLine = g.threshold;
        float offLine = Mathf.Max(0f, g.threshold - g.hysteresisMargin);
        if (!g.shown && _health >= onLine) { g.shown = true; g.showTimer = 0f; }
        else if (g.shown && !revealOnly && _health < offLine) { g.shown = false; g.showTimer = 0f; }

        g.showTimer += dt;

        for (int i = 0; i < g.items.Count; i++)
        {
            Item it = g.items[i];
            if (it == null || it.target == null) continue;

            float startDelay = g.appearMode == AppearMode.Staggered ? it.revealOrder * g.staggerInterval : 0f;
            float itemTarget = g.shown && g.showTimer >= startDelay ? 1f : 0f;
            StepReveal(it, itemTarget, g, dt);
        }
    }

    private void StepReveal(Item it, float itemTarget, RevealGroup g, float dt)
    {
        float step = Application.isPlaying ? dt / Mathf.Max(0.01f, g.growDuration) : 1f; // snap in edit-preview
        it.reveal = Mathf.MoveTowards(it.reveal, itemTarget, step);
        ApplyReveal(it, it.reveal, g);
    }

    private static int CountRevealed(RevealGroup g)
    {
        int n = 0;
        foreach (Item it in g.items) if (it != null && it.reveal > 0.001f) n++;
        return n;
    }

    // ---------------------------------------------------------------------------------------------
    // Health source
    // ---------------------------------------------------------------------------------------------

    private float ReadTargetHealth()
    {
        if (debugOverride) return Mathf.Clamp01(debugHealth);
        return sim != null ? Mathf.Clamp01(sim.EcoHealth01) : 0f;
    }

    // ---------------------------------------------------------------------------------------------
    // Collection
    // ---------------------------------------------------------------------------------------------

    private bool ItemsValid()
    {
        foreach (RevealGroup g in groups)
            if (g != null && g.parent != null && g.items == null) return false;
        return true;
    }

    private void CollectItems()
    {
        foreach (RevealGroup g in groups)
        {
            if (g == null) continue;
            g.items = new List<Item>();
            g.shown = false;
            g.showTimer = 0f;
            g.lastVisible = -1;

            if (g.parent == null)
            {
                Debug.LogWarning($"[EnvReveal] Group '{g.name}' has no Parent assigned — nothing to reveal.", this);
                continue;
            }

            Renderer[] renderers = g.parent.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers)
            {
                if (r == null) continue;
                Transform t = r.transform;
                g.items.Add(new Item
                {
                    target = t,
                    originalScale = t.localScale, // authored scale captured while at full size
                    reveal = 0f,
                    revealOrder = g.items.Count,
                });
            }

            AssignRevealOrder(g);

            if (g.items.Count == 0)
                Debug.LogWarning($"[EnvReveal] Group '{g.name}': found 0 Renderers under '{g.parent.name}'. " +
                                 "Are the corals children of this parent and do they have MeshRenderers?", this);
            else if (logDebug)
                Debug.Log($"[EnvReveal] Group '{g.name}': collected {g.items.Count} corals under '{g.parent.name}'.", this);
        }
    }

    private static void AssignRevealOrder(RevealGroup g)
    {
        int n = g.items.Count;
        if (!g.randomOrder)
        {
            for (int i = 0; i < n; i++) g.items[i].revealOrder = i;
            return;
        }
        int[] order = new int[n];
        for (int i = 0; i < n; i++) order[i] = i;
        for (int i = n - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (order[i], order[j]) = (order[j], order[i]);
        }
        for (int i = 0; i < n; i++) g.items[i].revealOrder = order[i];
    }

    // ---------------------------------------------------------------------------------------------
    // Visuals — the ONLY places that touch the corals. Swap these to change the reveal technique.
    // ---------------------------------------------------------------------------------------------

    /// <summary>Placeholder = scale pop-in: grow from 0 to authored scale, with an optional overshoot pop.</summary>
    private void ApplyReveal(Item it, float amount, RevealGroup g)
    {
        float eased = g.overshoot ? EaseOutBack(amount, g.overshootStrength)
                                  : Mathf.SmoothStep(0f, 1f, amount);
        it.target.localScale = it.originalScale * eased;
    }

    /// <summary>Restore every coral to its authored scale (only writes when it actually differs).</summary>
    private void ForceVisible()
    {
        foreach (RevealGroup g in groups)
        {
            if (g == null || g.items == null) continue;
            foreach (Item it in g.items)
            {
                if (it == null || it.target == null) continue;
                it.reveal = 1f;
                if (it.target.localScale != it.originalScale)
                    it.target.localScale = it.originalScale;
            }
        }
    }

    // Ease-out-back: 0 -> 1 with a small overshoot past 1 near the end, settling exactly at 1.
    private static float EaseOutBack(float t, float s)
    {
        t -= 1f;
        return 1f + (s + 1f) * t * t * t + s * t * t;
    }
}
