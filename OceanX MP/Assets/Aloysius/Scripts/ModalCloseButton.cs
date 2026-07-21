using UnityEngine;
using UnityEngine.EventSystems;

// Bulletproof close button: detects a tap whose screen position falls within this button's rect,
// checked directly in Update (bypasses EventSystem raycast ordering, which the panel background
// was swallowing). Plays a TapPunch animation on press and closes the modal on release.
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

        // Press: if it starts inside the button, play the punch animation.
        if (Input.GetMouseButtonDown(0))
        {
            _pressedInside = RectTransformUtility.RectangleContainsScreenPoint(_rt, Input.mousePosition, cam);
            if (_pressedInside && _punch != null) _punch.Play();
        }

        // Release: only close if both press and release were inside (a real tap, not a drag-off).
        if (Input.GetMouseButtonUp(0))
        {
            bool upInside = RectTransformUtility.RectangleContainsScreenPoint(_rt, Input.mousePosition, cam);
            if (_pressedInside && upInside && ModalController.Instance != null)
                ModalController.Instance.Close();
            _pressedInside = false;
        }
    }
}
