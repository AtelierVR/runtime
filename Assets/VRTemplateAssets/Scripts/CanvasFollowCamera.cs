using UnityEngine;

namespace Unity.VRTemplate
{
    /// <summary>
    /// Makes a World Space Canvas follow and stay in front of the camera.
    /// Useful for spatial notifications, HUDs, and panels that need to remain
    /// visible in XR (Meta Quest, AndroidXR, MetaXR).
    /// 
    /// Inspired by the XR Template follow patterns and PortablePanel constraint system.
    /// </summary>
    [AddComponentMenu("XR/Canvas Follow Camera")]
    [DefaultExecutionOrder(100)] // Run after camera tracking updates
    public class CanvasFollowCamera : MonoBehaviour
    {
        [Header("Camera Reference")]
        [Tooltip("The camera to follow. Assign the XR camera (e.g., XROrigin's Camera).")]
        [SerializeField] private Camera _targetCamera;

        [Header("Positioning")]
        [Tooltip("Distance from the camera in meters.")]
        [SerializeField] private float _distance = 1.5f;

        [Tooltip("Vertical offset relative to the camera's forward direction. Positive = above, Negative = below.")]
        [SerializeField] private float _verticalOffset = 0f;

        [Tooltip("Horizontal offset relative to the camera's forward direction.")]
        [SerializeField] private float _horizontalOffset = 0f;

        [Header("Smoothing")]
        [Tooltip("How smoothly the canvas follows the camera. Higher values = faster follow. Set to 0 for instant snap.")]
        [Range(0f, 30f)]
        [SerializeField] private float _smoothSpeed = 10f;

        [Tooltip("Maximum angular speed when rotating toward the camera (degrees/sec). Set to 0 for instant rotation.")]
        [Range(0f, 360f)]
        [SerializeField] private float _maxRotationSpeed = 120f;

        [Header("Angle Clamping")]
        [Tooltip("Maximum angle (degrees) the canvas can lag behind the camera's forward direction. " +
                 "If exceeded, the canvas snaps instantly to stay within the view cone. " +
                 "Prevents the canvas from flipping when the head moves quickly.")]
        [Range(10f, 180f)]
        [SerializeField] private float _maxAngle = 90f;

        [Tooltip("Speed multiplier used when the canvas is outside the max angle cone " +
                 "(relative to normal smoothSpeed). Higher = faster recovery. Set to 0 to disable angle clamping.")]
        [Range(0f, 10f)]
        [SerializeField] private float _angleRecoveryBoost = 3f;

        [Header("Behavior")]
        [Tooltip("If true, the canvas will face the camera (billboard effect).")]
        [SerializeField] private bool _faceCamera = true;

        [Tooltip("If true, the canvas will only update its position (rotation stays independent).")]
        [SerializeField] private bool _positionOnly = false;

        [Tooltip("If true, the follow will be enabled on Start.")]
        [SerializeField] private bool _followOnStart = true;

        [Header("Advanced")]
        [Tooltip("If true, uses LateUpdate instead of Update for smoother camera following.")]
        [SerializeField] private bool _useLateUpdate = true;

        // Internal state
        private bool _isFollowing;
        private Vector3 _velocityRef;
        private Quaternion _targetRotation;

        // Cached offset in camera-local space (when using View constraint-like behavior)
        private Vector3 _cachedLocalOffset;
        private Quaternion _cachedLocalRotation;
        private bool _hasCachedOffsets;

        /// <summary>
        /// The camera being followed.
        /// </summary>
        public Camera TargetCamera
        {
            get => _targetCamera;
            set => _targetCamera = value;
        }

        /// <summary>
        /// Distance from the camera in meters.
        /// </summary>
        public float Distance
        {
            get => _distance;
            set => _distance = Mathf.Max(0.1f, value);
        }

        /// <summary>
        /// How smoothly the canvas follows (0 = instant).
        /// </summary>
        public float SmoothSpeed
        {
            get => _smoothSpeed;
            set => _smoothSpeed = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Is the canvas currently following the camera?
        /// </summary>
        public bool IsFollowing => _isFollowing;

        /// <summary>
        /// Maximum angle (degrees) the canvas can lag behind the camera's forward direction
        /// before snapping/recovering. Set to 0 to disable.
        /// </summary>
        public float MaxAngle
        {
            get => _maxAngle;
            set => _maxAngle = Mathf.Clamp(value, 0f, 180f);
        }

        /// <summary>
        /// Speed boost multiplier when canvas exceeds MaxAngle (relative to SmoothSpeed).
        /// Set to 10+ for instant snap.
        /// </summary>
        public float AngleRecoveryBoost
        {
            get => _angleRecoveryBoost;
            set => _angleRecoveryBoost = Mathf.Max(0f, value);
        }

        private void Start()
        {
            if (_followOnStart)
            {
                StartFollowing();
            }
        }

        private void Update()
        {
            if (!_useLateUpdate && _isFollowing)
            {
                FollowCamera(Time.deltaTime);
            }
        }

        private void LateUpdate()
        {
            if (_useLateUpdate && _isFollowing)
            {
                FollowCamera(Time.deltaTime);
            }
        }

        /// <summary>
        /// Start making the canvas follow the camera.
        /// </summary>
        public void StartFollowing()
        {
            _isFollowing = true;
            CacheOffsets();
        }

        /// <summary>
        /// Stop following the camera. The canvas stays at its current position.
        /// </summary>
        public void StopFollowing()
        {
            _isFollowing = false;
        }

        /// <summary>
        /// Toggle follow on/off.
        /// </summary>
        public void ToggleFollowing()
        {
            if (_isFollowing)
                StopFollowing();
            else
                StartFollowing();
        }

        /// <summary>
        /// Instantly snaps the canvas in front of the camera, then starts following.
        /// </summary>
        public void SnapAndFollow()
        {
            CacheOffsets();
            _isFollowing = true;
            SnapToCamera();
        }

        /// <summary>
        /// Places the canvas directly in front of the camera at the configured distance.
        /// Same pattern as PortablePanel.PlacePanelInFrontOfPlayer.
        /// </summary>
        public void SnapToCamera()
        {
            Camera cam = GetCamera();
            if (cam == null) return;

            Transform camTransform = cam.transform;
            Vector3 targetPos = camTransform.position
                + camTransform.forward * _distance
                + camTransform.up * _verticalOffset
                + camTransform.right * _horizontalOffset;

            transform.position = targetPos;

            if (_faceCamera && !_positionOnly)
            {
                FaceCameraInstant(camTransform);
            }
        }

        /// <summary>
        /// Recalculates the offset between the canvas and the camera.
        /// Call this after manually moving the canvas to a desired position.
        /// Similar to PortablePanel.CacheConstraintOffsets.
        /// </summary>
        public void CacheOffsets()
        {
            Camera cam = GetCamera();
            if (cam == null) return;

            Transform camTransform = cam.transform;

            // Store offset in camera-local space for View-constrained follow
            _cachedLocalOffset = Quaternion.Inverse(camTransform.rotation)
                * (transform.position - camTransform.position);
            _cachedLocalRotation = Quaternion.Inverse(camTransform.rotation)
                * transform.rotation;
            _hasCachedOffsets = true;
        }

        /// <summary>
        /// Uses the cached camera-local offset to position the canvas relative to the camera.
        /// Similar to PortablePanel.ApplyPlayerConstraint (View mode).
        /// </summary>
        public void ApplyViewConstraint()
        {
            Camera cam = GetCamera();
            if (cam == null) return;

            Transform camTransform = cam.transform;

            if (!_hasCachedOffsets)
            {
                // Fall back to default position in front of camera
                Vector3 targetPos = camTransform.position
                    + camTransform.forward * _distance
                    + camTransform.up * _verticalOffset
                    + camTransform.right * _horizontalOffset;

                transform.position = SmoothPosition(transform.position, targetPos, Time.deltaTime);

                if (_faceCamera && !_positionOnly)
                {
                    SmoothFaceCamera(camTransform, Time.deltaTime);
                }
            }
            else
            {
                // Use cached offset in camera-local space
                Vector3 targetPos = camTransform.position
                    + camTransform.rotation * _cachedLocalOffset;
                Quaternion targetRot = camTransform.rotation * _cachedLocalRotation;

                transform.position = SmoothPosition(transform.position, targetPos, Time.deltaTime);

                if (_faceCamera && !_positionOnly)
                {
                    transform.rotation = SmoothRotation(transform.rotation, targetRot, Time.deltaTime);
                }
            }
        }

        private void FollowCamera(float deltaTime)
        {
            Camera cam = GetCamera();
            if (cam == null) return;

            Transform camTransform = cam.transform;

            // Calculate target position: in front of camera + offsets
            Vector3 targetPos = camTransform.position
                + camTransform.forward * _distance
                + camTransform.up * _verticalOffset
                + camTransform.right * _horizontalOffset;

            // Check if the canvas has fallen outside the max angle cone
            float angle = ComputeAngleFromCameraForward(camTransform);
            float effectiveSpeed = _smoothSpeed;

            if (_maxAngle > 0f && angle > _maxAngle)
            {
                // Canvas is too far behind — use boosted speed or snap
                if (_angleRecoveryBoost >= 10f)
                {
                    // Boost of 10+ means instant snap
                    transform.position = targetPos;
                }
                else
                {
                    effectiveSpeed = _smoothSpeed * _angleRecoveryBoost;
                    transform.position = SmoothPosition(transform.position, targetPos, deltaTime, effectiveSpeed);
                }
            }
            else
            {
                // Normal smooth follow
                transform.position = SmoothPosition(transform.position, targetPos, deltaTime, effectiveSpeed);
            }

            // Rotation
            if (!_positionOnly)
            {
                if (_faceCamera)
                {
                    SmoothFaceCamera(camTransform, deltaTime);
                }
            }
        }

        /// <summary>
        /// Returns the angle (in degrees) between the camera's forward direction
        /// and the direction from camera to canvas. 0° = perfectly in front.
        /// </summary>
        private float ComputeAngleFromCameraForward(Transform camTransform)
        {
            Vector3 camToCanvas = transform.position - camTransform.position;

            if (camToCanvas.sqrMagnitude < 0.001f)
                return 0f;

            return Vector3.Angle(camTransform.forward, camToCanvas.normalized);
        }

        private Vector3 SmoothPosition(Vector3 current, Vector3 target, float deltaTime, float speedOverride = -1f)
        {
            float speed = speedOverride >= 0f ? speedOverride : _smoothSpeed;

            if (speed <= 0f)
                return target;

            // Using SmoothDamp for nicer easing, but with a separate velocity per speed
            float smoothTime = 1f / speed;
            return Vector3.SmoothDamp(current, target, ref _velocityRef, smoothTime, Mathf.Infinity, deltaTime);
        }

        private Quaternion SmoothRotation(Quaternion current, Quaternion target, float deltaTime)
        {
            if (_maxRotationSpeed <= 0f)
                return target;

            return Quaternion.RotateTowards(current, target, _maxRotationSpeed * deltaTime);
        }

        private void SmoothFaceCamera(Transform camTransform, float deltaTime)
        {
            // Billboard: face the camera, then flip to look toward the player
            Vector3 camPosition = camTransform.position;
            Vector3 directionToCamera = camPosition - transform.position;

            if (directionToCamera.sqrMagnitude > 0.001f)
            {
                _targetRotation = Quaternion.LookRotation(-directionToCamera, camTransform.up);
                transform.rotation = SmoothRotation(transform.rotation, _targetRotation, deltaTime);
            }
        }

        private void FaceCameraInstant(Transform camTransform)
        {
            Vector3 camPosition = camTransform.position;
            Vector3 directionToCamera = camPosition - transform.position;

            if (directionToCamera.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(-directionToCamera, camTransform.up);
            }
        }

        /// <summary>
        private Camera GetCamera()
        {
            if (_targetCamera != null)
                return _targetCamera;

            _targetCamera = Camera.main;
            if (_targetCamera != null)
                return _targetCamera;

            return Camera.current;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _distance = Mathf.Max(0.1f, _distance);
            _maxAngle = Mathf.Clamp(_maxAngle, 0f, 180f);
            _angleRecoveryBoost = Mathf.Max(0f, _angleRecoveryBoost);
        }

        private void OnDrawGizmosSelected()
        {
            Camera cam = GetCamera();
            if (cam == null) return;

            Transform camTransform = cam.transform;
            Vector3 previewPos = camTransform.position
                + camTransform.forward * _distance
                + camTransform.up * _verticalOffset
                + camTransform.right * _horizontalOffset;

            if (!_isFollowing && !Application.isPlaying)
            {
                // Preview where the canvas would be placed
                Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
                Gizmos.DrawWireSphere(previewPos, 0.1f);
                Gizmos.DrawLine(camTransform.position, previewPos);
            }

            // Draw the max angle cone
            if (_maxAngle > 0f && _maxAngle < 180f)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
                DrawAngleCone(camTransform.position, camTransform.forward, _distance, _maxAngle);
            }
        }

        private static void DrawAngleCone(Vector3 origin, Vector3 direction, float distance, float angle)
        {
            int segments = 32;
            float halfAngleRad = angle * Mathf.Deg2Rad;
            Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
            if (perpendicular.sqrMagnitude < 0.001f)
                perpendicular = Vector3.Cross(direction, Vector3.right).normalized;
            Vector3 vertical = Vector3.Cross(direction, perpendicular).normalized;

            Vector3 prevPoint = Vector3.zero;
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments * Mathf.PI * 2f;
                Vector3 offset = (Mathf.Cos(t) * perpendicular + Mathf.Sin(t) * vertical) * Mathf.Sin(halfAngleRad) * distance;
                Vector3 point = origin + direction * distance * Mathf.Cos(halfAngleRad) + offset;

                if (i > 0)
                    Gizmos.DrawLine(prevPoint, point);

                Gizmos.DrawLine(origin, point);
                prevPoint = point;
            }
        }
#endif
    }
}
