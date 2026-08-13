using UnityEngine;

[DisallowMultipleComponent]
public class FishSwim : MonoBehaviour
{
    [Header("Glide")]
        public float glideX = 140f;
        public float glideY = 25f;
        public float glideSpeed = 0.15f;

    [Header("Facing")]
        public bool flipToFace = true;
        public bool artFacesLeft = false;

    [Header("Tail wag (fake propulsion)")]
        public float wagAmount = 0.05f;
        public float wagSpeed = 2.5f;

    [Header("Misc")]
    public bool randomPhase = true;

    private Vector3 _basePos;
    private Vector3 _baseScale;
    private float _phase;
    private float _lastX;

    void OnEnable()
    {
        _basePos = transform.localPosition;
        _baseScale = transform.localScale;
        _phase = randomPhase ? Random.Range(0f, Mathf.PI * 2f) : 0f;
        _lastX = 0f;
    }

    void OnDisable()
    {
        transform.localPosition = _basePos;
        transform.localScale = _baseScale;
    }

    void Update()
    {
        float tt = Time.unscaledTime * glideSpeed * Mathf.PI * 2f + _phase;

        float x = Mathf.Sin(tt) * glideX;
        float y = Mathf.Sin(tt * 0.5f + _phase) * glideY;
        transform.localPosition = _basePos + new Vector3(x, y, 0f);

        float vel = Mathf.Cos(tt);

        float wag = 1f + Mathf.Sin(Time.unscaledTime * wagSpeed * Mathf.PI * 2f + _phase) * wagAmount;

        float sx = Mathf.Abs(_baseScale.x) * wag;
        float sy = _baseScale.y * (1f - (wag - 1f) * 0.5f);

        if (flipToFace)
        {
            bool goingRight = vel >= 0f;
            bool faceRight = artFacesLeft ? !goingRight : goingRight;
            sx = faceRight ? Mathf.Abs(sx) : -Mathf.Abs(sx);
        }
        else
        {
            sx = Mathf.Sign(_baseScale.x == 0 ? 1 : _baseScale.x) * Mathf.Abs(sx);
        }

        transform.localScale = new Vector3(sx, sy, _baseScale.z);
    }
}
