using UnityEngine;
using TMPro;
using System.Collections;

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance;
    public TextMeshProUGUI messageText;

    [Tooltip("Seconds the toast stays fully shown before auto-hiding (excludes the fades).")]
    public float showSeconds = 4f;

    [Header("Animation")]
    [Tooltip("Seconds for the slide-in + fade-in.")]
    public float inDuration = 0.35f;
    [Tooltip("Seconds for the fade-out + slide-out.")]
    public float outDuration = 0.3f;
    [Tooltip("How far (px) the panel slides up from as it appears.")]
    public float slideDistance = 60f;

    [Tooltip("If true, this toast auto-subscribes to the unlock event and pops itself.")]
    public bool autoSubscribeUnlock = true;

    [Tooltip("The panel to show/hide. If unset, falls back to this GameObject (legacy behavior).")]
    public GameObject panel;

    private CanvasGroup _cg;
    private RectTransform _panelRt;
    private Vector2 _restPos;      // authored resting position
    private bool _posCaptured;

    void Awake()
    {
        Instance = this;
        EnsureRefs();
        HideInstant();
    }

    void EnsureRefs()
    {
        var target = panel != null ? panel : gameObject;
        _panelRt = target.GetComponent<RectTransform>();
        _cg = target.GetComponent<CanvasGroup>();
        if (_cg == null) _cg = target.AddComponent<CanvasGroup>();
        if (!_posCaptured && _panelRt != null)
        {
            _restPos = _panelRt.anchoredPosition; // remember where the designer placed it
            _posCaptured = true;
        }
    }

    void OnEnable()
    {
        if (autoSubscribeUnlock && EcosystemUnlockManagerGPU.Instance != null)
            EcosystemUnlockManagerGPU.Instance.OnSpeciesUnlocked += ShowUnlocked;
    }

    void Start()
    {
        if (autoSubscribeUnlock && EcosystemUnlockManagerGPU.Instance != null)
        {
            EcosystemUnlockManagerGPU.Instance.OnSpeciesUnlocked -= ShowUnlocked;
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
        EnsureRefs();
        if (messageText != null)
            messageText.text = s.speciesName;   // just the organism name

        var target = panel != null ? panel : gameObject;
        target.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(PlaySequence());
    }

    IEnumerator PlaySequence()
    {
        // --- slide up + fade in ---
        float t = 0f;
        Vector2 from = _restPos + Vector2.down * slideDistance;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, inDuration);
            float e = EaseOutCubic(Mathf.Clamp01(t));
            _cg.alpha = e;
            _panelRt.anchoredPosition = Vector2.LerpUnclamped(from, _restPos, e);
            yield return null;
        }
        _cg.alpha = 1f;
        _panelRt.anchoredPosition = _restPos;

        // --- hold ---
        yield return new WaitForSecondsRealtime(showSeconds);

        // --- fade out + slide down ---
        t = 0f;
        Vector2 to = _restPos + Vector2.down * (slideDistance * 0.5f);
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, outDuration);
            float e = EaseInCubic(Mathf.Clamp01(t));
            _cg.alpha = 1f - e;
            _panelRt.anchoredPosition = Vector2.LerpUnclamped(_restPos, to, e);
            yield return null;
        }
        HideInstant();
    }

    void HideInstant()
    {
        if (_cg != null) _cg.alpha = 0f;
        if (_panelRt != null && _posCaptured) _panelRt.anchoredPosition = _restPos;
        var target = panel != null ? panel : gameObject;
        target.SetActive(false);
    }

    static float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - x, 3f);
    static float EaseInCubic(float x) => x * x * x;
}
