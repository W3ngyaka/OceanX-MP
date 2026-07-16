using System.Collections.Generic;
using UnityEngine;
using OceanX.BoidsGPU.Ecosystem;

// Adaptive music. ONE track plays at a time, chosen by live ecosystem health; when the health moves
// into another track's band the two CROSS-FADE — the incoming track fades up while the outgoing
// fades down — so there is never a hard cut and never two songs sitting on top of each other.
//
// WHY IT WORKS THIS WAY
//  The earlier version layered every stem at once and mixed them by health. That is the right model
//  for STEMS (parts of one arrangement, same tempo + key, written to be heard together). These clips
//  are three separate complete songs, so layering them just muddies. Hence: exclusive bands, one
//  winner, time-based cross-fade.
//
// SETUP
//  1. Give each track an EXCLUSIVE health band — they must not overlap, e.g.
//       "Somber"   0.00 - 0.45
//       "Warmth"   0.45 - 0.75
//       "Thriving" 0.75 - 1.00
//     First matching band wins, so do not add an "always on" 0-1 layer here — it would swallow
//     everything. An ambient bed belongs on its own plain looping AudioSource, not in this list.
//  2. Drop this on ONE object (e.g. AudioManager) on the LARGE SCREEN scene.
//  3. Call PlaySwell() on a positive beat (e.g. a species arriving) for a momentary lift.
public class MusicDirector : MonoBehaviour
{
    /// <summary>
    /// Shape of the fade. This matters more than you'd expect: AudioSource.volume is LINEAR AMPLITUDE
    /// but hearing is LOGARITHMIC, so volume 0.5 is only -6dB — it already sounds nearly full. A linear
    /// ramp therefore reaches "loud" in the first ~20% of its duration and spends the rest crawling
    /// through changes you can barely hear. It sounds instant and harsh no matter how long you set it.
    /// </summary>
    public enum FadeCurve
    {
        /// <summary>Raw linear amplitude. Technically a fade; sounds like a snap. Here for comparison.</summary>
        Linear,
        /// <summary>volume = t². Slow, quiet onset that matches how hearing works — a fade you can
        /// actually hear taking its time. Dips slightly in the middle of a crossfade, which between two
        /// DIFFERENT songs usually reads as a pleasant breath rather than a hole.</summary>
        Smooth,
        /// <summary>volume = sin(t·π/2). Holds TOTAL loudness constant across a crossfade (no dip), which
        /// is the classic DJ-style blend. Rises faster than Smooth, so the incoming track asserts itself
        /// earlier — best when the two tracks share a key/tempo.</summary>
        EqualPower
    }

    /// <summary>What an incoming track does when it takes over. Switchable live, in Play mode, to A/B it.</summary>
    public enum TakeoverMode
    {
        /// <summary>Incoming track restarts from its first bar. You always hear the song as written,
        /// but flipping back and forth across a band edge re-triggers it each time.</summary>
        RestartFromBeginning,
        /// <summary>Every track runs silently from launch; taking over only fades its volume up, so it
        /// resumes mid-song. Never jarring, never restarts — but you rarely hear a song's opening,
        /// and all clips decode continuously.</summary>
        CarryOn
    }

    [System.Serializable]
    public class Layer
    {
        public string name = "Layer";
        public AudioClip clip;

        [Tooltip("Ecosystem-health band (0-1) this track owns. Bands must NOT overlap — first match wins.")]
        [Range(0f, 1f)] public float healthMin = 0f;
        [Range(0f, 1f)] public float healthMax = 1f;

        [Tooltip("This track's volume when fully faded in (before Master Volume).")]
        [Range(0f, 1f)] public float maxVolume = 1f;

        [HideInInspector] public AudioSource source;

        /// <summary>How far this track is faded in, 0-1. The FADE CURVE maps this to an actual volume —
        /// we ramp THIS linearly over time (so the seconds mean what they say) and shape it on the way
        /// out, rather than ramping the volume directly.</summary>
        [System.NonSerialized] public float fade;

        /// <summary>Seconds this track still has to hold before it may start fading out. Set from
        /// Fade Out Delay the moment it stops being the winner; counts down to 0, then the fade runs.</summary>
        [System.NonSerialized] public float outDelay;
    }

    [Header("Tracks (exclusive, non-overlapping health bands)")]
    public List<Layer> layers = new List<Layer>();

    [Header("Mix")]
    [Range(0f, 1f)] public float masterVolume = 0.7f;

    [Header("Transition")]
    [Tooltip("What the incoming track does when it takes over. Safe to change during Play to compare.")]
    public TakeoverMode takeoverMode = TakeoverMode.RestartFromBeginning;

    [Tooltip("Shape of the fade. Start with Smooth — Linear is technically a fade but sounds like a snap, " +
             "because volume 0.5 is already -6dB (nearly full loudness to your ear). Safe to change in Play.")]
    public FadeCurve fadeCurve = FadeCurve.Smooth;

    [Tooltip("SECONDS for the incoming track to rise from silence to its full volume.")]
    [Min(0f)] public float fadeInSeconds = 2f;

    [Tooltip("SECONDS for the outgoing track to fall from its full volume to silence. This is the fall " +
             "itself, NOT when it starts — see Fade Out Delay for that.")]
    [Min(0f)] public float fadeOutSeconds = 2.5f;

    [Tooltip("SECONDS the outgoing track HOLDS at its level before it begins fading out. The incoming " +
             "track always starts rising immediately, so this is the new song's head start:\n\n" +
             "  0    = CROSSFADE — both move together, they trade places\n" +
             "  > 0  = OVERLAP   — the new song arrives OVER the old one, which then bows out underneath\n\n" +
             "Warning: during the hold BOTH tracks are near full, so the mix genuinely gets louder — " +
             "that is inherent to this shape, not a bug. Drop Master Volume if it peaks.")]
    [Min(0f)] public float fadeOutDelaySeconds = 0f;

    [Tooltip("Health must move this far PAST a band edge before the track switches. Stops the music " +
             "flip-flopping when health parks exactly on a boundary. 0 = switch the instant it crosses.")]
    [Range(0f, 0.2f)] public float switchHysteresis = 0.03f;

    [Header("Event swell (e.g. a species arrives)")]
    public AudioClip swell;
    [Range(0f, 1f)] public float swellVolume = 0.8f;

    [Header("Debug")]
    [Tooltip("Ignore the live ecosystem and drive the music from the slider below (for auditioning).")]
    public bool overrideHealth = false;
    [Range(0f, 1f)] public float debugHealth = 0.5f;

    /// <summary>The director in the scene, so events can fire swells without wiring a reference.</summary>
    public static MusicDirector Instance { get; private set; }

    private AudioSource _swellSource;
    private EcosystemSimulationGPU _sim;
    private float _health = 0.5f;
    private int   _current = -1;              // index of the track that currently owns the mix
    private TakeoverMode _lastMode;

    /// <summary>Current health the music is following (for debugging / UI).</summary>
    public float CurrentHealth => _health;

    /// <summary>Name of the track currently fading in / holding. "" when silent.</summary>
    public string CurrentTrack => (_current >= 0 && _current < layers.Count && layers[_current] != null)
        ? layers[_current].name : "";

    void Awake()
    {
        // Singleton guard: if a MusicDirector already survives (e.g. carried from another scene, or a
        // second scene loaded additively brought its own AudioManager), destroy this duplicate so we
        // never stack two sets of tracks playing over each other.
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
        // One AudioSource per track. All start silent; Update decides who is audible.
        foreach (var l in layers)
        {
            if (l == null || l.clip == null) continue;   // an unassigned clip = that track simply doesn't exist
            var src = gameObject.AddComponent<AudioSource>();
            src.clip = l.clip;
            src.loop = true;
            src.playOnAwake = false;
            src.volume = 0f;
            l.source = src;
        }

        _lastMode = takeoverMode;
        if (takeoverMode == TakeoverMode.CarryOn) PlayAll();

        _swellSource = gameObject.AddComponent<AudioSource>();
        _swellSource.playOnAwake = false;
        _swellSource.loop = false;
    }

    void Update()
    {
        _health = overrideHealth ? debugHealth : ReadHealth();

        // Mode flipped in the Inspector mid-Play (this is meant to be A/B'd live).
        if (takeoverMode != _lastMode)
        {
            _lastMode = takeoverMode;
            if (takeoverMode == TakeoverMode.CarryOn) PlayAll();
        }

        UpdateWinner(_health);

        for (int i = 0; i < layers.Count; i++)
        {
            var l = layers[i];
            if (l?.source == null) continue;

            // Ramp PROGRESS (0-1) linearly in time, so "Fade In Seconds = 7" is honestly seven seconds
            // from silent to full regardless of the curve or how loud this track is. The curve is then
            // applied on the way out to the actual volume — see FadeCurve for why that matters.
            float targetFade = (i == _current) ? 1f : 0f;

            if (i != _current && l.outDelay > 0f)
            {
                // Dethroned, but HOLDING: this track stands at its level while the incoming one rises,
                // and only starts bowing out once the delay expires. That is what makes the new song
                // arrive over the top of the old one instead of trading places with it.
                l.outDelay -= Time.unscaledDeltaTime;
            }
            else
            {
                float seconds = (targetFade > l.fade) ? fadeInSeconds : fadeOutSeconds;
                float rate    = (seconds <= 0.001f) ? 1000f : 1f / seconds;
                l.fade = Mathf.MoveTowards(l.fade, targetFade, rate * Time.unscaledDeltaTime);
            }

            // Full = what this track sits at once faded in, i.e. its own level — not 1.0.
            float full = l.maxVolume * masterVolume;
            l.source.volume = full * ShapeFade(l.fade);

            // Once an outgoing track is silent, stop it so it isn't decoding for nothing. Only in
            // Restart mode — CarryOn deliberately keeps every clip rolling so it can resume mid-song.
            if (takeoverMode == TakeoverMode.RestartFromBeginning &&
                i != _current && l.source.isPlaying && l.fade <= 0f)
            {
                l.source.Stop();
            }
        }
    }

    // Maps linear fade progress (0-1) to an actual volume multiplier. See FadeCurve: a raw linear ramp
    // spends most of its time in a range your ear can barely tell apart, which is why a "7 second" linear
    // fade sounds like an instant, over-loud cut.
    float ShapeFade(float t)
    {
        switch (fadeCurve)
        {
            case FadeCurve.Smooth:     return t * t;                          // quiet onset, gentle arrival
            case FadeCurve.EqualPower: return Mathf.Sin(t * Mathf.PI * 0.5f); // constant loudness across a blend
            default:                   return t;                              // Linear — for comparison
        }
    }

    // Decides which track should own the mix, with hysteresis so a health value sitting on a band
    // edge doesn't make the music stutter between two songs.
    void UpdateWinner(float h)
    {
        if (_current >= 0 && _current < layers.Count)
        {
            Layer cur = layers[_current];
            if (cur?.source != null &&
                h >= cur.healthMin - switchHysteresis &&
                h <= cur.healthMax + switchHysteresis)
            {
                return;   // health hasn't clearly left the current track's band — hold it
            }
        }

        int winner = PickWinner(h);
        if (winner == _current) return;

        // Give the track we're leaving its hold time BEFORE we dethrone it, so it stands at its
        // current level while the newcomer rises rather than starting to leave straight away.
        if (_current >= 0 && _current < layers.Count && layers[_current] != null)
            layers[_current].outDelay = fadeOutDelaySeconds;

        _current = winner;

        // The newcomer starts rising immediately; the old one holds for Fade Out Delay first.
        // No-ops when winner < 0 (health outside every band — everything simply fades out).
        BeginTrack(winner);
    }

    // First band containing h wins, so bands are expected to be exclusive and in order.
    // Tracks with no clip were never given a source and are skipped entirely.
    int PickWinner(float h)
    {
        for (int i = 0; i < layers.Count; i++)
        {
            Layer l = layers[i];
            if (l?.source == null) continue;
            if (h >= l.healthMin && h <= l.healthMax) return i;
        }
        return -1;   // health falls outside every band — everything fades out
    }

    void BeginTrack(int index)
    {
        if (index < 0 || index >= layers.Count) return;
        Layer l = layers[index];
        if (l?.source == null) return;

        // This track is the winner now, so cancel any hold it was serving from a previous demotion
        // (health can bounce back across a band edge while the old track is still standing).
        l.outDelay = 0f;

        if (takeoverMode == TakeoverMode.RestartFromBeginning)
        {
            l.source.Stop();        // resets the playback position to the top of the clip
            l.fade = 0f;            // ...so the fade-in genuinely starts from silence
            l.source.volume = 0f;
            l.source.Play();
        }
        else if (!l.source.isPlaying)
        {
            l.source.Play();        // CarryOn: it should already be rolling; safety net
        }
    }

    void PlayAll()
    {
        foreach (var l in layers)
            if (l?.source != null && !l.source.isPlaying) l.source.Play();
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

    /// <summary>One-shot lift on a positive beat (species arrives, health milestone).</summary>
    public void PlaySwell()
    {
        if (swell != null && _swellSource != null)
            _swellSource.PlayOneShot(swell, swellVolume * masterVolume);
    }
}
