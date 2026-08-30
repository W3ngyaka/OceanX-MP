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
    [Tooltip("How long the bubble stays up for a line with NO voice clip. Lines that DO have " +
             "voice ignore this and use the clip's own length instead.")]
    public float autoHideSeconds = 5.2f;

    [Tooltip("Extra beat the bubble lingers after the voice finishes, so the last word isn't " +
             "cut off visually the instant the audio ends.")]
    public float voiceTailSeconds = 0.6f;

    // How long the most recent Say() will hold — clip length + tail, or autoHideSeconds.
    // IntroSequence paces itself on this so the three intro lines follow the voice rather
    // than a fixed gap that is wrong for every line of a different length.
    private float _lastSpokenSeconds;
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
    [Tooltip("Pause BETWEEN intro lines, on top of however long the line took. It is ADDITIVE, " +
             "not the total spacing — each line already waits for its own voice clip (or " +
             "autoHideSeconds when it has none) before this gap starts. Keep it under a second; " +
             "at 3 the intro drags for ~21s with 3.6s of silence between lines.")]
    public float introLineGap = 0.4f;

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

    // Intro gate. HandleStarted clears _muted the instant the session starts, but IntroSequence is a
    // coroutine that then waits — first for the tablet tutorial to be dismissed, then through three
    // paced lines. Without this, everything in that window (ecology events, unlock hints) went straight
    // through Say and either jumped ahead of intro.1 or landed between the intro lines. With voice that
    // is not just out of order: Say hands a new clip name to AluciaVoice, cutting the intro off mid-word.
    // _speakingIntro is the exemption that lets IntroSequence's own lines past its own gate.
    private bool _introComplete;
    private bool _speakingIntro;

    /// <summary>
    /// How hard a line is willing to push in while Alucia is already speaking. Deliberately only two
    /// tiers — anything finer becomes guesswork about which advisory line matters more.
    /// <c>Normal</c> is dropped while she is mid-line; <c>High</c> cuts in, and is for moments that are
    /// irreversible or are the point of the exhibit (a keystone species going extinct).
    /// </summary>
    public enum Priority { Normal = 0, High = 1 }

    // Priority of the line currently playing, so a Normal line cannot displace a High one.
    private Priority _currentPriority = Priority.Normal;

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
        // Re-close the intro gate too, so the next visitor also gets silence until their intro
        // has run. StopAllCoroutines above may have killed IntroSequence part-way through, which
        // would otherwise leave _speakingIntro stuck true and let anything speak.
        _introComplete = false;
        _speakingIntro = false;
        // Otherwise a High line from the previous session would keep blocking Normal ones for the next.
        _currentPriority = Priority.Normal;
        // Kill any line still playing — otherwise the next visitor walks into the tail of the
        // previous session's voice-over with no bubble on screen to explain it.
        if (AluciaVoice.Instance != null) AluciaVoice.Instance.StopSpeaking();
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

    /// <summary>
    /// Show a line. When <paramref name="audioName"/> names a clip in AluciaVoice, the bubble
    /// is held for the CLIP's length (plus voiceTailSeconds) instead of the fixed
    /// autoHideSeconds — so she stops talking and the bubble clears together. Falls back to
    /// autoHideSeconds whenever there is no clip, which is what lets VO be filled in gradually.
    /// </summary>
    public bool Say(string message, Mood mood = Mood.Calm, bool sticky = false, string audioName = null,
                    Priority priority = Priority.Normal)
    {
        Debug.Log($"[Alucia] Say(\"{message}\") started={_started} muted={_muted} sticky={sticky}.", this);
        if (_muted) return false;
        // Nothing but the intro itself may speak until the intro has finished. See _introComplete.
        // Safe against a permanent mute: HandleStarted only skips IntroSequence when _introPlayed is
        // already true, which means the intro DID run and set this — do not "optimise" that branch.
        if (!_introComplete && !_speakingIntro) return false;

        // Don't talk over herself. minGapBetweenMessages alone was not enough: it measures wall-clock
        // since the last line, so a 1.5s gap let anything cut into a 5s clip a third of the way in, and
        // AluciaVoice.TryPlay always Stop()s whatever was playing. A clipped word reads as a bug to a
        // visitor, so a busy line is DROPPED rather than queued — queueing would have her narrating a
        // problem the visitor already fixed, since the ecology tick re-evaluates and will simply say it
        // again if it is still true. High priority still interrupts (extinction, and other
        // irreversible moments worth cutting in for).
        // Degrades correctly while the VO is incomplete: with no clip IsSpeaking is false, so lines that
        // have no Audio cell yet behave exactly as they did before.
        if (!sticky && priority <= _currentPriority
            && AluciaVoice.Instance != null && AluciaVoice.Instance.IsSpeaking) return false;

        if (Time.unscaledTime - _lastMsgTime < minGapBetweenMessages && !sticky) return false;
        _currentPriority = priority;
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

        // Start the voice BEFORE choosing the hold time — TryPlay returns the clip length,
        // or -1 when this line has no VO.
        float spoken = AluciaVoice.Instance != null ? AluciaVoice.Instance.TryPlay(audioName) : -1f;
        _lastSpokenSeconds = spoken > 0f ? spoken + voiceTailSeconds : autoHideSeconds;

        if (!sticky) _hideRoutine = StartCoroutine(HideAfter(_lastSpokenSeconds));
        return true;
    }

    public void Hide()
    {
        StartCoroutine(FadeBoth(0f, 0.3f));
    }

    /// <summary>
    /// Silence Alucia and clear her off screen, or let her resume. Used by the win screen: that card
    /// shows her full-size across the whole display, so the little corner bubble talking at the same
    /// time reads as two Alucias.
    ///
    /// Muting alone is not enough. Say() returns early on _muted, but a bubble ALREADY on screen when
    /// the win fires would just sit there, and a voice clip already playing would keep talking over the
    /// win narration - so this also hides the bubble and stops the clip, the same pair
    /// ResetForNewSession uses.
    /// </summary>
    public void SetMuted(bool muted)
    {
        if (_muted == muted) return;
        _muted = muted;
        if (!muted) return;

        if (_hideRoutine != null) { StopCoroutine(_hideRoutine); _hideRoutine = null; }
        _sticky = false;
        _currentPriority = Priority.Normal;   // nothing is playing any more; don't block the next line
        Hide();
        if (AluciaVoice.Instance != null) AluciaVoice.Instance.StopSpeaking();
    }

    /// <summary>
    /// Look a line up by event key and say it, carrying its Audio column through to the voice
    /// player. Use this instead of Say(AluciaLines.Get(...)) — Get() returns only the text, so
    /// the randomly-picked variant's clip name would be lost and the line would stay silent.
    /// </summary>
    public bool SayEvent(string eventKey, string fallback, Mood mood = Mood.Calm,
                         string species = null, bool sticky = false,
                         Priority priority = Priority.Normal)
    {
        AluciaLines.Line line = AluciaLines.GetLine(eventKey, species);
        string text = line.Found && !string.IsNullOrEmpty(line.Text) ? line.Text : fallback;
        return Say(text, mood, sticky, line.Audio, priority);
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
        // Hold everything else back until all three lines have been SPOKEN, not merely issued.
        _speakingIntro = true;
        // Each line waits for the PREVIOUS one to actually finish speaking. With voice present
        // that's the clip length + tail; without it, autoHideSeconds. introLineGap is now only
        // the extra breath BETWEEN lines, not the whole spacing, so lines can't overlap.
        SayEvent("intro.1", introLine1, Mood.Calm);
        yield return new WaitForSeconds(_lastSpokenSeconds + introLineGap);
        SayEvent("intro.2", introLine2, Mood.Warn);
        yield return new WaitForSeconds(_lastSpokenSeconds + introLineGap);
        SayEvent("intro.3", introLine3, Mood.Calm);
        // Wait out the LAST line too, otherwise the first hint lands on top of its tail.
        yield return new WaitForSeconds(_lastSpokenSeconds);
        _speakingIntro = false;
        _introComplete = true;
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
                SayEvent("health.critical", "The reef is collapsing — we're losing species fast!", Mood.Warn);
                break;
            case Band.Unstable:
                if (improving) SayEvent("health.unstable.up", "It's stabilising a little... keep going!", Mood.Calm);
                else SayEvent("health.unstable.down", "Things are slipping — the balance is breaking down.", Mood.Warn);
                break;
            case Band.Healthy:
                SayEvent("health.healthy", "The reef's looking much healthier now!", Mood.Calm);
                break;
            case Band.Thriving:
                // Deliberately silent. The win is already shown as a full congratulation image, so a
                // speech bubble on top of it was duplicate praise - and this one was sticky:true, so it
                // sat over the artwork until something else replaced it.
                // The band still matters: _lastBand is updated above, so dropping back to Healthy still
                // fires the Healthy line, and BandFor hysteresis keeps treating Thriving as a high band.
                // The health.thriving row was removed from alucia_lines (sheet + CSV) at the same time.
                // Do NOT re-add a Say here without removing the image, or both will show at once.
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
