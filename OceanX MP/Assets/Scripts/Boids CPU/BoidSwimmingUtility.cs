using UnityEngine;

public static class BoidSwimmingUtility
{
    public static BoidInfo InitializeBoid(Transform boidTransform, BoidMovementProperties movement)
    {
        return new BoidInfo
        {
            Position         = boidTransform.position,
            MovementDirection = boidTransform.forward,
            Speed            = movement.CruisingSpeed,
            Acceleration     = 0f,
            AngularAcceleration = 0f,
            AngularVelocity  = 0f,
        };
    }

    // Rotates the fish toward targetDirection using jerk-limited angular physics.
    // A fraction of angular acceleration is transferred to forward acceleration.
    public static void UpdateMovementDirection(Vector3 targetDirection, float timeDelta,
        BoidMovementProperties movement, ref BoidInfo boidInfo)
    {
        Vector3 currentDir        = boidInfo.MovementDirection;
        float   currentAccel      = boidInfo.Acceleration;
        float   currentAngAccel   = boidInfo.AngularAcceleration;
        float   currentAngVel     = boidInfo.AngularVelocity;

        Quaternion targetRot  = Quaternion.LookRotation(targetDirection, Vector3.up);
        Quaternion currentRot = Quaternion.LookRotation(currentDir, Vector3.up);
        float angleToTarget   = Quaternion.Angle(currentRot, targetRot);

        // Ramp up angular acceleration with jerk when we need to turn more than we currently are.
        float angularJerk       = currentAngVel < angleToTarget
            ? Mathf.Lerp(0f, movement.AngularJerk, (angleToTarget - currentAngVel) / 50f)
            : 0f;
        float angularDecel      = angularJerk > 0f ? 0f : movement.AngularDeceleration;
        currentAngAccel         = Mathf.Clamp(currentAngAccel + (angularJerk - angularDecel) * timeDelta,
            0f, movement.MaxAngularAcceleration);
        currentAngVel           = Mathf.Clamp(currentAngVel + currentAngAccel * timeDelta,
            0f, movement.MaxAngularVelocity);

        Quaternion newRot = Quaternion.RotateTowards(currentRot, targetRot, currentAngVel * timeDelta);

        if (angularJerk <= 0f)
        {
            currentAngVel = Mathf.Clamp(currentAngVel - movement.AngularVelocityReduction * timeDelta,
                0f, movement.MaxAngularVelocity);
        }

        // Turning pushes the fish forward slightly.
        currentAccel += currentAngAccel * movement.RotationEffectOnSpeed * timeDelta;

        boidInfo.Acceleration       = currentAccel;
        boidInfo.MovementDirection  = newRot * Vector3.forward;
        boidInfo.AngularVelocity    = currentAngVel;
        boidInfo.AngularAcceleration = currentAngAccel;
    }

    // Maintains cruising speed via friction model; accelerates when shouldAccelerate is true.
    public static void UpdateMovementSpeed(bool shouldAccelerate, float timeDelta,
        BoidMovementProperties movement, ref BoidInfo boidInfo)
    {
        float accel = boidInfo.Acceleration;
        float speed = boidInfo.Speed;

        float jerk  = shouldAccelerate ? movement.MovementJerk : 0f;
        float decel = jerk > 0f ? 0f : movement.Deceleration;
        accel = Mathf.Clamp(accel + (jerk - decel) * timeDelta, 0f, movement.MaxAcceleration);

        // Keep at least cruising speed.
        float projectedSpeed = speed + (accel - movement.WaterFriction * speed) * timeDelta;
        if (projectedSpeed < movement.CruisingSpeed)
            accel = movement.CruisingSpeed * movement.WaterFriction;

        speed += (accel - movement.WaterFriction * speed) * timeDelta;
        speed  = Mathf.Clamp(speed, movement.CruisingSpeed, movement.MaxSpeed);

        boidInfo.Acceleration = accel;
        boidInfo.Speed        = speed;
    }

    public static void UpdatePositionAndRotation(float timeDelta, Transform transform, ref BoidInfo boidInfo)
    {
        Vector3    dir         = boidInfo.MovementDirection;
        Vector3    newPosition = boidInfo.Position + dir * (timeDelta * boidInfo.Speed);
        Quaternion newRotation = Quaternion.LookRotation(dir, Vector3.up);
        transform.SetPositionAndRotation(newPosition, newRotation);
        boidInfo.Position = newPosition;
    }

    public static bool WillLeaveSimulationArea(Vector3 position, Vector3 direction,
        float speed, Bounds bounds)
    {
        if (!bounds.Contains(position))
            return true;

        return !bounds.Contains(position + direction.normalized * speed);
    }
}
