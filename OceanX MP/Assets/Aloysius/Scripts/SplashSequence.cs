using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Splash / start screen: fades through a list of logos, then (optionally) shows a "Tap to Start"
// prompt before advancing to the game scene. The destination is simply the NEXT scene in Build
// Settings (put this splash at index 0 and the game scene at index 1) — no scene name to keep in
// sync, and the SAME splash works for both the large-screen and tablet builds, since each build
// ships its own scene 1. The next scene is preloaded in the background during the logos, so the
// cut-in is instant.
public class SplashSequence : MonoBehaviour
{
    [Header("Composed logo group (all shown at once)")]
    [Tooltip("Parent CanvasGroup holding the images you PLACED in the scene (left/middle/right). " +
             "If set, all of them fade in together and the sprite-cycle 'Logos' list below is ignored.")]
    public CanvasGroup logosGroup;
    [Tooltip("Fade the group back out after the hold. Off = leave it on screen until the scene changes.")]
    public bool fadeLogosOut = false;

    [Header("Logos (legacy — single slot, one sprite at a time)")]
    [Tooltip("The single Image the logos are shown in (reused for each). Ignored when a Logos Group is set.")]
    public Image logoImage;
    [Tooltip("Logos shown one after another. Ignored when a Logos Group is set.")]
    public Sprite[] logos;
    public float fadeDuration = 0.4f;
    public float holdDuration = 1.3f;

    [Header("After the logos")]
    [Tooltip("ON  = show 'Tap to Start' and wait for a tap  (use on the interactive TABLET).\n" +
             "OFF = auto-advance into the game once logos finish + scene loads (use on the passive LARGE SCREEN).")]
    public bool waitForTap = true;

    [Header("Tap to start (only used when Wait For Tap is ON)")]
    [Tooltip("The 'Tap to Start!' prompt — hidden until the logos finish.")]
    public GameObject tapToStart;
    [Tooltip("Full-screen button that catches the tap (wired automatically).")]
    public Button startButton;
    [Tooltip("Gently pulse the prompt so it reads as interactive.")]
    public bool pulsePrompt = true;
    public float pulseSpeed = 2f;

    [Header("Screen fade")]
    [Tooltip("Full-screen black Image faded IN at the start and OUT before the game loads.")]
    public Image fadeOverlay;
    public float screenFadeDuration = 0.6f;

    private AsyncOperation _load;
    private bool _ready;     // logos done, prompt showing
    private bool _tapped;
    private float _pulseT;

    void Start()
    {
        if (tapToStart != null) tapToStart.SetActive(false);
        if (startButton != null) startButton.onClick.AddListener(OnTap);
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        // Start on a full black screen; the fader does the fade IN and OUT. Logos are shown at full
        // so the black lifting reveals them, and re-darkening covers them again on the way out.
        if (fadeOverlay != null) SetFade(1f);
        if (logosGroup != null) logosGroup.alpha = 1f;
        else if (logoImage != null) SetAlpha(1f);

        // Destination = the NEXT scene in Build Settings (this splash at index 0 -> game at index 1).
        // No scene name to maintain, and it auto-picks the right "scene 1" in each build (tablet or large screen).
        int current = SceneManager.GetActiveScene().buildIndex;
        int nextIndex = current + 1;
        if (current < 0)
        {
            Debug.LogError("[SplashSequence] This splash scene isn't in Build Settings — add it (as index 0), " +
                           "with the game scene right after it (index 1). File > Build Settings.");
            yield break;
        }
        if (nextIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError($"[SplashSequence] No scene at build index {nextIndex}. Put the game scene right after " +
                           "this splash in Build Settings (File > Build Settings).");
            yield break;
        }

        // Preload the next scene in the background, but don't switch to it yet.
        _load = SceneManager.LoadSceneAsync(nextIndex);
        _load.allowSceneActivation = false;

        // Make sure the logo group is on and fully visible — the fader does the fading.
        if (logosGroup != null)
        {
            logosGroup.gameObject.SetActive(true);
            foreach (var g in logosGroup.GetComponentsInChildren<Graphic>(true)) g.enabled = true;
            logosGroup.alpha = 1f;
        }

        // FADE IN — the black screen lifts to reveal the logos.
        if (fadeOverlay != null) yield return FadeScreen(1f, 0f);

        // Hold on the logos.
        yield return new WaitForSecondsRealtime(holdDuration);

        if (waitForTap)
        {
            // Interactive tablet: show the prompt and wait for a tap.
            if (tapToStart != null) tapToStart.SetActive(true);
            _ready = true;
            while (!_tapped) yield return null;
        }
        // else: passive large screen — auto-advance once the scene is ready.

        while (_load != null && _load.progress < 0.9f) yield return null;

        // Fade the screen out to black, then switch to the game scene.
        if (fadeOverlay != null) yield return FadeScreen(0f, 1f);
        if (_load != null) _load.allowSceneActivation = true;
    }

    // Wired to the full-screen button; only counts once the prompt is up.
    public void OnTap()
    {
        if (_ready) _tapped = true;
    }

    void Update()
    {
        if (pulsePrompt && _ready && tapToStart != null && tapToStart.activeSelf)
        {
            _pulseT += Time.unscaledDeltaTime * pulseSpeed;
            var cg = tapToStart.GetComponent<CanvasGroup>();
            if (cg == null) cg = tapToStart.AddComponent<CanvasGroup>();
            cg.alpha = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(_pulseT));
        }
    }

    IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(from, to, EaseInOut(t, fadeDuration)));
            yield return null;
        }
        SetAlpha(to);
    }

    // Fade the full-screen black overlay (the screen fade in/out).
    IEnumerator FadeScreen(float from, float to)
    {
        if (fadeOverlay == null) yield break;
        fadeOverlay.gameObject.SetActive(true);
        SetFade(from);
        yield return null;   // skip the (often huge) first frame after a scene load so the fade is smooth
        float t = 0f;
        while (t < screenFadeDuration)
        {
            t += Mathf.Min(Time.unscaledDeltaTime, 0.05f);   // clamp so a frame hitch can't jump the fade
            SetFade(Mathf.Lerp(from, to, EaseInOut(t, screenFadeDuration)));
            yield return null;
        }
        SetFade(to);
    }

    void SetFade(float a)
    {
        if (fadeOverlay == null) return;
        var c = fadeOverlay.color; c.a = a; fadeOverlay.color = c;
    }

    // Smooth ease-in-out (S-curve) so fades start and end gently instead of a hard linear ramp.
    static float EaseInOut(float t, float duration)
    {
        float u = duration <= 0f ? 1f : Mathf.Clamp01(t / duration);
        return Mathf.SmoothStep(0f, 1f, u);
    }

    // Fade a whole placed-logo group in/out together.
    IEnumerator FadeGroup(CanvasGroup cg, float from, float to)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, EaseInOut(t, fadeDuration));
            yield return null;
        }
        cg.alpha = to;
    }

    void SetAlpha(float a)
    {
        if (logoImage == null) return;
        var c = logoImage.color; c.a = a; logoImage.color = c;
    }
}
