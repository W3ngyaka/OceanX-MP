using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Alucia's voice-over player. Lives on the big screen (Host / Trifold), beside AluciaController.
///
/// Deliberately its own component rather than reusing either existing audio system:
///   • UISoundManager is tablet-only and models short interchangeable UI blips.
///   • AdaptiveMusicSystem is a mood-band music crossfader with hysteresis and an 8s minimum
///     dwell — the opposite of interrupting dialogue — and it cannot report a clip's length.
///
/// That length is the whole point. <see cref="TryPlay"/> returns the clip's duration so the
/// speech bubble can be held for exactly as long as she is actually speaking, instead of the
/// fixed autoHideSeconds that currently over- or under-runs every line.
///
/// It returns -1 when there is no clip, and that is what makes PARTIAL voice coverage safe:
/// any line without audio silently keeps the old timed behaviour, so the set can be filled in
/// a few clips at a time without leaving the exhibit in a broken half-state.
/// </summary>
public class AluciaVoice : MonoBehaviour
{
    public static AluciaVoice Instance { get; private set; }

    [Tooltip("Every Alucia voice clip. Select the whole Assets/Sounds/Alucia folder and drag it " +
             "in once — clips are matched to the CSV's Audio column by file name, so the order " +
             "here does not matter and new clips only need dropping in.")]
    public AudioClip[] clips;

    [Range(0f, 1f)] public float volume = 1f;

    [Tooltip("Warn the first time a line asks for a clip name that isn't in the list above. " +
             "Leave this on while filling in the CSV — it is how you catch typos in the Audio " +
             "column, which would otherwise fail silently as 'no VO for this line'.")]
    public bool warnOnMissingClip = true;

    private AudioSource _source;
    private readonly Dictionary<string, AudioClip> _byName =
        new Dictionary<string, AudioClip>(System.StringComparer.OrdinalIgnoreCase);
    // One warning per bad name, not one per occurrence — a missing hint.flavour clip would
    // otherwise spam the console every time that hint fires.
    private readonly HashSet<string> _warned = new HashSet<string>();

    void Awake()
    {
        Instance = this;

        _source = gameObject.AddComponent<AudioSource>();
        _source.playOnAwake = false;
        _source.loop = false;
        _source.spatialBlend = 0f;   // 2D: she narrates the scene, she isn't positioned in it

        if (clips != null)
        {
            foreach (AudioClip c in clips)
            {
                if (c == null) continue;
                if (!_byName.ContainsKey(c.name)) _byName.Add(c.name, c);
                else Debug.LogWarning($"[AluciaVoice] Two clips named '{c.name}' — keeping the first.", this);
            }
        }
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    /// <summary>
    /// Play a line's voice clip. Returns its length in seconds, or -1 when the name is blank
    /// or no matching clip is loaded — callers should fall back to their own timing on -1.
    /// A new line cuts off the previous one, mirroring how Say() replaces the bubble text.
    /// </summary>
    public float TryPlay(string clipName)
    {
        if (_source == null || string.IsNullOrWhiteSpace(clipName)) return -1f;

        clipName = clipName.Trim();
        // Tolerate an extension in the sheet ("shark starving.wav"); AudioClip.name has none.
        int dot = clipName.LastIndexOf('.');
        if (dot > 0) clipName = clipName.Substring(0, dot);

        if (!_byName.TryGetValue(clipName, out AudioClip clip) || clip == null)
        {
            if (warnOnMissingClip && _warned.Add(clipName))
                Debug.LogWarning($"[AluciaVoice] No clip named '{clipName}'. Check the CSV's " +
                                 $"Audio column against the file names in Assets/Sounds/Alucia.", this);
            return -1f;
        }

        _source.Stop();
        _source.clip = clip;
        _source.volume = volume;
        _source.Play();
        return clip.length;
    }

    /// <summary>Cut her off — used on exhibit reset so a new visitor doesn't walk into the
    /// tail of the previous session's line.</summary>
    public void StopSpeaking()
    {
        if (_source != null) _source.Stop();
    }

    public bool IsSpeaking => _source != null && _source.isPlaying;
}
