using UnityEngine;
using System.Collections;

// Runs the fade coroutine on the overlay's own GameObject so it isn't killed
// when the modal that owns it gets SetActive(false).
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
        // A coroutine can't run on an inactive GameObject (Unity logs "Coroutine couldn't be
        // started..."). If the overlay is already inactive/disabled, just snap to the target and
        // fire the callback synchronously so callers (e.g. ModalController.Close) still finish.
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
