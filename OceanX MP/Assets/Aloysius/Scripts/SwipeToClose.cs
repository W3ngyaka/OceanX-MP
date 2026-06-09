using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class SwipeToClose : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public float swipeThreshold = 50f;
    public float slideDuration = 0.25f;

    private RectTransform rt;
    private Vector2 startPos;

    void Start()
    {
        rt = GetComponent<RectTransform>();
        startPos = rt.anchoredPosition;
    }

    public void OnPointerDown(PointerEventData e)
    {
        StopAllCoroutines();
        startPos = rt.anchoredPosition;
    }

    public void OnDrag(PointerEventData e)
    {
        rt.anchoredPosition += e.delta;
    }

    public void OnPointerUp(PointerEventData e)
    {
        Vector2 swipeDelta = rt.anchoredPosition - startPos;

        if (swipeDelta.magnitude > swipeThreshold)
        {
            // slide out in the direction of the swipe
            Vector2 direction = swipeDelta.normalized;
            Vector2 offscreen = startPos + direction * Screen.height * 1.5f;
            StartCoroutine(SlideOut(offscreen));
        }
        else
        {
            StartCoroutine(SnapBack());
        }
    }

    IEnumerator SlideOut(Vector2 target)
    {
        float t = 0f;
        Vector2 from = rt.anchoredPosition;
        while (t < 1f)
        {
            t += Time.deltaTime / slideDuration;
            float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
            rt.anchoredPosition = Vector2.Lerp(from, target, eased);
            yield return null;
        }
        ModalController.Instance.Close();
        rt.anchoredPosition = startPos;
    }

    IEnumerator SnapBack()
    {
        float t = 0f;
        Vector2 from = rt.anchoredPosition;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.15f;
            rt.anchoredPosition = Vector2.Lerp(from, startPos, Mathf.Clamp01(t));
            yield return null;
        }
        rt.anchoredPosition = startPos;
    }
}