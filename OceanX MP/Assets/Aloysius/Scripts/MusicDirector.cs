using System.Collections.Generic;
using UnityEngine;
using OceanX.BoidsGPU.Ecosystem;

// Adaptive ("dynamic") music. Several looping stems all play IN SYNC from the start; their
// volumes are cross-faded against the live ecosystem health, so the music gains or loses
// momentum with what the player is doing — no track switching, no restart, no seams.
//
// SETUP
//  1. Author stems of the SAME length + tempo so they stay locked together, e.g.
//       "Ambient"  health 0.00 - 1.00   (bed — always audible)
//       "Tension"  health 0.00 - 0.45   (reef struggling / apex removed)
//       "Warmth"   health 0.45 - 1.00   (recovering)
//       "Thriving" health 0.75 - 1.00   (full, busy reef)
//  2. Drop this on ONE object (e.g. AudioManager) on the LARGE SCREEN scene.
//  3. Call PlaySwell() on a positive beat (e.g. a species arriving) for a momentary lift.
public class MusicDirector : MonoBehaviour
{
    [System.Serializable]
    public class Layer
    {
        public string name = "Layer";
        public AudioClip clip;

        [Tooltip("Ecosystem-health range (0-1) this stem is audible in.")]
        [Range(0f, 1f)] public float healthMin = 0f;
        [Range(0f, 1f)] public float healthMax = 1f;

        [Range(0f, 1f)] public float maxVolume = 1f;

        [Tooltip("How soft the edges of the band are (0 = hard cut in/out).")]
        [Range(0f, 0.5f)] public float blend = 0.12f;

        [HideInInspector] public AudioSource source;
    }

    [Header("Stems (same length + tempo so they stay in sync)")]
    public List<Layer> layers = new List<Layer>();

    [Header("Mix")]
    [Range(0f, 1f)] public float masterVolume = 0.7f;
    [Tooltip("How fast a stem fades in/out (volume per second). Lower = smoother momentum shifts.")]
    public float fadeSpeed = 0.4f;

    [Header("Event swell (e.g. a species arrives)")]
    public AudioClip swell;
    [Range(0f, 1f)] public float swellVolume = 0.8f;

    [Header("Debug")]
    [Tooltip("Ignore the live ecosystem and drive the mix from this slider (for auditioning).")]
    public bool overrideHealth = false;
    [Range(0f, 1f)] public float debugHealth = 0.5f;

    /// <summary>The director in the scene, so events can fire swells without wiring a reference.</summary>
    public static MusicDirector Instance { get; private set; }

    private AudioSource _swellSource;
    private EcosystemSimulationGPU _sim;
    private float _health = 0.5f;

    void Awake()
    {
        // Singleton guard: if a MusicDirector already survives (e.g. carried from another
        // scene, or a second scene loaded additively brought its own AudioManager), destroy
        // this duplicate so we never stack two sets of stems playing out of phase.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);   // one director persists across scene loads
    }

    void Start()
    {
        // One AudioSource per stem, all started together so they stay phase-locked.
        foreach (var l in layers)
        {
            if (l == null || l.clip == null) continue;
            var src = gameObject.AddComponent<AudioSource>();
            src.clip = l.clip;
            src.loop = true;
            src.playOnAwake = false;
            src.volume = 0f;
            l.source = src;
        }
        foreach (var l in layers) if (l?.source != null) l.source.Play();

        _swellSource = gameObject.AddComponent<AudioSource>();
        _swellSource.playOnAwake = false;
        _swellSource.loop = false;
    }

    void Update()
    {
        _health = overrideHealth ? debugHealth : ReadHealth();

        foreach (var l in layers)
        {
            if (l?.source == null) continue;
            float target = l.maxVolume * Weight(l, _health) * masterVolume;
            l.source.volume = Mathf.MoveTowards(l.source.volume, target, fadeSpeed * Time.unscaledDeltaTime);
        }
    }

    // Live ecosystem health (0-1). Works on the host (sim) or a client (synced value).
    float ReadHealth()
    {
        var net = EcosystemNetworkManagerGPU.Instance;
        if (net != null) return net.GetEcoHealth();

        if (_sim == null) _sim = FindFirstObjectByType<EcosystemSimulationGPU>();
        if (_sim != null) return _sim.EcoHealth01;

        return _health;   // nothing to read yet — hold the last value
    }

    // How present this stem should be at the given health, with soft band edges.
    static float Weight(Layer l, float h)
    {
        if (l.blend <= 0f) return (h >= l.healthMin && h <= l.healthMax) ? 1f : 0f;

        // Anchored ends (0 / 1) have no edge to fade against.
        float lo = (l.healthMin <= 0f) ? 1f : Mathf.InverseLerp(l.healthMin - l.blend, l.healthMin + l.blend, h);
        float hi = (l.healthMax >= 1f) ? 1f : 1f - Mathf.InverseLerp(l.healthMax - l.blend, l.healthMax + l.blend, h);
        return Mathf.Clamp01(Mathf.Min(lo, hi));
    }

    /// <summary>One-shot lift on a positive beat (species arrives, health milestone).</summary>
    public void PlaySwell()
    {
        if (swell != null && _swellSource != null)
            _swellSource.PlayOneShot(swell, swellVolume * masterVolume);
    }

    /// <summary>Current health the mix is following (for debugging / UI).</summary>
    public float CurrentHealth => _health;
}
