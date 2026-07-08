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
    [Tooltip("Scene to load on tap. MUST be added to Build Settings.")]
    public string gameScene = "new netcode 1";

    [Header("Logos (shown in order)")]
    [Tooltip("The single Image the logos are shown in (reused for each).")]
    public Image logoImage;
    [Tooltip("Logos shown one after another.")]
    public Sprite[] logos;
    public float fadeDuration = 0.4f;
    public float holdDuration = 1.3f;

    [Header("Tap to start")]
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
        // Preload the game scene in the background, but don't switch to it yet.
        _load = SceneManager.LoadSceneAsync(gameScene);
        _load.allowSceneActivation = false;

        // Play the logos.
        if (logoImage != null)
        {
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

        // Show the tap prompt, then wait for a tap AND the scene to have finished loading.
        if (tapToStart != null) tapToStart.SetActive(true);
        _ready = true;

        while (!_tapped) yield return null;
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

    void SetAlpha(float a)
    {
        if (logoImage == null) return;
        var c = logoImage.color; c.a = a; logoImage.color = c;
    }
}
