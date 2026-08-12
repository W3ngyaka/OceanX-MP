using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class SplashSequence : MonoBehaviour
{
    
    public CanvasGroup logosGroup;
    public Image logoImage;
    public float holdDuration = 1.3f;
    public Image fadeOverlay;
    public float screenFadeDuration = 0.6f;

    private AsyncOperation _load;

    void Start()
    {
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
     
        if (fadeOverlay != null) SetFade(1f);
        if (logosGroup != null) logosGroup.alpha = 1f;
        else if (logoImage != null) SetAlpha(1f);

        
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

        _load = SceneManager.LoadSceneAsync(nextIndex);
        _load.allowSceneActivation = false;

        if (logosGroup != null)
        {
            logosGroup.gameObject.SetActive(true);
            foreach (var g in logosGroup.GetComponentsInChildren<Graphic>(true)) g.enabled = true;
            logosGroup.alpha = 1f;
        }

       
        if (fadeOverlay != null) yield return FadeScreen(1f, 0f);

        yield return new WaitForSecondsRealtime(holdDuration);

        while (_load != null && _load.progress < 0.9f) yield return null;

       
        if (fadeOverlay != null) yield return FadeScreen(0f, 1f);
        if (_load != null) _load.allowSceneActivation = true;
    }


    IEnumerator FadeScreen(float from, float to)
    {
        if (fadeOverlay == null) yield break;
        fadeOverlay.gameObject.SetActive(true);
        SetFade(from);
        yield return null;   

        float t = 0f;
        while (t < screenFadeDuration)
        {
            t += Mathf.Min(Time.unscaledDeltaTime, 0.05f);   
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

    void SetAlpha(float a)
    {
        if (logoImage == null) return;
        var c = logoImage.color; c.a = a; logoImage.color = c;
    }

    static float EaseInOut(float t, float duration)
    {
        float u = duration <= 0f ? 1f : Mathf.Clamp01(t / duration);
        return Mathf.SmoothStep(0f, 1f, u);
    }
}