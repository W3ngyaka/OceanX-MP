using UnityEngine;
using System.Collections;

public class DimFader : MonoBehaviour
{
    private CanvasGroup cg;
    private Coroutine current;

    void Awake()
    {
        cg = GetComponent<CanvasGroup>();
    }

    public void FadeTo(float target, float duration, System.Action onComplete = null)
    {

        if (!isActiveAndEnabled)
        {
            if (cg != null) cg.alpha = target;
            onComplete?.Invoke();
            return;
        }

        if (current != null) StopCoroutine(current);
        current = StartCoroutine(Fade(target, duration, onComplete));
    }

    IEnumerator Fade(float target, float duration, System.Action onComplete)
    {
        float from = cg.alpha;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            cg.alpha = Mathf.Lerp(from, target, Mathf.Clamp01(t));
            yield return null;
        }
        cg.alpha = target;
        onComplete?.Invoke();
    }
}
