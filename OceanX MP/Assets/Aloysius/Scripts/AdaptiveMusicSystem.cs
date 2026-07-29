using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;
using OceanX.BoidsGPU.Ecosystem;

// Health-driven WHOLE-SONG music switcher (a.k.a. horizontal re-sequencing).
//
// ONE mood song plays at a time. Each song owns a band of the live ecosystem health (0-1). When health
// moves into a different band, we CROSSFADE from the current song to that band's song — the old one
// fades out as the new one fades in, then the old one stops. Only one song (two during the ~3s fade) is
// ever audible, so the songs do NOT need to share tempo / key / length. That is the whole point: these
// are complete, standalone tracks of different lengths, and stacking them permanently (vertical layering)
// would drift out of phase the instant the shortest one loops. Switching between whole songs avoids that.
//
// WHY IT WON'T FLICKER when health hovers on a boundary (and the sim is DESIGNED to oscillate):
//   1. The health INPUT is smoothed (healthSmoothing) before anything looks at it.
//   2. Band selection has HYSTERESIS — once we're in a band, health must move PAST the shared edge by
//      `hysteresis` before we'll leave it, so sitting right on 0.30 doesn't ping-pong.
//   3. A minimum DWELL time (minMoodSeconds) blocks a second switch until the current mood has played a
//      while, so a brief dip can't yank the music back and forth.
//
// FADES ARE EQUAL-POWER, IN DECIBELS.
//   Two DIFFERENT songs are uncorrelated, so at the midpoint of a crossfade their powers ADD. A plain
//   linear (equal-gain) fade puts both at -6 dB there, which sums to ~-3 dB — an audible dip / "hole" in
//   the middle of every transition. Equal-power uses sin/cos so both sit at ~-3 dB at the midpoint and
//   sum back to full — no dip. We drive each song's mixer-group Volume (dB), which the audio engine
//   interpolates between frames for smoothness even at low frame-rate. A layer with no mixer group falls
//   back to fading its AudioSource volume directly (same curve, just no between-frame smoothing).
//
// SETUP
//  1. Drop this on ONE object (e.g. AudioManager) on the LARGE-SCREEN / host scene — the one that
//     actually contains EcosystemSimulationGPU.
//  2. Add one Layer per mood, LOW health first. Assign each its song, and (optionally) its mixer Group +
//     exposed Volume param. `bandEdges` sets where one mood hands over to the next (needs layers-1 edges).
//  3. Call PlaySwell() on a positive beat (a species arriving) for a momentary one-shot lift.
public class AdaptiveMusicSystem : MonoBehaviour
{
    [System.Serializable]
    public class Layer
    {
        public string name = "Mood";
        [Tooltip("The whole song for this mood band. Different lengths/tempos are fine — only one plays " +
                 "at a time.")]
        public AudioClip clip;

        [Tooltip("This song's OWN level — a per-track trim so you can balance one against another " +
                 "(e.g. make the somber one quieter, the finale louder) WITHOUT changing the bands. " +
                 "1 = full, 0 = silent.")]
        [Range(0f, 1f)] public float volume = 1f;

        [Tooltip("This song's mixer group (its output). Leave empty to fall back to plain AudioSource-" +
                 "volume fading for this mood.")]
        public AudioMixerGroup group;

        [Tooltip("The exposed Volume parameter (dB) on that group, driven by this script. Must match the " +
                 "name exposed in the AudioMixer exactly. Empty = use the AudioSource fallback.")]
        public string exposedParam;

        [System.NonSerialized] public AudioSource source;
        [System.NonSerialized] public float currentVolume;   // last linear volume applied (for debug/UI)
    }

    [Header("Mood songs (LOW health first)")]
    public List<Layer> layers = new List<Layer>();

    [Tooltip("Health values where one mood hands over to the next, ascending. Needs (layers - 1) edges: " +
             "e.g. 4 songs -> {0.30, 0.60, 0.90} means song0 below 30%, song1 30-60%, song2 60-90%, " +
             "song3 above 90%. Wrong length = even split is used instead.")]
    public float[] bandEdges = new float[] { 0.30f, 0.60f, 0.90f };

    [Header("Mix")]
    [Tooltip("The AudioMixer holding the groups + exposed params. Optional — see the per-layer fallback.")]
    public AudioMixer mixer;

    [Range(0f, 1f)]
    [Tooltip("Overall level, multiplied into every song before the dB conversion.")]
    public float masterVolume = 0.6f;

    [Tooltip("Exposed MASTER volume parameter (dB) on the mixer, if you want this script to drive it too. " +
             "Leave empty to apply masterVolume per-layer instead (works with or without a mixer).")]
    public string masterExposedParam = "";

    [Header("Transition feel")]
    [Tooltip("How long the crossfade from one song to the next takes, in seconds. Bigger = slower, more " +
             "cinematic handover; 0 = hard cut.")]
    [Min(0f)]
    [FormerlySerializedAs("responseSeconds")]   // keep existing scene values after earlier renames
    [FormerlySerializedAs("transitionSeconds")]
    public float crossfadeSeconds = 3f;

    [Tooltip("Smoothing time on the health INPUT before band selection sees it. Softens spikes so a brief " +
             "flicker in health doesn't immediately trigger a mood change. 0 = react to raw health.")]
    [Min(0f)]
    public float healthSmoothing = 2f;

    [Range(0f, 0.25f)]
    [Tooltip("Dead-band around each mood boundary. Once we're in a mood, health must move PAST the edge " +
             "by this much before switching — stops ping-ponging when health sits on a boundary.")]
    public float hysteresis = 0.05f;

    [Min(0f)]
    [Tooltip("Minimum seconds a mood plays before another switch is allowed — a hard floor against rapid " +
             "back-and-forth even if hysteresis is exceeded.")]
    public float minMoodSeconds = 8f;

    [Tooltip("Decibels treated as fully silent — the floor for the log conversion. -80 is inaudible.")]
    public float silenceDb = -80f;

    [Header("Event swell (e.g. a species arrives)")]
    public AudioClip swell;
    [Range(0f, 1f)] public float swellVolume = 0.8f;

    [Header("Debug")]
    [Tooltip("Ignore the live ecosystem and drive the music from the slider below (for auditioning).")]
    public bool overrideHealth = false;
    [Range(0f, 1f)] public float debugHealth = 0.5f;
    [Tooltip("Show a small on-screen readout of health + the current mood, so behaviour is never a guess. " +
             "Editor/host only; turn off for the exhibit.")]
    public bool showDebugReadout = false;

    /// <summary>The music system in the scene, so events can fire swells without wiring a reference.</summary>
    public static AdaptiveMusicSystem Instance { get; private set; }

    private AudioSource _swellSource;
    private EcosystemSimulationGPU _sim;
    private float _health = 0f;
    private bool  _started;

    // Which mood song is "in charge". During a crossfade _fadeTo is the incoming one; _fadeFrom is the
    // outgoing one (or -1 for the initial fade-in from silence). _fadeT runs 0->1 over crossfadeSeconds.
    private int   _currentBand = 0;
    private int   _fadeFrom    = -1;
    private int   _fadeTo      = -1;
    private float _fadeT       = 1f;   // 1 = no fade in progress
    private bool  _fading;
    private float _moodTimer;          // seconds since the last switch began

    /// <summary>Current (smoothed) health the music is following (for debugging / UI).</summary>
    public float CurrentHealth => _health;

    void Awake()
    {
        // Singleton guard: never let a second system stack a duplicate song out of phase.
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
        // One AudioSource per mood, routed to its own mixer group. Nothing plays yet — we only start the
        // song for the mood we begin in, and start others when we crossfade to them (they're whole songs,
        // so we don't keep them all decoding in the background).
        foreach (var l in layers)
        {
            if (l == null || l.clip == null) continue;
            var src = gameObject.AddComponent<AudioSource>();
            src.clip = l.clip;
            src.loop = true;
            src.playOnAwake = false;
            src.spatialBlend = 0f;                 // 2D — music, not a positional sound
            src.volume = 1f;                       // loudness is controlled by the mixer / fallback below
            if (l.group != null) src.outputAudioMixerGroup = l.group;
            l.source = src;
        }

        NormaliseBandEdges();

        // Seed health and pick the mood we should open in, so we don't always start on the "collapsed"
        // track and ramp up.
        _health = overrideHealth ? debugHealth : ReadHealth();
        int startBand = NominalBand(_health);

        _swellSource = gameObject.AddComponent<AudioSource>();
        _swellSource.playOnAwake = false;
        _swellSource.loop = false;
        _swellSource.spatialBlend = 0f;

        _currentBand = startBand;
        _started = true;

        // Gentle fade-in of the opening mood from silence (a "transition" with no outgoing song).
        BeginTransition(startBand, fromSilence: true);
    }

    void Update()
    {
        if (!_started) return;

        float raw = overrideHealth ? debugHealth : ReadHealth();

        // Smooth the health input (frame-rate-independent exponential approach — same shape
        // HealthBarBinder / EnvironmentHealthReveal use) before band selection looks at it.
        if (healthSmoothing <= 0.0001f)
            _health = raw;
        else
        {
            float k = 1f - Mathf.Exp(-(Time.unscaledDeltaTime / healthSmoothing));
            _health = Mathf.Lerp(_health, raw, k);
        }

        _moodTimer += Time.unscaledDeltaTime;

        if (_fading) AdvanceCrossfade();
        else         ConsiderMoodChange();

        ApplyLevels();
    }

    // Decide whether health has moved far enough (past the boundary + hysteresis) AND long enough
    // (min dwell elapsed) to warrant switching to another mood song.
    void ConsiderMoodChange()
    {
        if (_moodTimer < minMoodSeconds) return;

        int c = _currentBand;
        bool leaveUp   = c < layers.Count - 1 && _health > bandEdges[c] + hysteresis;
        bool leaveDown = c > 0               && _health < bandEdges[c - 1] - hysteresis;

        if (!leaveUp && !leaveDown) return;

        int target = NominalBand(_health);
        if (target != c && HasClip(target))
            BeginTransition(target, fromSilence: false);
    }

    void BeginTransition(int toBand, bool fromSilence)
    {
        _fadeFrom = fromSilence ? -1 : _currentBand;
        _fadeTo   = toBand;
        _fadeT    = (crossfadeSeconds <= 0.0001f) ? 1f : 0f;
        _fading   = true;
        _moodTimer = 0f;

        // Start the incoming song from the top so entering a mood is a clear, deliberate musical moment.
        var to = LayerAt(_fadeTo);
        if (to?.source != null) { to.source.time = 0f; if (!to.source.isPlaying) to.source.Play(); }

        if (_fadeT >= 1f) FinishCrossfade();   // hard-cut case (crossfadeSeconds == 0)
    }

    void AdvanceCrossfade()
    {
        _fadeT += Time.unscaledDeltaTime / Mathf.Max(0.0001f, crossfadeSeconds);
        if (_fadeT >= 1f) { _fadeT = 1f; FinishCrossfade(); }
    }

    void FinishCrossfade()
    {
        var from = LayerAt(_fadeFrom);
        if (from?.source != null && from.source.isPlaying) from.source.Stop();

        _currentBand = _fadeTo;
        _fadeFrom = -1;
        _fadeTo   = -1;
        _fading   = false;
    }

    // Push the current state to actual loudness. Equal-power sin/cos during a crossfade; a flat full level
    // for the settled mood; silence for everyone else.
    void ApplyLevels()
    {
        bool masterOnMixer = mixer != null && !string.IsNullOrEmpty(masterExposedParam);
        if (masterOnMixer) mixer.SetFloat(masterExposedParam, LinearToDb(masterVolume));

        // Equal-power gains for the two songs involved in a fade.
        float toGain = 1f, fromGain = 0f;
        if (_fading)
        {
            toGain   = Mathf.Sin(_fadeT * Mathf.PI * 0.5f);
            fromGain = Mathf.Cos(_fadeT * Mathf.PI * 0.5f);
        }

        for (int i = 0; i < layers.Count; i++)
        {
            var l = layers[i];
            if (l == null) continue;

            float gain;
            if (_fading)
            {
                if      (i == _fadeTo)   gain = toGain;
                else if (i == _fadeFrom) gain = fromGain;
                else                     gain = 0f;
            }
            else
            {
                gain = (i == _currentBand) ? 1f : 0f;
            }

            float trim = Mathf.Clamp01(l.volume);
            float lin  = masterOnMixer ? (gain * trim) : (gain * trim * masterVolume);
            l.currentVolume = lin;

            bool useMixer = mixer != null && l.group != null && !string.IsNullOrEmpty(l.exposedParam);
            if (useMixer) mixer.SetFloat(l.exposedParam, LinearToDb(lin));
            else if (l.source != null) l.source.volume = lin;
        }
    }

    // --- band helpers --------------------------------------------------------------------------------

    int EdgeCount() => Mathf.Max(0, layers.Count - 1);

    // Rebuild bandEdges to an even split if they're the wrong length or not ascending — keeps selection
    // sane if someone adds/removes a mood without re-setting the edges.
    void NormaliseBandEdges()
    {
        int need = EdgeCount();
        bool ok = bandEdges != null && bandEdges.Length == need;
        if (ok)
            for (int i = 1; i < bandEdges.Length; i++)
                if (bandEdges[i] <= bandEdges[i - 1]) { ok = false; break; }

        if (ok) return;

        bandEdges = new float[need];
        for (int i = 0; i < need; i++) bandEdges[i] = (i + 1f) / (need + 1f);
    }

    // The band whose range contains h, ignoring hysteresis (the "nominal" home for this health).
    int NominalBand(float h)
    {
        int edges = EdgeCount();
        for (int i = 0; i < edges; i++)
            if (h < bandEdges[i]) return i;
        return Mathf.Max(0, layers.Count - 1);
    }

    Layer LayerAt(int i) => (i >= 0 && i < layers.Count) ? layers[i] : null;
    bool HasClip(int i)  { var l = LayerAt(i); return l != null && l.clip != null; }

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

    /// <summary>Per-species intro sting on first introduction, routed through the swell source so it
    /// respects masterVolume. Falls back to the generic swell when <paramref name="clip"/> is null.</summary>
    public void PlayIntro(AudioClip clip, float volume = 1f)
    {
        if (clip == null) { PlaySwell(); return; }
        if (_swellSource != null)
            _swellSource.PlayOneShot(clip, Mathf.Clamp01(volume) * masterVolume);
    }

    void OnGUI()
    {
        if (!showDebugReadout) return;

        var style = new GUIStyle(GUI.skin.label) { fontSize = 14, richText = true };
        float y = 10f;

        string moodName = LayerAt(_currentBand)?.name ?? "-";
        string state = _fading
            ? $"<color=yellow>crossfading -> {LayerAt(_fadeTo)?.name ?? "-"} ({_fadeT:0.00})</color>"
            : "settled";
        GUI.Label(new Rect(10, y, 600, 22),
            $"<b>Music</b>   health {_health:0.00}   mood <b>{moodName}</b>   {state}" +
            (overrideHealth ? "   <color=yellow>(override)</color>" : ""), style);
        y += 22f;

        foreach (var l in layers)
        {
            if (l == null) continue;
            int bars = Mathf.RoundToInt(l.currentVolume * 20f);
            string bar = new string('|', bars) + new string('.', 20 - bars);
            GUI.Label(new Rect(10, y, 600, 20), $"{l.name,-12} {l.currentVolume:0.00} [{bar}]", style);
            y += 20f;
        }
    }
}
