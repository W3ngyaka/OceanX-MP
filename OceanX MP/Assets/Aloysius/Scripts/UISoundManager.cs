using UnityEngine;

// Tiny shared player for UI sound effects. Drop your tap clip into the Inspector slot
// on the one UISoundManager object in the scene; SpeciesBubble calls PlayTap() on tap.
[RequireComponent(typeof(AudioSource))]
public class UISoundManager : MonoBehaviour
{
    public static UISoundManager Instance;

    [Header("Clips")]
    [Tooltip("Played when a species bubble is tapped.")]
    public AudioClip tapSound;
    [Range(0f, 1f)] public float tapVolume = 1f;

    private AudioSource _source;

    void Awake()
    {
        Instance = this;
        _source = GetComponent<AudioSource>();
        _source.playOnAwake = false;
    }

    public void PlayTap()
    {
        if (tapSound != null && _source != null)
            _source.PlayOneShot(tapSound, tapVolume);
    }
}
