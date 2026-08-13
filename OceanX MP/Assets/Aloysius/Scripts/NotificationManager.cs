using UnityEngine;
using TMPro;
using System.Collections;

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance;
    public TextMeshProUGUI messageText;

        public float showSeconds = 4f;

    [Header("Animation")]
        public float inDuration = 0.35f;
        public float outDuration = 0.3f;
        public float slideDistance = 60f;

        public bool autoSubscribeUnlock = true;

        public GameObject panel;

    private CanvasGroup _cg;
    private RectTransform _panelRt;
    private Vector2 _restPos;
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
            _restPos = _panelRt.anchoredPosition;
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
        if (UISoundManager.Instance != null) UISoundManager.Instance.Play(UISound.Unlock);
        if (messageText != null)
            messageText.text = s.speciesName;

        var target = panel != null ? panel : gameObject;
        target.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(PlaySequence());
    }

    IEnumerator PlaySequence()
    {

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

        yield return new WaitForSecondsRealtime(showSeconds);

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

    public void ClearAll()
    {
        StopAllCoroutines();
        EnsureRefs();
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
