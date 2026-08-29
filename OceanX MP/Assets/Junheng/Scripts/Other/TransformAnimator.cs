using System;
using UnityEngine;

namespace OceanX
{
    /// <summary>
    /// Component that animates the position of the referenced <see cref="Transform"/> component
    /// over time, to follow the selected shape pattern.
    /// </summary>
    public class TransformAnimator : MonoBehaviour
    {
        /// <summary>
        /// Enumeration specifying currently supported movement patterns for the 
        /// animation of the transform component.
        /// </summary>
        [Serializable]
        public enum PositionAnimationShape : byte
        {
            Rectangle = 0,
            Circle = 1, 
            Line = 2
        }

        [Header("References: ")]
        public Transform TargetTransform = null;

        [Header("Settings: ")]
        public PositionAnimationShape AnimationShape = PositionAnimationShape.Rectangle;
        public float MovementSpeed = 2f;
        public float AnimationSpeedMultiplier = 1.0f;
        public float EdgeReachingDistance = 0.5f;
        public bool MovingClockwise = false;
        [Header("Rectangle Shape Settings: ")]
        public float RectangleWidth = 20f;
        public float RectangleLength = 10f;

        [Header("Circle Shape Settings: ")]
        public float CircleRadius = 25f;
        public int CircleResolution = 24;

        [Header("Line Shape Settings: ")]
        public float LineLength = 50f;

        // Rectangle movement pattern properties.
        private Vector3 _nextRectangleTargetPoint = Vector3.zero;

        // Circle movement pattern properties.
        private float _currentCircleTargetPointAngle = 0f;
        private Vector3 _nextCircleTargetPoint = Vector3.zero;

        // Line movement pattern properties.
        private Vector3 _nextLineTargetPoint = Vector3.zero;

        private Vector3 RectangleBottomLeft  { get => transform.position - transform.right * RectangleWidth * 0.5f - transform.forward * RectangleLength * 0.5f; }
        private Vector3 RectangleBottomRight { get => transform.position + transform.right * RectangleWidth * 0.5f - transform.forward * RectangleLength * 0.5f; }
        private Vector3 RectangleTopLeft     { get => transform.position - transform.right * RectangleWidth * 0.5f + transform.forward * RectangleLength * 0.5f; }
        private Vector3 RectangleTopRight    { get => transform.position + transform.right * RectangleWidth * 0.5f + transform.forward * RectangleLength * 0.5f; }

        private void OnDrawGizmosSelected()
        {
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;

            Gizmos.color = Color.green;
            switch (AnimationShape)
            {
                case PositionAnimationShape.Rectangle:
                    Gizmos.DrawWireCube(Vector3.zero, new Vector3(RectangleWidth, 0f, RectangleLength));
                    break;
                case PositionAnimationShape.Circle:
                    Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, new Vector3(transform.localScale.x, 0.0f, transform.localScale.z));
                    Gizmos.DrawWireSphere(Vector3.zero, CircleRadius);
                    break;
                case PositionAnimationShape.Line:
                    Gizmos.DrawWireCube(Vector3.zero, new Vector3(LineLength, 0f, 0f));
                    break;
                default:
                    break;
            }

            Gizmos.matrix = previousMatrix;
        }

        private void Start()
        {
            ResetAnimation();
        }

        /// <summary>
        /// Snaps the target back onto the start of this animator's path and re-seeds the waypoint state to
        /// match. Runs on <see cref="Start"/>, and again whenever the target has to be handed BACK to the
        /// animator after something else moved it — see <c>EcosystemTargetGPU.Unpark</c>, where a school
        /// that was parked at an off-screen exit point is recalled.
        ///
        /// This has to reposition the target, not just re-enable the animation. The Move* methods below
        /// step the target from wherever it currently IS toward the next waypoint; they never snap to the
        /// path. So a target left at an exit point simply crawls home from out there at MovementSpeed,
        /// dragging its school along with it — the fish hang around the exit gate instead of rejoining the
        /// reef. Re-seeding the waypoint at the same time matters too: position and next-waypoint are a
        /// pair, and setting only the position would have the target set off toward a stale corner.
        /// </summary>
        public void ResetAnimation()
        {
            if (TargetTransform == null)
            {
                return;
            }

            // Initialize the starting position of the target transform and all required movement properties based on the
            // movement pattern that the transform will follow.
            Vector3 shapeCenter = transform.position;
            Vector3 shapeRightAxis = transform.right;
            switch (AnimationShape)
            {
                case PositionAnimationShape.Rectangle:
                    // Initialize movement with the target transform being placed in the bottom-right corner of the rectangle and 
                    // setting its destination to the next corner of the rectangle, depending on the movement direction.
                    TargetTransform.position = RectangleBottomRight;
                    _nextRectangleTargetPoint = MovingClockwise ? RectangleBottomLeft : RectangleTopRight;
                    break;
                case PositionAnimationShape.Circle:
                    // Initialize movement by placing the target transform at the top point of the circle and setting the
                    // destination point at the same point so it would be automatically updated to the next when the simulation starts.
                    TargetTransform.position = shapeCenter + shapeRightAxis * CircleRadius;
                    _nextCircleTargetPoint = shapeCenter + shapeRightAxis * CircleRadius;
                    _currentCircleTargetPointAngle = 0f; // re-seed the sweep too, so a re-run starts a clean lap
                    break;
                case PositionAnimationShape.Line:
                    // Initialize movement with the target transform placed in the center of the line and setting
                    // its destination to the right side of the line.
                    TargetTransform.position = shapeCenter - shapeRightAxis * LineLength * 0.5f;
                    _nextLineTargetPoint = shapeCenter + shapeRightAxis * LineLength * 0.5f;
                    break;
                default:
                    break;
            }
        }

        private void Update()
        {
            switch (AnimationShape)
            {
                case PositionAnimationShape.Rectangle:
                    MoveInRectangleShape();
                    break;
                case PositionAnimationShape.Circle:
                    MoveInCircularShape();
                    break;
                case PositionAnimationShape.Line:
                    MoveInLineShape();
                    break;
                default:
                    break;
            }
        }

        public void SetupInitialTargetPosition()
        {
            if(TargetTransform == null)
            {
                return;
            }

            TargetTransform.position = GetInitialPosition();
        }

        /// <summary>
        /// Function calculates the initial position that the transform will have at the beginning of the animation.
        /// </summary>
        /// <returns>Initial world position at the beginning of the animation.</returns>
        private Vector3 GetInitialPosition()
        {
            Vector3 shapeCenter = transform.position;
            Vector3 shapeRightAxis = transform.right;
            Vector3 shapeForwardAxis = transform.forward;

            switch (AnimationShape)
            {
                case PositionAnimationShape.Rectangle:
                    return shapeCenter + shapeRightAxis * RectangleWidth * 0.5f - shapeForwardAxis * RectangleLength * 0.5f;
                case PositionAnimationShape.Circle:
                    return shapeCenter + shapeRightAxis * CircleRadius;
                case PositionAnimationShape.Line:
                    return shapeCenter - shapeRightAxis * LineLength * 0.5f;
                default:
                    return shapeCenter;
            }
        }

        #region RECTANGLE_SHAPE_MOVEMENT

        private void MoveInRectangleShape()
        {
            Vector3 currentTransformPosition = TargetTransform.position;
            if((_nextRectangleTargetPoint - currentTransformPosition).magnitude <= EdgeReachingDistance)
            {
                // The target transform has reached a corner of the rectangle, switch the movement destination to the next corner.
                SwitchToNextRectangleTargetPoint();
            }

            Vector3 movementDirection = (_nextRectangleTargetPoint - currentTransformPosition).normalized;
            currentTransformPosition += movementDirection * MovementSpeed * AnimationSpeedMultiplier * Time.deltaTime;
            TargetTransform.position = currentTransformPosition;
        }

        private void SwitchToNextRectangleTargetPoint()
        {
            Vector3 targetPointInLocalSpace = transform.InverseTransformPoint(_nextRectangleTargetPoint);
            bool isOnTheRight = targetPointInLocalSpace.x > 0f;
            bool isOnTheBottom = targetPointInLocalSpace.z < 0f;

            if (isOnTheRight && isOnTheBottom)
            {
                // Bottom-Right vertex. Updating target to the Top-Right or Bottom-Left vertex of the rectangle.
                _nextRectangleTargetPoint = MovingClockwise ? RectangleBottomLeft : RectangleTopRight;
            }
            else if(isOnTheRight)
            {
                // Top-Right vertex. Updating target to the Top-Left or Bottom-Right vertex of the rectangle.
                _nextRectangleTargetPoint = MovingClockwise ? RectangleBottomRight : RectangleTopLeft;
            }
            else if(isOnTheBottom)
            {
                // Bottom-Left vertex. Updating target to the Bottom-Right vertex of the rectangle.
                _nextRectangleTargetPoint = MovingClockwise ? RectangleTopLeft : RectangleBottomRight;
            }
            else
            {
                // Top-Left vertex. Updating target to the Bottom-Left vertex of the rectangle.
                _nextRectangleTargetPoint = MovingClockwise ? RectangleTopRight : RectangleBottomLeft;
            }
        }

        #endregion // RECTANGLE_SHAPE_MOVEMENT

        #region CIRCLE_SHAPE_MOVEMENT

        private void MoveInCircularShape()
        {
            Vector3 currentTransformPosition = TargetTransform.position;
            if ((_nextCircleTargetPoint - currentTransformPosition).magnitude <= EdgeReachingDistance)
            {
                // The target transform has reached a corner of the rectangle, switch the movement destination to the next corner.
                SwitchToNextPointOnCircle();
            }

            Vector3 movementDirection = (_nextCircleTargetPoint - currentTransformPosition).normalized;
            currentTransformPosition += movementDirection * MovementSpeed * AnimationSpeedMultiplier * Time.deltaTime;
            TargetTransform.position = currentTransformPosition;
        }

        private void SwitchToNextPointOnCircle()
        {
            float angleDelta = 360.0f / CircleResolution;
            float angleDirectionMultiplier = MovingClockwise ? -1.0f : 1.0f;
            float nextTargetPointAngle = (_currentCircleTargetPointAngle + angleDelta * angleDirectionMultiplier) % 360.0f;
            float nextTargetPointAngleRadians = Mathf.Deg2Rad * nextTargetPointAngle;
            float circlePositionX = Mathf.Cos(nextTargetPointAngleRadians) * CircleRadius;
            float circlePositionZ = Mathf.Sin(nextTargetPointAngleRadians) * CircleRadius;
            Vector3 targetPointInLocalSpace = new Vector3(circlePositionX, 0f, circlePositionZ);

            _nextCircleTargetPoint = transform.TransformPoint(targetPointInLocalSpace);
            _currentCircleTargetPointAngle = nextTargetPointAngle;
        }

        #endregion // CIRCLE_SHAPE_MOVEMENT

        #region LINE_SHAPE_MOVEMENT

        private void MoveInLineShape()
        {
            Vector3 currentTransformPosition = TargetTransform.position;
            if ((_nextLineTargetPoint - currentTransformPosition).magnitude <= EdgeReachingDistance)
            {
                // The target transform has reached a corner of the rectangle, switch the movement destination to the next corner.
                SwitchToNextLineTargetPoint();
            }

            Vector3 movementDirection = (_nextLineTargetPoint - currentTransformPosition).normalized;
            currentTransformPosition += movementDirection * MovementSpeed * AnimationSpeedMultiplier * Time.deltaTime;
            TargetTransform.position = currentTransformPosition;
        }

        private void SwitchToNextLineTargetPoint()
        {
            Vector3 lineCenter = transform.position;
            Vector3 lineRightAxis = transform.right;
            Vector3 targetPointInLocalSpace = transform.InverseTransformPoint(_nextLineTargetPoint);
            bool isOnTheRight = targetPointInLocalSpace.x > 0f;

            if (isOnTheRight)
            {
                _nextLineTargetPoint = lineCenter - lineRightAxis * LineLength * 0.5f;
            }
            else
            {
                _nextLineTargetPoint = lineCenter + lineRightAxis * LineLength * 0.5f;
            }
        }

        #endregion // LINE_SHAPE_MOVEMENT
    }
}