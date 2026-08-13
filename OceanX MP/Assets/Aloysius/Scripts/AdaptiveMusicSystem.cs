using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;
using OceanX.BoidsGPU.Ecosystem;

public class AdaptiveMusicSystem : MonoBehaviour
{
    [System.Serializable]
    public class Layer
    {
        public string name = "Mood";
                public AudioClip clip;

                [Range(0f, 1f)] public float volume = 1f;

                public AudioMixerGroup group;

                public string exposedParam;

        [System.NonSerialized] public AudioSource source;
        [System.NonSerialized] public float currentVolume;
    }

    [Header("Mood songs (LOW health first)")]
    public List<Layer> layers = new List<Layer>();

        public float[] bandEdges = new float[] { 0.30f, 0.60f, 0.90f };

    [Header("Mix")]
        public AudioMixer mixer;

    [Range(0f, 1f)]
        public float masterVolume = 0.6f;

        public string masterExposedParam = "";

    [Header("Transition feel")]
        [Min(0f)]
    [FormerlySerializedAs("responseSeconds")]
    [FormerlySerializedAs("transitionSeconds")]
    public float crossfadeSeconds = 3f;

        [Min(0f)]
    public float healthSmoothing = 2f;

    [Range(0f, 0.25f)]
        public float hysteresis = 0.05f;

    [Min(0f)]
        public float minMoodSeconds = 8f;

        public float silenceDb = -80f;

    [Header("Event swell (e.g. a species arrives)")]
    public AudioClip swell;
    [Range(0f, 1f)] public float swellVolume = 0.8f;

    [Header("Debug")]
        public bool overrideHealth = false;
    [Range(0f, 1f)] public float debugHealth = 0.5f;
        public bool showDebugReadout = false;

    public static AdaptiveMusicSystem Instance { get; private set; }

    private AudioSource _swellSource;
    private EcosystemSimulationGPU _sim;
    private float _health = 0f;
    private bool  _started;

    private int   _currentBand = 0;
    private int   _fadeFrom    = -1;
    private int   _fadeTo      = -1;
    private float _fadeT       = 1f;
    private bool  _fading;
    private float _moodTimer;

    public float CurrentHealth => _health;

    void Awake()
    {

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {

        foreach (var l in layers)
        {
            if (l == null || l.clip == null) continue;
            var src = gameObject.AddComponent<AudioSource>();
            src.clip = l.clip;
            src.loop = true;
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            src.volume = 1f;
            if (l.group != null) src.outputAudioMixerGroup = l.group;
            l.source = src;
        }

        NormaliseBandEdges();

        _health = overrideHealth ? debugHealth : ReadHealth();
        int startBand = NominalBand(_health);

        _swellSource = gameObject.AddComponent<AudioSource>();
        _swellSource.playOnAwake = false;
        _swellSource.loop = false;
        _swellSource.spatialBlend = 0f;

        _currentBand = startBand;
        _started = true;

        BeginTransition(startBand, fromSilence: true);
    }

    void Update()
    {
        if (!_started) return;

        float raw = overrideHealth ? debugHealth : ReadHealth();

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

        var to = LayerAt(_fadeTo);
        if (to?.source != null) { to.source.time = 0f; if (!to.source.isPlaying) to.source.Play(); }

        if (_fadeT >= 1f) FinishCrossfade();
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

    void ApplyLevels()
    {
        bool masterOnMixer = mixer != null && !string.IsNullOrEmpty(masterExposedParam);
        if (masterOnMixer) mixer.SetFloat(masterExposedParam, LinearToDb(masterVolume));

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

    int EdgeCount() => Mathf.Max(0, layers.Count - 1);

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

    int NominalBand(float h)
    {
        int edges = EdgeCount();
        for (int i = 0; i < edges; i++)
            if (h < bandEdges[i]) return i;
        return Mathf.Max(0, layers.Count - 1);
    }

    Layer LayerAt(int i) => (i >= 0 && i < layers.Count) ? layers[i] : null;
    bool HasClip(int i)  { var l = LayerAt(i); return l != null && l.clip != null; }

    float LinearToDb(float linear)
    {
        if (linear <= 0.0001f) return silenceDb;
        return Mathf.Max(silenceDb, Mathf.Log10(linear) * 20f);
    }

    float ReadHealth()
    {
        if (_sim == null) _sim = FindFirstObjectByType<EcosystemSimulationGPU>();
        if (_sim != null) return Mathf.Clamp01(_sim.EcoHealth01);
        return _health;
    }

    public void PlaySwell()
    {
        if (swell != null && _swellSource != null)
            _swellSource.PlayOneShot(swell, swellVolume * masterVolume);
    }

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
