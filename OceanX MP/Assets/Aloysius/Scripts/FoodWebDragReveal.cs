using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections;

public class FoodWebDragReveal : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("References")]
    public CanvasGroup dimOverlay;
    public CurrentOrganismsGrid organismsGrid;
    public RectTransform currentOrganismsView;
    public GameObject closeButton;

    [Header("Settings")]
    public float swipeThreshold = 50f;
    public float animDuration = 0.3f;
    public float slideDistance = 60f;

    private CanvasGroup cg;
    private Vector2 shownPos;
    private Vector2 hiddenPos;
    private Vector2 pointerDownPos;
    private bool isDragging = false;

    void Start()
    {
        if (currentOrganismsView != null)
        {
            cg = currentOrganismsView.GetComponent<CanvasGroup>();

            if (cg == null)
                cg = currentOrganismsView.gameObject.AddComponent<CanvasGroup>();

            shownPos = currentOrganismsView.anchoredPosition;
            hiddenPos = shownPos + new Vector2(0, slideDistance);

            currentOrganismsView.anchoredPosition = hiddenPos;
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
            currentOrganismsView.gameObject.SetActive(false);
        }

        if (dimOverlay != null)
        {
            dimOverlay.alpha = 0f;
            dimOverlay.blocksRaycasts = false;
            dimOverlay.gameObject.SetActive(false);
        }

        if (closeButton != null)
            closeButton.SetActive(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pointerDownPos = eventData.position;
        isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Required by IDragHandler, but not used.
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        isDragging = false;

        float dragDistance = pointerDownPos.y - eventData.position.y;

        // Swipe UP to open
        if (dragDistance > swipeThreshold)
            OpenView();
    }

    public void OpenView()
    {
        if (currentOrganismsView == null)
            return;

        StopAllCoroutines();

        currentOrganismsView.gameObject.SetActive(true);

        if (closeButton != null)
            closeButton.SetActive(true);

        if (organismsGrid != null)
            organismsGrid.Refresh();

        if (dimOverlay != null)
        {
            dimOverlay.gameObject.SetActive(true);
            dimOverlay.blocksRaycasts = true;

            StartCoroutine(
                FadeCanvasGroup(dimOverlay, 0f, 1f, animDuration)
            );
        }

        StartCoroutine(
            Animate(hiddenPos, shownPos, 0f, 1f)
        );
    }

    public void CloseView()
    {
        if (currentOrganismsView == null)
            return;

        StopAllCoroutines();

        if (closeButton != null)
            closeButton.SetActive(false);

        if (dimOverlay != null)
        {
            dimOverlay.blocksRaycasts = false;

            StartCoroutine(
                FadeCanvasGroup(
                    dimOverlay,
                    1f,
                    0f,
                    animDuration,
                    () =>
                    {
                        dimOverlay.gameObject.SetActive(false);
                    })
            );
        }

        StartCoroutine(
            Animate(
                shownPos,
                hiddenPos,
                1f,
                0f,
                () =>
                {
                    currentOrganismsView.gameObject.SetActive(false);
                })
        );
    }

    IEnumerator Animate(
        Vector2 fromPos,
        Vector2 toPos,
        float fromAlpha,
        float toAlpha,
        Action onComplete = null)
    {
        cg.interactable = false;
        cg.blocksRaycasts = false;

        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / animDuration;

            float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);

            currentOrganismsView.anchoredPosition =
                Vector2.Lerp(fromPos, toPos, eased);

            cg.alpha = Mathf.Lerp(fromAlpha, toAlpha, eased);

            yield return null;
        }

        currentOrganismsView.anchoredPosition = toPos;
        cg.alpha = toAlpha;

        if (toAlpha > 0.5f)
        {
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }

        onComplete?.Invoke();
    }

    IEnumerator FadeCanvasGroup(
        CanvasGroup target,
        float from,
        float to,
        float duration,
        Action onComplete = null)
    {
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;

            target.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t));

            yield return null;
        }

        target.alpha = to;

        onComplete?.Invoke();
    }
}