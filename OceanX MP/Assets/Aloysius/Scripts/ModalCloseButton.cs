using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class ModalCloseButton : MonoBehaviour
{
    private RectTransform _rt;
    private Canvas _canvas;
    private TapPunch _punch;
    private bool _pressedInside;

    void Awake()
    {
        _rt = transform as RectTransform;
        _canvas = GetComponentInParent<Canvas>();
        _punch = GetComponent<TapPunch>();
    }

    void Update()
    {
        var cam = (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? _canvas.worldCamera : null;

        if (Input.GetMouseButtonDown(0))
        {
            _pressedInside = RectTransformUtility.RectangleContainsScreenPoint(_rt, Input.mousePosition, cam);
            if (_pressedInside && _punch != null) _punch.Play();
        }

        if (Input.GetMouseButtonUp(0))
        {
            bool upInside = RectTransformUtility.RectangleContainsScreenPoint(_rt, Input.mousePosition, cam);
            if (_pressedInside && upInside && ModalController.Instance != null)
                ModalController.Instance.Close();
            _pressedInside = false;
        }
    }
}
