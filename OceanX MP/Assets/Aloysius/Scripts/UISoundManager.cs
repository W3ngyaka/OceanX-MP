using System.Collections.Generic;
using UnityEngine;

public enum UISound
{
    Tap,
    Add,
    Remove,
    Locked,
    Disabled,
    TabSwitch,
    ModalOpen,
    ModalClose,
    Unlock,
    SpeciesAdded,
    Notification,
    Warning,
    Win,
    Start,
    TutorialNext
}

public class UISoundManager : MonoBehaviour
{
    public static UISoundManager Instance;

    [System.Serializable]
    public class Entry
    {
        public UISound id;
                public AudioClip[] variants;
        [Range(0f, 1f)] public float volume = 1f;
                [Range(0f, 0.5f)] public float pitchJitter = 0f;
    }

    [Header("Master")]
    [Range(0f, 1f)] public float masterVolume = 1f;
        public bool muted = false;

    [Header("Sounds")]
        public List<Entry> sounds = new List<Entry>();

    [Header("Voices")]
        [Range(1, 12)] public int voiceCount = 4;

    [Header("Auto-load")]
        public bool autoLoadFromResources = true;
    public string resourcesFolder = "UISounds";

    [Header("Legacy (migrated at runtime)")]
        public AudioClip tapSound;
    [Range(0f, 1f)] public float tapVolume = 1f;

    private readonly Dictionary<UISound, Entry> _lookup = new Dictionary<UISound, Entry>();
    private AudioSource[] _voices;
    private int _nextVoice;

    void Awake()
    {
        Instance = this;
        BuildVoicePool();
        BuildLookup();
        if (autoLoadFromResources) AutoLoadMissing();
    }

    void AutoLoadMissing()
    {
        string folder = string.IsNullOrEmpty(resourcesFolder) ? "" : resourcesFolder.TrimEnd('/') + "/";
        foreach (UISound id in System.Enum.GetValues(typeof(UISound)))
        {
            if (_lookup.TryGetValue(id, out var existing) &&
                existing.variants != null && existing.variants.Length > 0 && existing.variants[0] != null)
                continue;

            var clip = Resources.Load<AudioClip>(folder + id.ToString());
            if (clip == null) continue;

            if (existing == null)
            {
                existing = new Entry { id = id };
                _lookup[id] = existing;
            }
            existing.variants = new[] { clip };
        }
    }

    void BuildVoicePool()
    {
        _voices = new AudioSource[Mathf.Max(1, voiceCount)];
        for (int i = 0; i < _voices.Length; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            src.loop = false;
            _voices[i] = src;
        }
    }

    void BuildLookup()
    {
        _lookup.Clear();
        foreach (var e in sounds)
        {
            if (e != null && !_lookup.ContainsKey(e.id))
                _lookup.Add(e.id, e);
        }

        if (tapSound != null)
        {
            if (!_lookup.TryGetValue(UISound.Tap, out var tapEntry))
            {
                tapEntry = new Entry { id = UISound.Tap };
                _lookup.Add(UISound.Tap, tapEntry);
            }
            if (tapEntry.variants == null || tapEntry.variants.Length == 0)
            {
                tapEntry.variants = new[] { tapSound };
                tapEntry.volume = tapVolume;
            }
        }
    }

    public void Play(UISound sound, float volumeScale = 1f)
    {
        if (muted || _voices == null) return;
        // Fall back to the generic Tap click if a specific sound has no clip assigned yet
        // (lets TutorialNext etc. work out of the box before a custom clip is added).
        if (!_lookup.TryGetValue(sound, out var e) || e.variants == null || e.variants.Length == 0)
        {
            if (sound != UISound.Tap && _lookup.TryGetValue(UISound.Tap, out var tap)
                && tap.variants != null && tap.variants.Length > 0)
                e = tap;
            else
                return;
        }

        AudioClip clip = e.variants.Length == 1
            ? e.variants[0]
            : e.variants[Random.Range(0, e.variants.Length)];
        if (clip == null) return;

        AudioSource src = NextVoice();
        src.pitch = e.pitchJitter > 0f
            ? 1f + Random.Range(-e.pitchJitter, e.pitchJitter)
            : 1f;
        src.PlayOneShot(clip, e.volume * masterVolume * Mathf.Clamp01(volumeScale));
    }

    AudioSource NextVoice()
    {
        AudioSource src = _voices[_nextVoice];
        _nextVoice = (_nextVoice + 1) % _voices.Length;
        return src;
    }

    public void PlayTap() => Play(UISound.Tap);

#if UNITY_EDITOR

    void Reset() => AddMissingSlots();

    [ContextMenu("Add Missing Sound Slots")]
    void AddMissingSlots()
    {
        foreach (UISound value in System.Enum.GetValues(typeof(UISound)))
        {
            if (!sounds.Exists(s => s != null && s.id == value))
                sounds.Add(new Entry { id = value });
        }
    }
#endif
}
