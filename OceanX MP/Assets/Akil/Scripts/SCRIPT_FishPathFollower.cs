using System.Collections.Generic;
using UnityEngine;

public class SharkPathFollower : MonoBehaviour
{
    [Header("Waypoints")]
    [Tooltip("Empty GameObjects defining the loop, in order.")]
    public Transform[] waypoints;
    public bool closedLoop = true;

    [Header("Movement")]
    [Tooltip("Units per second along the path.")]
    public float speed = 3f;
    [Tooltip("How quickly the shark rotates to face its travel direction.")]
    public float turnSpeed = 3f;

    [Header("Banking")]
    [Tooltip("How much the shark rolls into turns (degrees).")]
    public float bankAmount = 25f;
    public float bankSmoothing = 2f;

    [Header("Sampling")]
    [Tooltip("Points sampled per spline segment. Higher = smoother.")]
    public int samplesPerSegment = 20;

    private List<Vector3> _path = new List<Vector3>();
    private List<float> _cumulative = new List<float>(); // arc length at each sample
    private float _totalLength;
    private float _distance;      // current distance travelled along path
    private float _currentBank;

    void Start()
    {
        BuildPath();
    }

    void Update()
    {
        if (_path.Count < 2) return;

        _distance += speed * Time.deltaTime;
        if (_totalLength > 0f) _distance %= _totalLength;

        Vector3 pos = SampleAtDistance(_distance);
        // look slightly ahead for a stable forward direction
        Vector3 ahead = SampleAtDistance(_distance + 0.1f);
        Vector3 dir = (ahead - pos);
        if (dir.sqrMagnitude < 1e-6f) dir = transform.forward;
        dir.Normalize();

        transform.position = pos;

        // Base rotation toward travel direction
        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);

        // Bank: roll based on how sharply the horizontal direction is changing
        Vector3 flatDir = new Vector3(dir.x, 0f, dir.z).normalized;
        Vector3 aheadMore = SampleAtDistance(_distance + 0.5f);
        Vector3 futureDir = (aheadMore - pos);
        futureDir.y = 0f;
        futureDir.Normalize();
        float turn = Vector3.SignedAngle(flatDir, futureDir, Vector3.up);
        float targetBank = Mathf.Clamp(-turn, -bankAmount, bankAmount);
        _currentBank = Mathf.Lerp(_currentBank, targetBank, Time.deltaTime * bankSmoothing);
        targetRot *= Quaternion.Euler(0f, 0f, _currentBank);

        transform.rotation = Quaternion.Slerp(
            transform.rotation, targetRot, Time.deltaTime * turnSpeed);
    }

    void BuildPath()
    {
        _path.Clear();
        _cumulative.Clear();
        if (waypoints == null || waypoints.Length < 2) return;

        int count = waypoints.Length;
        int segments = closedLoop ? count : count - 1;

        for (int seg = 0; seg < segments; seg++)
        {
            Transform p0 = waypoints[GetIndex(seg - 1, count)];
            Transform p1 = waypoints[GetIndex(seg, count)];
            Transform p2 = waypoints[GetIndex(seg + 1, count)];
            Transform p3 = waypoints[GetIndex(seg + 2, count)];

            for (int i = 0; i < samplesPerSegment; i++)
            {
                float t = i / (float)samplesPerSegment;
                _path.Add(CatmullRom(p0.position, p1.position, p2.position, p3.position, t));
            }
        }
        if (!closedLoop) _path.Add(waypoints[count - 1].position);

        // Precompute arc lengths
        _totalLength = 0f;
        _cumulative.Add(0f);
        for (int i = 1; i < _path.Count; i++)
        {
            _totalLength += Vector3.Distance(_path[i - 1], _path[i]);
            _cumulative.Add(_totalLength);
        }
        // close the loop distance
        if (closedLoop && _path.Count > 1)
            _totalLength += Vector3.Distance(_path[_path.Count - 1], _path[0]);
    }

    int GetIndex(int i, int count)
    {
        if (closedLoop) return ((i % count) + count) % count;
        return Mathf.Clamp(i, 0, count - 1);
    }

    // Returns interpolated position at a given arc-length distance
    Vector3 SampleAtDistance(float dist)
    {
        if (_path.Count == 0) return transform.position;
        if (_totalLength <= 0f) return _path[0];
        dist = ((dist % _totalLength) + _totalLength) % _totalLength;

        for (int i = 1; i < _cumulative.Count; i++)
        {
            if (_cumulative[i] >= dist)
            {
                float segLen = _cumulative[i] - _cumulative[i - 1];
                float t = segLen > 0f ? (dist - _cumulative[i - 1]) / segLen : 0f;
                return Vector3.Lerp(_path[i - 1], _path[i], t);
            }
        }
        // wrap segment (last point back to first)
        if (closedLoop)
        {
            float segLen = _totalLength - _cumulative[_cumulative.Count - 1];
            float t = segLen > 0f ? (dist - _cumulative[_cumulative.Count - 1]) / segLen : 0f;
            return Vector3.Lerp(_path[_path.Count - 1], _path[0], t);
        }
        return _path[_path.Count - 1];
    }

    Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (
            2f * p1 +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

    void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length < 2) return;

        // waypoint markers
        Gizmos.color = Color.yellow;
        foreach (var w in waypoints)
            if (w != null) Gizmos.DrawWireSphere(w.position, 0.3f);

        // preview the spline in the editor
        Gizmos.color = Color.cyan;
        int count = waypoints.Length;
        int segments = closedLoop ? count : count - 1;
        Vector3 prev = Vector3.zero;
        bool has = false;
        for (int seg = 0; seg < segments; seg++)
        {
            Transform p0 = waypoints[GetIndex(seg - 1, count)];
            Transform p1 = waypoints[GetIndex(seg, count)];
            Transform p2 = waypoints[GetIndex(seg + 1, count)];
            Transform p3 = waypoints[GetIndex(seg + 2, count)];
            if (!p0 || !p1 || !p2 || !p3) continue;
            for (int i = 0; i <= 10; i++)
            {
                Vector3 pt = CatmullRom(p0.position, p1.position, p2.position, p3.position, i / 10f);
                if (has) Gizmos.DrawLine(prev, pt);
                prev = pt; has = true;
            }
        }
    }
}