using UnityEngine;
using UnityEngine.EventSystems;

// Reusable tap "punch" scale animation. Matches the feel of SpeciesBubble's TapPunch:
// scale up to 1.2x over 0.1s, then ease back to 1x over 0.15s. Drop this on any UI
// element (e.g. the Add button) to give it the same tactile tap as the food-web bubbles.
public class TapPunch : MonoBehaviour, IPointerDownHandler
{
    [Tooltip("Peak scale multiplier at the top of the punch.")]
    public float punchScale = 1.2f;

    [Tooltip("Seconds to scale up to the peak.")]
    public float upTime = 0.1f;

    [Tooltip("Seconds to settle back to rest.")]
    public float downTime = 0.15f;

    private Vector3 baseScale = Vector3.one;
    private Coroutine punchRoutine;

    void Awake()
    {
        baseScale = transform.localScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Play();
    }

    public void Play()
    {
        if (punchRoutine != null) StopCoroutine(punchRoutine);
        transform.localScale = baseScale;
        punchRoutine = StartCoroutine(PunchRoutine());
    }

    System.Collections.IEnumerator PunchRoutine()
    {
        Vector3 original = baseScale;
        Vector3 big = original * punchScale;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / upTime;
            transform.localScale = Vector3.Lerp(original, big, Mathf.Clamp01(t));
            yield return null;
        }

        t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / downTime;
            transform.localScale = Vector3.Lerp(big, original, Mathf.Clamp01(t));
            yield return null;
        }

        transform.localScale = original;
        punchRoutine = null;
    }
}
