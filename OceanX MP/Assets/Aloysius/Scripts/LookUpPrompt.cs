using UnityEngine;
using System.Collections;

public class LookUpPrompt : MonoBehaviour
{
    public static LookUpPrompt Instance { get; private set; }

    [Header("Refs")]
    public CanvasGroup group;

    [Header("Timing")]
        public float holdDuration = 2.5f;
    public float fadeDuration = 0.35f;

        public bool suppressWhileTutorialOpen = true;

    private Coroutine _fade;
    private Coroutine _hide;
    private bool _visible;

    void Awake()
    {
        Instance = this;
        if (group == null) group = GetComponent<CanvasGroup>();
        if (group != null) { group.alpha = 0f; group.blocksRaycasts = false; group.interactable = false; }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static bool IsShowing =>
        Instance != null && Instance.group != null && Instance.group.alpha > 0.01f;

    public static void Trigger()
    {
        if (Instance != null) Instance.Show();
    }

    public void Show()
    {
        if (group == null) return;
        if (suppressWhileTutorialOpen && TutorialPanel.Instance != null && TutorialPanel.Instance.IsOpenOrPending) return;

        if (_hide != null) { StopCoroutine(_hide); _hide = null; }
        if (!_visible)
        {
            _visible = true;
            Fade(1f);

            if (UISoundManager.Instance != null) UISoundManager.Instance.Play(UISound.Notification);
        }
        _hide = StartCoroutine(HideAfterHold());
    }

    IEnumerator HideAfterHold()
    {

        float t = 0f;
        while (t < holdDuration)
        {
            if (suppressWhileTutorialOpen && TutorialPanel.Instance != null && TutorialPanel.Instance.IsOpenOrPending)
                break;
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        _hide = null;
        _visible = false;
        Fade(0f);
    }

    public void ResetForNewSession()
    {
        StopAllCoroutines();
        _fade = null;
        _hide = null;
        _visible = false;
        if (group != null) { group.alpha = 0f; group.blocksRaycasts = false; group.interactable = false; }
    }

    void Fade(float target)
    {
        if (group == null) return;
        if (_fade != null) StopCoroutine(_fade);
        _fade = StartCoroutine(FadeTo(target));
    }

    IEnumerator FadeTo(float target)
    {
        float from = group.alpha, t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, fadeDuration);
            group.alpha = Mathf.Lerp(from, target, t);
            yield return null;
        }
        group.alpha = target;
    }
}
