using UnityEngine;

public static class MathTools
{
  public const float epsilon = 1e-10f;
  public const float sqrEpsilon = 1e-10f;

  // Returns the closest point to cur on the bounds of cld
  public static Vector3 CalcPointOnBounds( Collider cld, Vector3 cur )
  {
    SphereCollider sphc = cld as SphereCollider;

    if( !sphc )
      return cld.ClosestPointOnBounds( cur );

    // ClosestPointOnBounds is imprecise for spheres, so calculate manually
    var realPos    = sphc.transform.position + sphc.center;
    var dir        = cur - realPos;
    var realScale  = sphc.transform.lossyScale;
    var realRadius = sphc.radius * Mathf.Max( realScale.x, realScale.y, realScale.z );
    var dirLength  = dir.magnitude;

    if( dirLength < realRadius )
      return cur;

    return realPos + (realRadius / dirLength) * dir;
  }

  // Projects vec onto XZ plane and rotates 90 degrees right
  public static Vector3 RightVectorXZProjected( Vector3 vec )
  {
    // http://en.wikipedia.org/wiki/Rotation_matrix#Basic_rotations
    return new Vector3( vec.z, 0, -vec.x );
  }

  // Returns signed magnitude of vec projected onto vecNormal
  public static float VecProjectedLength( Vector3 vec, Vector3 vecNormal )
  {
    var proj = Vector3.Project( vec, vecNormal );
    return proj.magnitude * Mathf.Sign( Vector3.Dot(proj, vecNormal) );
  }

  // Check that a Quaternion is not NaN
  public static bool IsValid( Quaternion q )
  {
    #pragma warning disable 1718
    return q == q;
    #pragma warning restore 1718
  }

  // Maps the angle between two normalized vectors [0..Pi] to [0..1]
  public static float AngleToFactor( Vector3 a, Vector3 b )
  {
    return ( 1 - Vector3.Dot(a, b) ) / 2;
  }
}
