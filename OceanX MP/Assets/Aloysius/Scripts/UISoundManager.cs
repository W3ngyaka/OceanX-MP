using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Every UI sound the tablet can fire. Add new events here, then assign clips
/// in the UISoundManager Inspector list. Order does not matter — entries are
/// matched by this id, so reordering the enum never breaks scene assignments.
/// </summary>
public enum UISound
{
    Tap,            // species bubble tapped / general select
    Add,            // [+] add a species
    Remove,         // [-] remove a species
    Locked,         // tapped a locked (padlock) bubble — can't add yet
    Disabled,       // pressed a greyed-out button (at cap / at 0)
    TabSwitch,      // Food Web / Organisms / Hints tab change
    ModalOpen,      // info card / View Details panel opens
    ModalClose,     // panel closes / swipe to close
    Unlock,         // a locked species unlocks (reveal)
    SpeciesAdded,   // first-time add "joining the reef" flourish
    Notification,   // hint / toast pops in
    Warning,        // Alucia flags off-balance (overpopulated / starving)
    Win,            // reef restored / 100%
    Start           // tap-to-start / attract
}

/// <summary>
/// Shared 2D player for UI sound effects on the tablet. Singleton — one per scene.
/// Callers use <see cref="Play(UISound)"/>; the legacy <see cref="PlayTap"/> still works.
/// </summary>
public class UISoundManager : MonoBehaviour
{
    public static UISoundManager Instance;

    [System.Serializable]
    public class Entry
    {
        public UISound id;
        [Tooltip("One or more clips. If more than one, a random variant plays each time " +
                 "(good for the Tap sound to avoid repetition).")]
        public AudioClip[] variants;
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("Random pitch spread, ± this amount (0 = no variation). ~0.05 keeps taps lively.")]
        [Range(0f, 0.5f)] public float pitchJitter = 0f;
    }

    [Header("Master")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Tooltip("Global mute (e.g. for a silent-exhibit mode).")]
    public bool muted = false;

    [Header("Sounds")]
    [Tooltip("One entry per UISound. Right-click the component → 'Add Missing Sound Slots' " +
             "to auto-fill any that are absent.")]
    public List<Entry> sounds = new List<Entry>();

    [Header("Voices")]
    [Tooltip("Pooled AudioSources so overlapping/rapid sounds don't cut each other off.")]
    [Range(1, 12)] public int voiceCount = 4;

    [Header("Auto-load")]
    [Tooltip("For any sound with no clip assigned above, load Resources/<folder>/<EnumName> " +
             "(e.g. Resources/UISounds/Add). Lets clips be dropped in a Resources folder with no " +
             "scene wiring. An Inspector-assigned clip always wins over the auto-loaded one.")]
    public bool autoLoadFromResources = true;
    public string resourcesFolder = "UISounds";

    // --- Legacy (kept so the old Inspector Tap.mp3 assignment isn't lost) ---
    [Header("Legacy (migrated at runtime)")]
    [Tooltip("Old single tap slot. If the Tap entry above has no clips, this is used instead.")]
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

    // For every UISound with no assigned clip, try Resources/<folder>/<EnumName>.
    // Assigned clips are left untouched, so the Inspector always wins.
    void AutoLoadMissing()
    {
        string folder = string.IsNullOrEmpty(resourcesFolder) ? "" : resourcesFolder.TrimEnd('/') + "/";
        foreach (UISound id in System.Enum.GetValues(typeof(UISound)))
        {
            if (_lookup.TryGetValue(id, out var existing) &&
                existing.variants != null && existing.variants.Length > 0 && existing.variants[0] != null)
                continue; // already has a clip — don't override

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
            src.spatialBlend = 0f;      // pure 2D UI
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

        // Migrate the legacy tap clip into the Tap entry if that entry is empty.
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

    /// <summary>Play a UI sound by event. No-op if no clip is assigned for it.</summary>
    public void Play(UISound sound, float volumeScale = 1f)
    {
        if (muted || _voices == null) return;
        if (!_lookup.TryGetValue(sound, out var e) || e.variants == null || e.variants.Length == 0)
            return;

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

    /// <summary>Legacy entry point — SpeciesBubble still calls this on tap.</summary>
    public void PlayTap() => Play(UISound.Tap);

#if UNITY_EDITOR
    // Fresh component → show every sound slot ready to assign.
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
