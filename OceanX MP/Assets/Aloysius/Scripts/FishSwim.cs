using UnityEngine;

// Makes a 2D fish sprite feel alive: glides side-to-side along a slow path, flips to face its
// travel direction, and does a subtle tail-wag (horizontal squash) for propulsion. Optional bob.
// Uses unscaled time so it runs on menus. Restores base transform on disable.
[DisallowMultipleComponent]
public class FishSwim : MonoBehaviour
{
    [Header("Glide")]
    [Tooltip("Horizontal travel distance each way (px).")]
    public float glideX = 140f;
    [Tooltip("Vertical drift amount (px).")]
    public float glideY = 25f;
    [Tooltip("Full glide cycles per second (lower = slower swim).")]
    public float glideSpeed = 0.15f;

    [Header("Facing")]
    [Tooltip("Flip the sprite horizontally to face travel direction.")]
    public bool flipToFace = true;
    [Tooltip("If the source art faces LEFT by default, tick this.")]
    public bool artFacesLeft = false;

    [Header("Tail wag (fake propulsion)")]
    [Tooltip("How much the body squashes horizontally (0 = off).")]
    public float wagAmount = 0.05f;
    [Tooltip("Tail wags per second — faster than the glide.")]
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

        // Glide: horizontal sine, vertical at half rate for a lazy arc
        float x = Mathf.Sin(tt) * glideX;
        float y = Mathf.Sin(tt * 0.5f + _phase) * glideY;
        transform.localPosition = _basePos + new Vector3(x, y, 0f);

        // Direction = sign of horizontal velocity (derivative of sin = cos)
        float vel = Mathf.Cos(tt);

        // Tail wag: horizontal squash oscillation
        float wag = 1f + Mathf.Sin(Time.unscaledTime * wagSpeed * Mathf.PI * 2f + _phase) * wagAmount;

        float sx = Mathf.Abs(_baseScale.x) * wag;
        float sy = _baseScale.y * (1f - (wag - 1f) * 0.5f); // slight counter-squash on Y (volume feel)

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
