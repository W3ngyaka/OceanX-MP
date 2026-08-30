using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OceanX.BoidsGPU.Ecosystem;

public class AluciaController : MonoBehaviour
{
    public enum Mood { Calm, Warn, Win }

    [Header("Refs")]
    public CanvasGroup characterGroup;
    public Image characterImage;
    public CanvasGroup bubbleGroup;
    public TMP_Text bubbleText;

    [Header("Simulation (host)")]
        public EcosystemSimulationGPU simulation;

    [Header("Timing")]
    public float autoHideSeconds = 5.2f;
    public float minGapBetweenMessages = 1.5f;
    public float introStartDelay = 0.6f;

    [Header("Health bands (0-100%)")]
    public float criticalMax = 35f;
    public float healthyMin = 70f;
    public float hysteresis = 4f;

    [Header("Bubble sprites (per mood) \u2014 leave empty to keep the scene sprite")]
        public Sprite bubbleCalmSprite;
        public Sprite bubbleWarnSprite;

    [Header("Mood colours (bubble tint)")]

    public Color calmColor = Color.white;
    public Color warnColor = Color.white;
    public Color winColor  = Color.white;

    [Header("Mood sprites (Alucia poses) — leave empty to keep current")]
    public Sprite calmSprite;
    public Sprite warnSprite;
    public Sprite winSprite;

    [Header("Intro lines")]
    [TextArea] public string introLine1 = "Hey, my name's Alucia!";
    [TextArea] public string introLine2 = "As you can see, this ecosystem isn't doing too well...";
    [TextArea] public string introLine3 = "Please help me save it!";
    public float introLineGap = 3f;

    [Header("Start gating")]
        public bool waitForExperienceStart = true;

    private Image _bubbleBg;
    private float _lastMsgTime = -99f;
    private Coroutine _hideRoutine;
    private bool _sticky;

    private enum Band { Critical, Unstable, Healthy, Thriving }
    private Band _lastBand = Band.Critical;
    private bool _bandInit;

    private bool _started;
    private bool _introPlayed;
    private bool _tutorialDoneThisSession;
    private bool _subscribed;

    private bool _muted;

    void Awake()
    {
        if (bubbleGroup != null) { bubbleGroup.alpha = 0f; _bubbleBg = bubbleGroup.GetComponent<Image>(); }
        if (characterGroup != null) characterGroup.alpha = 0f;
    }

    void Start()
    {

        if (!waitForExperienceStart)
            HandleStarted();
    }

    void Update()
    {

        if (waitForExperienceStart && !_subscribed)
        {
            var net = EcosystemNetworkManagerGPU.Instance;
            if (net != null)
            {
                _subscribed = true;
                net.OnStarted += HandleStarted;
        net.OnTutorialDone += () => _tutorialDoneThisSession = true;
                if (net.HasStarted) HandleStarted();
            }
        }

        if (!_started) return;
        if (simulation == null) return;
        float h = Mathf.Clamp01(simulation.EcoHealth01) * 100f;
        EvaluateHealth(h);
    }

    void HandleStarted()
    {
        if (_started) return;
        _muted = false;
        _started = true;
        if (!_introPlayed)
        {
            _introPlayed = true;
            StartCoroutine(IntroSequence());
        }
    }

    void OnDestroy()
    {
        var net = EcosystemNetworkManagerGPU.Instance;
        if (net != null) net.OnStarted -= HandleStarted;
    }

    public void ResetForNewSession()
    {
        Debug.Log($"[Alucia] ResetForNewSession — hiding + re-arming (was started={_started}, introPlayed={_introPlayed}).", this);
        StopAllCoroutines();

        _muted = true;
        _started = false;
        _introPlayed = false;
        _tutorialDoneThisSession = false;
        // Re-arm the tutorial gate so the intro waits for the NEXT visitor's tutorial to finish.
        { var n = EcosystemNetworkManagerGPU.Instance; if (n != null) n.SetTutorialDoneRpc(false); }  // re-arm

        if (bubbleGroup != null) bubbleGroup.alpha = 0f;
        if (characterGroup != null) characterGroup.alpha = 0f;
        if (bubbleText != null) bubbleText.text = "";
        _sticky = false;
        _hideRoutine = null;
        _lastMsgTime = -99f;

        _lastBand = Band.Critical;
        _bandInit = false;
    }

    public void Say(string message, Mood mood = Mood.Calm, bool sticky = false)
    {
        Debug.Log($"[Alucia] Say(\"{message}\") started={_started} muted={_muted} sticky={sticky}.", this);
        if (_muted) return;
        if (Time.unscaledTime - _lastMsgTime < minGapBetweenMessages && !sticky) return;
        _lastMsgTime = Time.unscaledTime;
        _sticky = sticky;

        if (bubbleText != null) bubbleText.text = message;

        if (bubbleGroup != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)bubbleGroup.transform);
        if (_bubbleBg != null)
        {
            Sprite bs = MoodBubbleSprite(mood);
            if (bs != null) _bubbleBg.sprite = bs;
            _bubbleBg.color = MoodColor(mood);
        }
        if (characterImage != null)
        {
            Sprite s = MoodSprite(mood);
            if (s != null) characterImage.sprite = s;
        }

        if (_hideRoutine != null) StopCoroutine(_hideRoutine);
        StartCoroutine(FadeBoth(1f, 0.3f));

        if (!sticky) _hideRoutine = StartCoroutine(HideAfter(autoHideSeconds));
    }

    public void Hide()
    {
        StartCoroutine(FadeBoth(0f, 0.3f));
    }

    [Tooltip("Wait for the tablet's how-to panel to be dismissed before the intro plays.")]
    public bool waitForTutorial = true;

    IEnumerator IntroSequence()
    {
        // Hold the intro until the visitor taps GOT IT! on the tablet tutorial, so Alucia
        // isn't talking over the how-to panel. Falls through if the flag's already set.
        if (waitForTutorial)
        {
            // Wait for the tutorial to be completed THIS session (event-driven), so a stale
            // 'done=true' left over from the previous visitor doesn't let the intro start early.
            while (!_tutorialDoneThisSession)
                yield return null;
        }

        yield return new WaitForSeconds(introStartDelay);
        Say(AluciaLines.Get("intro.1", introLine1), Mood.Calm);
        yield return new WaitForSeconds(introLineGap);
        Say(AluciaLines.Get("intro.2", introLine2), Mood.Warn);
        yield return new WaitForSeconds(introLineGap);
        Say(AluciaLines.Get("intro.3", introLine3), Mood.Calm);
    }

    void EvaluateHealth(float h)
    {
        Band band = BandFor(h);

        if (!_bandInit)
        {
            _lastBand = band;
            _bandInit = true;
            return;
        }

        if (band == _lastBand) return;

        bool improving = band > _lastBand;
        _lastBand = band;

        switch (band)
        {
            case Band.Critical:
                Say(AluciaLines.Get("health.critical", "The reef is collapsing — we're losing species fast!"), Mood.Warn);
                break;
            case Band.Unstable:
                if (improving) Say(AluciaLines.Get("health.unstable.up", "It's stabilising a little... keep going!"), Mood.Calm);
                else Say(AluciaLines.Get("health.unstable.down", "Things are slipping — the balance is breaking down."), Mood.Warn);
                break;
            case Band.Healthy:
                Say(AluciaLines.Get("health.healthy", "The reef's looking much healthier now!"), Mood.Calm);
                break;
            case Band.Thriving:
                Say(AluciaLines.Get("health.thriving", "You did it — the ecosystem is thriving again! \ud83e\udea8"), Mood.Win, sticky: true);
                break;
        }
    }

    Band BandFor(float h)
    {

        float critLine = criticalMax + (_lastBand == Band.Critical ? hysteresis : -hysteresis);
        float healLine = healthyMin + (_lastBand == Band.Healthy || _lastBand == Band.Thriving ? -hysteresis : hysteresis);

        if (h >= 100f) return Band.Thriving;
        if (h >= healLine) return Band.Healthy;
        if (h <= critLine) return Band.Critical;
        return Band.Unstable;
    }

    Sprite MoodSprite(Mood m)
    {
        switch (m)
        {
            case Mood.Warn: return warnSprite;
            case Mood.Win:  return winSprite;
            default:        return calmSprite;
        }
    }

    Sprite MoodBubbleSprite(Mood m)
    {
        return m == Mood.Warn ? bubbleWarnSprite : bubbleCalmSprite;
    }

    Color MoodColor(Mood m)
    {
        switch (m)
        {
            case Mood.Warn: return warnColor;
            case Mood.Win:  return winColor;
            default:        return calmColor;
        }
    }

    IEnumerator HideAfter(float secs)
    {
        yield return new WaitForSeconds(secs);
        if (!_sticky) yield return FadeBoth(0f, 0.3f);
    }

    IEnumerator FadeBoth(float target, float dur)
    {
        float bStart = bubbleGroup != null ? bubbleGroup.alpha : 0f;
        float cStart = characterGroup != null ? characterGroup.alpha : 0f;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / dur;
            float k = Mathf.Clamp01(t);
            if (bubbleGroup != null) bubbleGroup.alpha = Mathf.Lerp(bStart, target, k);
            if (characterGroup != null) characterGroup.alpha = Mathf.Lerp(cStart, target, k);
            yield return null;
        }
        if (bubbleGroup != null) bubbleGroup.alpha = target;
        if (characterGroup != null) characterGroup.alpha = target;
    }
}
