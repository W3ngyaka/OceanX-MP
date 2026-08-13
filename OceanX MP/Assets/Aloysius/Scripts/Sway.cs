using UnityEngine;

[DisallowMultipleComponent]
public class Sway : MonoBehaviour
{
        public float angle = 5f;

        public float speed = 0.5f;

        public bool randomPhase = true;

        public bool pivotFromBase = false;

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
        float s = Mathf.Sin(Time.unscaledTime * speed * Mathf.PI * 2f + _phase);
        float a = s * angle;

        if (pivotFromBase && _rt != null)
        {

            Quaternion rot = _baseRot * Quaternion.Euler(0, 0, a);
            transform.localRotation = rot;

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
