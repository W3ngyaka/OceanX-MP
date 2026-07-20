using UnityEngine;

// Gentle looping sway for 2D deco (seaweed, coral, fish) — Paper-Mario-ish rock.
// Rotates back and forth on a sine wave. Can pivot from the BASE (bottom) so rooted things
// like seaweed sway from the seabed instead of spinning around their center.
[DisallowMultipleComponent]
public class Sway : MonoBehaviour
{
    [Tooltip("Max rotation in degrees each way.")]
    public float angle = 5f;

    [Tooltip("Full sways per second (lower = lazier).")]
    public float speed = 0.5f;

    [Tooltip("Randomized per-instance so deco doesn't move in unison. Leave on.")]
    public bool randomPhase = true;

    [Tooltip("Pivot the rotation around the object's bottom edge (good for seaweed/coral). " +
             "Off = rotate around center (good for free-floating fish).")]
    public bool pivotFromBase = false;

    [Tooltip("Optional tiny bob added on top of the sway (px). 0 = none.")]
    public float bobHeight = 0f;

    private float _phase;
    private Quaternion _baseRot;
    private Vector3 _basePos;
    private RectTransform _rt;
    private float _halfHeight;

    void OnEnable()
    {
        _rt = transform as RectTransform;
        _baseRot = transform.localRotation;
        _basePos = transform.localPosition;
        _phase = randomPhase ? Random.Range(0f, Mathf.PI * 2f) : 0f;
        if (_rt != null) _halfHeight = _rt.rect.height * 0.5f;
    }

    void OnDisable()
    {
        transform.localRotation = _baseRot;
        transform.localPosition = _basePos;
    }

    void Update()
    {
        float s = Mathf.Sin(Time.unscaledTime * speed * Mathf.PI * 2f + _phase); // -1..1
        float a = s * angle;

        if (pivotFromBase && _rt != null)
        {
            // Rotate around the bottom-center: offset down, rotate, offset back.
            Quaternion rot = _baseRot * Quaternion.Euler(0, 0, a);
            transform.localRotation = rot;
            // compensate position so the base stays put
            Vector3 down = _baseRot * new Vector3(0, -_halfHeight, 0);
            Vector3 rotatedDown = rot * new Vector3(0, -_halfHeight, 0);
            transform.localPosition = _basePos + (down - rotatedDown);
        }
        else
        {
            transform.localRotation = _baseRot * Quaternion.Euler(0, 0, a);
        }

        if (bobHeight != 0f)
        {
            float b = Mathf.Sin(Time.unscaledTime * speed * Mathf.PI * 2f + _phase + 1.3f) * bobHeight;
            transform.localPosition = transform.localPosition + new Vector3(0, b, 0);
        }
    }
}
