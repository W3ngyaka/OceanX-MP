using UnityEngine;
using UnityEngine.EventSystems;

public class TapPunch : MonoBehaviour, IPointerDownHandler
{
        public float punchScale = 1.2f;

        public float upTime = 0.1f;

        public float downTime = 0.15f;

    private Vector3 baseScale = Vector3.one;
    private Coroutine punchRoutine;

    void Awake()
    {
        baseScale = transform.localScale;
    }

    void OnDisable()
    {

        if (punchRoutine != null) { StopCoroutine(punchRoutine); punchRoutine = null; }
        transform.localScale = baseScale;
    }

    void OnEnable()
    {
        transform.localScale = baseScale;
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
