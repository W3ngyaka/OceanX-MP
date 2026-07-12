using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Splash / start screen: fades through a list of logos, then shows a "Tap to Start"
// prompt and waits for a tap before switching to the game scene. The game scene is
// preloaded in the background during the logos, so the cut-in on tap is instant.
public class SplashSequence : MonoBehaviour
{
    [Header("Scene")]
    [Tooltip("Scene the splash leads to (large screen). MUST be in Build Settings.")]
    public string gameScene = "SceneTemp";

    [Tooltip("On a mobile (tablet) build, skip the logo splash and boot straight into this scene " +
             "instead. Leave empty to always run the full splash.")]
    public string tabletScene = "new netcode 1";

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
        // Route to whichever destination is actually IN THIS build: the large-screen scene if
        // present, else the tablet scene. This lets one splash serve BOTH builds — the large-screen
        // build ships SceneTemp, the tablet build ships new netcode 1, and each auto-picks its own.
        string target = gameScene;
        if (!Application.CanStreamedLevelBeLoaded(gameScene) &&
            !string.IsNullOrEmpty(tabletScene) && Application.CanStreamedLevelBeLoaded(tabletScene))
            target = tabletScene;

        // Preload the destination in the background, but don't switch to it yet.
        _load = SceneManager.LoadSceneAsync(target);
        if (_load == null)
        {
            Debug.LogError($"[SplashSequence] Neither '{gameScene}' nor '{tabletScene}' is in Build Settings.");
            yield break;
        }
        _load.allowSceneActivation = false;

        // Show the splash logos.
        if (logosGroup != null)
        {
            // Preferred: images placed in the scene, revealed together as one composed group.
            logosGroup.gameObject.SetActive(true);
            // Visibility is driven by the CanvasGroup alpha, so make sure the placed graphics
            // are actually enabled (duplicated logos often come in disabled).
            foreach (var g in logosGroup.GetComponentsInChildren<Graphic>(true)) g.enabled = true;
            logosGroup.alpha = 0f;
            yield return FadeGroup(logosGroup, 0f, 1f);
            yield return new WaitForSecondsRealtime(holdDuration);
            if (fadeLogosOut) yield return FadeGroup(logosGroup, 1f, 0f);
        }
        else if (logoImage != null)
        {
            // Legacy: cycle each sprite through a single image slot, one at a time.
            SetAlpha(0f);
            if (logos != null)
            {
                foreach (var logo in logos)
                {
                    if (logo == null) continue;
                    logoImage.sprite = logo;
                    logoImage.enabled = true;
                    yield return Fade(0f, 1f);
                    yield return new WaitForSecondsRealtime(holdDuration);
                    yield return Fade(1f, 0f);
                }
            }
            logoImage.enabled = false;
        }

        if (waitForTap)
        {
            // Interactive tablet: show the prompt and wait for a tap.
            if (tapToStart != null) tapToStart.SetActive(true);
            _ready = true;
            while (!_tapped) yield return null;
        }
        // else: passive large screen — auto-advance once the scene is ready.

        while (_load != null && _load.progress < 0.9f) yield return null;
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
            SetAlpha(Mathf.Lerp(from, to, fadeDuration <= 0f ? 1f : t / fadeDuration));
            yield return null;
        }
        SetAlpha(to);
    }

    // Fade a whole placed-logo group in/out together.
    IEnumerator FadeGroup(CanvasGroup cg, float from, float to)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, fadeDuration <= 0f ? 1f : t / fadeDuration);
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
