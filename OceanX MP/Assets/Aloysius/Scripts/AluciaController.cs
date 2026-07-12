using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OceanX.BoidsGPU.Ecosystem;

// Alucia — 2D character overlay on the large/host screen.
// Plays an intro sequence, then reacts to live ecosystem health (EcoHealth01)
// by speaking when health crosses a band boundary (debounced, with hysteresis).
//
// SETUP (assign in Inspector):
//   characterImage  -> the Image showing Alucia's sprite (placeholder ok)
//   bubbleGroup     -> CanvasGroup on the speech-bubble container (for fade)
//   bubbleText      -> the Text/TMP on the speech bubble
//   simulation      -> the EcosystemSimulationGPU in the scene (host). If left
//                      null, health reactions are skipped (intro still plays).
//
// MOODS just tint the bubble; swap for sprite/animation states when art exists.
public class AluciaController : MonoBehaviour
{
    public enum Mood { Calm, Warn, Win }

    [Header("Refs")]
    public CanvasGroup characterGroup;     // CanvasGroup on the character (for fade in/out)
    public Image characterImage;          // Alucia sprite (placeholder for now)
    public CanvasGroup bubbleGroup;        // speech bubble container
    public Text bubbleText;                // message text (swap to TMP if you use it)

    [Header("Simulation (host)")]
    [Tooltip("EcosystemSimulationGPU in this scene. Leave null to skip health reactions.")]
    public EcosystemSimulationGPU simulation;

    [Header("Timing")]
    public float autoHideSeconds = 5.2f;
    public float minGapBetweenMessages = 1.5f;  // anti-spam floor
    public float introStartDelay = 0.6f;

    [Header("Health bands (0-100%)")]
    public float criticalMax = 35f;   // below this = critical
    public float healthyMin = 70f;    // above this = healthy
    public float hysteresis = 4f;     // dead-zone around each boundary so jitter doesn't re-fire

    [Header("Mood colours (bubble tint)")]
    public Color calmColor = new Color(0.85f, 0.96f, 1f, 0.92f);
    public Color warnColor = new Color(1f, 0.93f, 0.85f, 0.92f);
    public Color winColor  = new Color(0.86f, 0.99f, 0.94f, 0.92f);

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
    [Tooltip("Wait for the tablet's 'tap to begin' (networked OnStarted) before playing the intro " +
             "and reacting to health, so players don't miss it. Turn off to play on scene load.")]
    public bool waitForExperienceStart = true;

    private Image _bubbleBg;            // optional bg image on the bubble for tinting
    private float _lastMsgTime = -99f;
    private Coroutine _hideRoutine;
    private bool _sticky;

    // Health-band tracking
    private enum Band { Critical, Unstable, Healthy, Thriving }
    private Band _lastBand = Band.Critical;
    private bool _bandInit;

    // Start gating
    private bool _started;
    private bool _introPlayed;
    private bool _subscribed;

    void Awake()
    {
        if (bubbleGroup != null) { bubbleGroup.alpha = 0f; _bubbleBg = bubbleGroup.GetComponent<Image>(); }
        if (characterGroup != null) characterGroup.alpha = 0f;
    }

    void Start()
    {
        // When not gating on the networked start, play the intro immediately (e.g. standalone testing).
        if (!waitForExperienceStart)
            HandleStarted();
    }

    void Update()
    {
        // Begin only once the tablet's "tap to begin" flips the shared HasStarted flag,
        // so the intro isn't spoken to an empty title screen.
        if (waitForExperienceStart && !_subscribed)
        {
            var net = EcosystemNetworkManagerGPU.Instance;
            if (net != null)
            {
                _subscribed = true;
                net.OnStarted += HandleStarted;
                if (net.HasStarted) HandleStarted();   // already started (e.g. joined late)
            }
        }

        if (!_started) return;                          // no intro / health chatter before we begin
        if (simulation == null) return;
        float h = Mathf.Clamp01(simulation.EcoHealth01) * 100f;
        EvaluateHealth(h);
    }

    // Fires once, when the experience actually begins.
    void HandleStarted()
    {
        if (_started) return;
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

    // ---------- Public API ----------

    public void Say(string message, Mood mood = Mood.Calm, bool sticky = false)
    {
        if (Time.unscaledTime - _lastMsgTime < minGapBetweenMessages && !sticky) return;
        _lastMsgTime = Time.unscaledTime;
        _sticky = sticky;

        if (bubbleText != null) bubbleText.text = message;
        if (_bubbleBg != null) _bubbleBg.color = MoodColor(mood);
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

    // ---------- Intro ----------

    IEnumerator IntroSequence()
    {
        yield return new WaitForSeconds(introStartDelay);
        Say(AluciaLines.Get("intro.1", introLine1), Mood.Calm);
        yield return new WaitForSeconds(introLineGap);
        Say(AluciaLines.Get("intro.2", introLine2), Mood.Warn);
        yield return new WaitForSeconds(introLineGap);
        Say(AluciaLines.Get("intro.3", introLine3), Mood.Calm);
    }

    // ---------- Health reactions ----------

    void EvaluateHealth(float h)
    {
        Band band = BandFor(h);

        if (!_bandInit)
        {
            _lastBand = band;
            _bandInit = true;
            return; // don't speak on the very first read
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
        // Hysteresis: require crossing a boundary by `hysteresis` before changing band,
        // biased by the current band so we don't flap right on the line.
        float critLine = criticalMax + (_lastBand == Band.Critical ? hysteresis : -hysteresis);
        float healLine = healthyMin + (_lastBand == Band.Healthy || _lastBand == Band.Thriving ? -hysteresis : hysteresis);

        if (h >= 100f) return Band.Thriving;
        if (h >= healLine) return Band.Healthy;
        if (h <= critLine) return Band.Critical;
        return Band.Unstable;
    }

    // ---------- Helpers ----------

    Sprite MoodSprite(Mood m)
    {
        switch (m)
        {
            case Mood.Warn: return warnSprite;
            case Mood.Win:  return winSprite;
            default:        return calmSprite;
        }
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
