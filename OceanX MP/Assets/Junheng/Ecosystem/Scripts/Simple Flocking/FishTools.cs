using System;
using UnityEngine;

public static class FishTools
{
  // Prevents fish from collapsing into a single point. Works with cohesion force.
  public struct SeparationForce
  {
    public SeparationForce( Fish.Settings sts )
    {
      // Compensate cohesion force which at OptDistance equals OptDistance / 2
      optFactor = sts.OptDistance * sts.OptDistance / 2;
    }

    public bool Calc( Vector3 cur, Vector3 other, out Vector3 force )
    {
      var revDir = cur - other;
      var sqrDist = revDir.sqrMagnitude;

      force = Vector3.zero;

      if( sqrDist < MathTools.sqrEpsilon ) // ignore self
        return false;

      force = revDir * ( optFactor / sqrDist );
      return true;
    }

    public float Calc( float dist )
    {
      return optFactor / dist;
    }

    readonly float optFactor;
  };


  // Force between fish and obstacles (reefs, rocks, cage walls, etc.)
  public struct CollisionAvoidanceForce
  {
    public CollisionAvoidanceForce( Fish.Settings sts, float sepForceAtOptDistance )
    {
      optDistance = sts.OptDistance;

      #if COLLISION_AVOIDANCE_SQUARE
        var ViewRadius2 = sts.ViewRadius * sts.ViewRadius;
        var OptDistance2 = sts.OptDistance * sts.OptDistance;
        factor1 = ViewRadius2;
        factor2 = -2 * sts.SpeedMultipliyer * sepForceAtOptDistance * OptDistance2 / ( OptDistance2 - ViewRadius2 );
      #else
        factor1 = sts.ViewRadius;
        factor2 = -2 * sts.SpeedMultipliyer * sepForceAtOptDistance * sts.OptDistance / ( sts.OptDistance - sts.ViewRadius );
      #endif
    }

    public struct Force
    {
      public Vector3 dir;
      public Vector3 pos;
    };

    public bool Calc( Vector3 cur, Vector3 fishDir, Collider cld, out Force force )
    {
      var pointOnBounds = MathTools.CalcPointOnBounds( cld, cur );
      var revDir = cur - pointOnBounds;
      var dist = revDir.magnitude;

      if( dist <= MathTools.epsilon )
      {
        revDir = (pointOnBounds - cld.transform.position).normalized;
        dist = 0.1f * optDistance;
      }
      else
        revDir /= dist;

      force.dir = revDir * ( CalcImpl(dist) * MathTools.AngleToFactor(revDir, fishDir) );
      force.pos = pointOnBounds;
      return true;
    }

    #if COLLISION_AVOIDANCE_SQUARE
      float CalcImpl( float dist )
      {
        return factor2 * (factor1 / (dist * dist) - 1);
      }
    #else
      float CalcImpl( float dist )
      {
        return factor2 * (factor1 / dist - 1);
      }
    #endif

    readonly float factor1;
    readonly float factor2;
    readonly float optDistance;
  };
}
