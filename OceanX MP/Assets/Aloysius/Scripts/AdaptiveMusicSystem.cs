using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;
using OceanX.BoidsGPU.Ecosystem;

// Reactive, health-driven LAYERED music (a.k.a. vertical remixing / adaptive layering).
//
// Every stem loops IN SYNC from the start and NEVER stops. Each stem's loudness rides the live,
// smoothed ecosystem health through its own curve: at 0% health the "sad" layer is up and the
// hopeful/thriving layers are silent; as health climbs, the hopeful layers fade IN ON TOP while the
// sad one fades out. Because nothing ever starts or stops, transitions have no seam and no restart —
// a "transition" is just faders moving. Where two layers' curves overlap, they are audible together,
// which is the "layers mixing" the brief asks for.
//
// FADES ARE DONE IN DECIBELS, through an AudioMixer.
//   AudioSource.volume is LINEAR amplitude but hearing is LOGARITHMIC — volume 0.5 is already -6dB
//   (nearly full loudness), so a linear fade sounds "instant then crawling / too loud". Driving a
//   mixer group's exposed volume (dB) is perceptually even AND smooth even at low frame-rate, because
//   the audio engine interpolates the parameter between frames. (Snapshots are deliberately NOT used —
//   they dip in the middle of a crossfade.)
//
// SETUP
//  1. Author stems of the SAME tempo / key / LENGTH so they stay phase-locked as they loop.
//  2. Create an AudioMixer (e.g. Assets/Sounds/ReefMusic.mixer): one group per layer under Master,
//     and expose each group's Volume as a parameter (BedVol / SorrowVol / ... + MasterVol).
//  3. Drop this on ONE object (e.g. AudioManager) on the LARGE-SCREEN / host scene — the one that
//     actually contains EcosystemSimulationGPU. Assign the mixer, then per Layer assign its clip,
//     its mixer Group, its exposed-param name, and draw its Health -> Volume curve.
//  4. Call PlaySwell() on a positive beat (a species arriving) for a momentary lift.
//
// The mixer is optional: a layer with no group/param assigned falls back to fading its AudioSource
// volume directly (same dB-shaped curve, just no between-frame smoothing), so you can hear the system
// working before the mixer exists.
public class AdaptiveMusicSystem : MonoBehaviour
{
    [System.Serializable]
    public class Layer
    {
        public string name = "Layer";
        public AudioClip clip;

        [Tooltip("This stem's OWN level — a per-track trim so you can balance one against another " +
                 "(e.g. make the bed quieter, the finale louder) WITHOUT redrawing its curve. Multiplied " +
                 "on top of the curve and the master. 1 = full, 0 = this layer silent.")]
        [Range(0f, 1f)] public float volume = 1f;

        [Tooltip("The whole emotional arc for this stem: health (0-1, X) -> its presence (0-1, Y). " +
                 "Draw where it fades in / out. Overlap with other layers' curves = they mix together.")]
        public AnimationCurve healthToVolume = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Tooltip("This stem's own mixer group (its output). Leave empty to fall back to plain " +
                 "AudioSource-volume fading for this layer.")]
        public AudioMixerGroup group;

        [Tooltip("The exposed Volume parameter (dB) on that group, that this script drives. Must match " +
                 "the name you exposed in the AudioMixer exactly. Empty = use the AudioSource fallback.")]
        public string exposedParam;

        [System.NonSerialized] public AudioSource source;
        [System.NonSerialized] public float currentVolume;   // last linear volume applied (for debug/UI)
    }

    [Header("Stems (same tempo + length so they stay in sync)")]
    public List<Layer> layers = new List<Layer>();

    [Header("Mix")]
    [Tooltip("The AudioMixer holding the groups + exposed params. Optional — see the per-layer fallback.")]
    public AudioMixer mixer;

    [Range(0f, 1f)]
    [Tooltip("Overall level, multiplied into every layer before the dB conversion.")]
    public float masterVolume = 0.6f;

    [Tooltip("Exposed MASTER volume parameter (dB) on the mixer, if you want this script to drive it too. " +
             "Leave empty to apply masterVolume per-layer instead (works with or without a mixer).")]
    public string masterExposedParam = "";

    [Header("Transition")]
    [Tooltip("HOW LONG a transition takes, in seconds. The music follows a smoothed version of health, " +
             "and this is that smoothing time. RAISE IT for longer, slower, more cinematic fades " +
             "(try 5-8); lower it for snappy, responsive moves; 0 = instant, no fade. This is the main " +
             "knob for transition length.")]
    [Min(0f)]
    [FormerlySerializedAs("responseSeconds")]   // keep existing scene values after the rename
    public float transitionSeconds = 2f;

    [Tooltip("Decibels treated as fully silent — the floor for the log conversion. -80 is inaudible.")]
    public float silenceDb = -80f;

    [Header("Event swell (e.g. a species arrives)")]
    public AudioClip swell;
    [Range(0f, 1f)] public float swellVolume = 0.8f;

    [Header("Debug")]
    [Tooltip("Ignore the live ecosystem and drive the music from the slider below (for auditioning).")]
    public bool overrideHealth = false;
    [Range(0f, 1f)] public float debugHealth = 0.5f;
    [Tooltip("Show a small on-screen readout of health + each layer's current volume, so behaviour is " +
             "never a guess. Editor/host only; turn off for the exhibit.")]
    public bool showDebugReadout = false;

    /// <summary>The music system in the scene, so events can fire swells without wiring a reference.</summary>
    public static AdaptiveMusicSystem Instance { get; private set; }

    private AudioSource _swellSource;
    private EcosystemSimulationGPU _sim;
    private float _health = 0f;
    private bool  _started;

    /// <summary>Current (smoothed) health the music is following (for debugging / UI).</summary>
    public float CurrentHealth => _health;

    void Awake()
    {
        // Singleton guard: never let a second system stack a duplicate set of stems out of phase.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);   // one music system persists across scene loads
    }

    void Start()
    {
        // One AudioSource per stem, each routed to its own mixer group. They all play, forever, in sync;
        // only their loudness moves. Volume stays 1 here — loudness is controlled at the mixer (or via the
        // per-layer fallback below), so this source level is a clean unity gain.
        foreach (var l in layers)
        {
            if (l == null || l.clip == null) continue;   // an unassigned clip = that layer simply doesn't exist
            var src = gameObject.AddComponent<AudioSource>();
            src.clip = l.clip;
            src.loop = true;
            src.playOnAwake = false;
            src.volume = 1f;
            if (l.group != null) src.outputAudioMixerGroup = l.group;
            l.source = src;
        }

        // Seed health BEFORE the first fade so we don't audibly ramp up from 0 on load — start the mix
        // already sitting where current health says it should.
        _health = overrideHealth ? debugHealth : ReadHealth();
        ApplyMix(instant: true);

        // Sample-accurate sync: schedule every stem to begin on the SAME DSP tick, a hair in the future.
        // Plain Play() in a loop is close but not sample-locked; PlayScheduled guarantees they align.
        double startAt = AudioSettings.dspTime + 0.1;
        foreach (var l in layers)
            if (l?.source != null) l.source.PlayScheduled(startAt);

        _swellSource = gameObject.AddComponent<AudioSource>();
        _swellSource.playOnAwake = false;
        _swellSource.loop = false;

        _started = true;
    }

    void Update()
    {
        if (!_started) return;

        float raw = overrideHealth ? debugHealth : ReadHealth();

        // Smooth the HEALTH input (one value drives every layer, so they move coherently). Frame-rate
        // independent exponential approach — the same shape HealthBarBinder / EnvironmentHealthReveal use.
        if (transitionSeconds <= 0.0001f)
        {
            _health = raw;
        }
        else
        {
            float k = 1f - Mathf.Exp(-(Time.unscaledDeltaTime / transitionSeconds));
            _health = Mathf.Lerp(_health, raw, k);
        }

        ApplyMix(instant: false);
    }

    // Pushes the current smoothed health through every layer's curve to a volume, and applies it either
    // via the mixer (dB) or, if this layer has no group/param, straight onto the AudioSource.
    void ApplyMix(bool instant)
    {
        // Optional master param on the mixer. When it's driven here, per-layer keeps masterVolume out of
        // its own maths (so we don't apply master twice).
        bool masterOnMixer = mixer != null && !string.IsNullOrEmpty(masterExposedParam);
        if (masterOnMixer) mixer.SetFloat(masterExposedParam, LinearToDb(masterVolume));

        for (int i = 0; i < layers.Count; i++)
        {
            var l = layers[i];
            if (l == null) continue;

            float curve = Mathf.Clamp01(l.healthToVolume != null ? l.healthToVolume.Evaluate(_health) : 0f);
            float trim  = Mathf.Clamp01(l.volume);
            float lin   = masterOnMixer ? (curve * trim) : (curve * trim * masterVolume);
            l.currentVolume = lin;

            bool useMixer = mixer != null && l.group != null && !string.IsNullOrEmpty(l.exposedParam);
            if (useMixer)
            {
                mixer.SetFloat(l.exposedParam, LinearToDb(lin));
            }
            else if (l.source != null)
            {
                // Fallback: no mixer wiring for this layer. Fade the source directly. It's still shaped
                // by the same curve, so it sounds right — it just misses the engine's between-frame
                // interpolation the mixer path gets.
                l.source.volume = lin;
            }
        }
    }

    // Linear amplitude (0-1) -> decibels for a mixer volume parameter. Silence floors at silenceDb so we
    // never feed -infinity into the mixer.
    float LinearToDb(float linear)
    {
        if (linear <= 0.0001f) return silenceDb;
        return Mathf.Max(silenceDb, Mathf.Log10(linear) * 20f);
    }

    // Live ecosystem health (0-1). Host-side: read the sim directly. Holds the last value if the sim
    // isn't present yet (e.g. first frames, or a scene without the simulation).
    float ReadHealth()
    {
        if (_sim == null) _sim = FindFirstObjectByType<EcosystemSimulationGPU>();
        if (_sim != null) return Mathf.Clamp01(_sim.EcoHealth01);
        return _health;
    }

    /// <summary>One-shot lift on a positive beat (species arrives, health milestone).</summary>
    public void PlaySwell()
    {
        if (swell != null && _swellSource != null)
            _swellSource.PlayOneShot(swell, swellVolume * masterVolume);
    }

    void OnGUI()
    {
        if (!showDebugReadout) return;

        var style = new GUIStyle(GUI.skin.label) { fontSize = 14, richText = true };
        float y = 10f;
        GUI.Label(new Rect(10, y, 500, 22), $"<b>Adaptive Music</b>   health {_health:0.00}" +
            (overrideHealth ? "  <color=yellow>(override)</color>" : ""), style);
        y += 22f;
        foreach (var l in layers)
        {
            if (l == null) continue;
            int bars = Mathf.RoundToInt(l.currentVolume * 20f);
            string bar = new string('|', bars) + new string('.', 20 - bars);
            GUI.Label(new Rect(10, y, 500, 20), $"{l.name,-10} {l.currentVolume:0.00} [{bar}]", style);
            y += 20f;
        }
    }
}
