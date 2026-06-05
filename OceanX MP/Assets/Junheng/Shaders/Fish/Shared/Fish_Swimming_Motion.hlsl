#ifndef FISH_SWIMMING_MOTION
#define FISH_SWIMMING_MOTION

float3 RotateAroundPivot3D(float3 position, float3 rotationAxis, float angleInDegrees) 
{
    // Get sin and cosine of an angle.
    float angleInRadians = radians(angleInDegrees);
    float angleCos, angleSin;
    sincos(angleInRadians, angleSin, angleCos);

    // Making sure that the rotation axis is normalized.
    rotationAxis = normalize(rotationAxis);

    // Return the rotated point based on the Euler's rotation formula.
    return position * angleCos + 
           cross(rotationAxis, position) * angleSin + 
           rotationAxis * dot(rotationAxis, position) * (1.0 - angleCos);
}

float2 RotateAroundPivot2D(float2 position, float angleInDegrees)
{
    // Get sin and cosine of an angle.
    float angleInRadians = radians(angleInDegrees);
    float angleCos, angleSin;
    sincos(angleInRadians, angleSin, angleCos);

    // Apply a 2D rotation matrix to the position to get the rotated position.
    return float2(
        position.x * angleCos - position.y * angleSin,
        position.x * angleSin + position.y * angleCos
    );
}

float3 ApplyFishSwimmingMotion(float3 originalLocalSpacePosition, float2 tailMaskUV, float currentSwimTime, float sideToSideAmplitude, 
    float yawRotationAmplitude, float rollRotationAmplitude, float panningYawAmplitude) 
{
    // Mask used to control the strength of the roll and panning rotations to mask them only on the tail of the fish.
    float tailMask = saturate(pow(1.0 - tailMaskUV.x, _TailMaskFalloff));

    // Calculating swim time for this fish. Multipliying the swim speed with 2 * PI (6.28) so that the controls can be in range 0-1.
    float swimTime = (_AutomaticSwimVisualization > 0.0 ? _Time.y * _AutomaticSwimSpeed : currentSwimTime) * 6.28;

    // Yaw rotation around the pivot of the fish.
    float yawRotationAngle = yawRotationAmplitude * 360.0 * sin(swimTime + _YawRotationOffset);
    float3 yawRotatedPosition = RotateAroundPivot3D(originalLocalSpacePosition, float3(0, 1, 0), yawRotationAngle);

    // DEBUG VISUALIZATION: Yaw Rotation
    // return yawRotatedPosition;

    // Roll rotation along the spine.
    float rollAngle = rollRotationAmplitude * 360.0 * sin(swimTime + originalLocalSpacePosition.z / _TailWaveLength + _TailRollOffset) * tailMask;
    float3 rollRotatedPosition = RotateAroundPivot3D(originalLocalSpacePosition, float3(0, 0, 1), rollAngle);

    // DEBUG VISUALIZATION: Roll Rotation
    // return rollRotatedPosition;

    // Panning Yaw rotation along the spline.
    float panningYawAngle = panningYawAmplitude * 360.0 * sin(swimTime + originalLocalSpacePosition.z / _TailWaveLength) * tailMask;
    float3 panningYawPosition = RotateAroundPivot3D(originalLocalSpacePosition, float3(0, 1, 0), panningYawAngle);

    // DEBUG VISUALIZATION: Panning Roll Rotation
    // return panningYawPosition;

    // Applying side-to-side movement to the fish. 
    // Scaling the movement by 1/100 since this is the conversion multiplier between Blender and Unity.
    float sideToSideMovement = sideToSideAmplitude * 0.01 * sin(swimTime + _SideToSideOffset);

    // DEBUG VISUALIZATION: Side To Side Movement
    // return originalLocalSpacePosition + float3(sideToSideMovement, 0, 0);

    // Combining all movements into one single motion.
    float3 yawRotationOffset = (yawRotatedPosition - originalLocalSpacePosition);
    float3 rollRotationOffset = (rollRotatedPosition - originalLocalSpacePosition);
    float3 panningYawOffset = (panningYawPosition - originalLocalSpacePosition);
    float3 totalSwimmingMotionOffset = yawRotationOffset + rollRotationOffset + panningYawOffset  + float3(sideToSideMovement, 0, 0);

    // Offseting the original vertex position to produce swimming motion. 
    return originalLocalSpacePosition + totalSwimmingMotionOffset;
}

float3 ApplyFishSwimmingMotion(float3 originalLocalSpacePosition, float2 tailMaskUV) 
{
    return ApplyFishSwimmingMotion(originalLocalSpacePosition, tailMaskUV, _CurrentSwimTime, _SideToSideAmplitude, 
        _YawRotationAmplitude, _TailRollAmplitude, _TailYawAmplitude);
}

#endif // FISH_SWIMMING_MOTION