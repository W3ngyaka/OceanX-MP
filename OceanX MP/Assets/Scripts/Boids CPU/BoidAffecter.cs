using UnityEngine;

public enum BoidAffecterType { Target, Obstacle }

// Place this on any GameObject to make it act as a target (attractor) or
// obstacle (repeller) for boids. Set BoidGroupId to -1 to affect all groups.
public class BoidAffecter : MonoBehaviour
{
    public BoidAffecterType Type      = BoidAffecterType.Target;
    public float            Radius    = 2f;
    public int              BoidGroupId = -1;

    public Vector3 Position => transform.position;

    public bool AffectsGroup(int groupId)
    {
        return BoidGroupId < 0 || BoidGroupId == groupId;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Type == BoidAffecterType.Target
            ? new Color(0f, 1f, 0f, 0.4f)
            : new Color(1f, 0f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, Radius);
    }
}
