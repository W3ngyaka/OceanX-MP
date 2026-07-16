using UnityEngine;
using TMPro;
using System.Collections;

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance;
    public TextMeshProUGUI messageText;

    [Tooltip("Seconds the toast stays on screen before auto-hiding.")]
    public float showSeconds = 4f;

    [Tooltip("If true, this toast auto-subscribes to the unlock event and pops itself. "
           + "Enable on the tablet toast so it fires without the large-screen calling it.")]
    public bool autoSubscribeUnlock = true;

    // The visible content lives on a child so THIS object can stay active (needed to receive
    // the unlock event) while the toast panel itself hides/shows.
    [Tooltip("The panel to show/hide. If unset, falls back to this GameObject (legacy behavior).")]
    public GameObject panel;

    void Awake()
    {
        Instance = this;
        HidePanel();
    }

    void OnEnable()
    {
        if (autoSubscribeUnlock && EcosystemUnlockManagerGPU.Instance != null)
            EcosystemUnlockManagerGPU.Instance.OnSpeciesUnlocked += ShowUnlocked;
    }

    void Start()
    {
        // Late-bind in case the unlock manager spawned after us.
        if (autoSubscribeUnlock && EcosystemUnlockManagerGPU.Instance != null)
        {
            EcosystemUnlockManagerGPU.Instance.OnSpeciesUnlocked -= ShowUnlocked; // avoid double
            EcosystemUnlockManagerGPU.Instance.OnSpeciesUnlocked += ShowUnlocked;
        }
    }

    void OnDisable()
    {
        if (EcosystemUnlockManagerGPU.Instance != null)
            EcosystemUnlockManagerGPU.Instance.OnSpeciesUnlocked -= ShowUnlocked;
    }

    public void ShowUnlocked(SpeciesData s)
    {
        if (s == null) return;
        if (messageText != null)
            messageText.text = AluciaLines.Get("notify.unlocked", "You've unlocked the {species}!").Replace("{species}", s.speciesName);
        ShowPanel();
        StopAllCoroutines();
        StartCoroutine(AutoHide());
    }

    IEnumerator AutoHide()
    {
        yield return new WaitForSeconds(showSeconds);
        HidePanel();
    }

    void ShowPanel() { (panel != null ? panel : gameObject).SetActive(true); }
    void HidePanel() { if (panel != null) panel.SetActive(false); else gameObject.SetActive(false); }
}
