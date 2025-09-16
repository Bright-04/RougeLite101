using UnityEngine;

namespace RougeLite.Camera
{
    /// <summary>
    /// Camera system for following the player with bounds checking and smooth movement
    /// Perfect for expanded maps while keeping camera within world boundaries
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Follow Settings")]
        [SerializeField] private Transform target; // Player transform
        [SerializeField] private float followSpeed = 5f; // Increased default speed
        [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);
        [SerializeField] private bool smoothFollow = true;

        [Header("Camera Bounds")]
        [SerializeField] private bool useBounds = false; // Disabled for infinite world
        [SerializeField] private float minX = -50f;
        [SerializeField] private float maxX = 50f;
        [SerializeField] private float minY = -30f;
        [SerializeField] private float maxY = 30f;

        [Header("Camera Shake")]
        [SerializeField] private float shakeDuration = 0f;
        [SerializeField] private float shakeIntensity = 0f;
        
        [Header("Zoom Settings")]
        [SerializeField] private UnityEngine.Camera cam;
        [SerializeField] private float defaultSize = 8f; // Increased for wider FOV
        [SerializeField] private float minZoom = 5f; // Increased minimum
        [SerializeField] private float maxZoom = 15f; // Increased maximum
        [SerializeField] private float zoomSpeed = 2f;

        private Vector3 velocity = Vector3.zero;
        private Vector3 originalPosition;
        private float targetZoom;

        #region Unity Lifecycle

        private void Start()
        {
            // Get camera component if not assigned
            if (cam == null)
                cam = GetComponent<UnityEngine.Camera>();

            // Find player if not assigned
            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    target = player.transform;
            }

            // Set default zoom
            targetZoom = defaultSize;
            if (cam != null)
                cam.orthographicSize = defaultSize;

            originalPosition = transform.position;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            HandleCameraFollow();
            HandleCameraShake();
            // HandleZoom(); // Temporarily disabled to allow manual orthographic size
        }

        #endregion

        #region Camera Following

        private void HandleCameraFollow()
        {
            Vector3 targetPosition = target.position + offset;

            if (smoothFollow)
            {
                // Calculate distance to target for adaptive speed
                float distance = Vector3.Distance(transform.position, targetPosition);
                
                // Use adaptive follow speed - faster when further away
                float adaptiveSpeed = followSpeed;
                if (distance > 10f) // If player is moving very fast and camera is far behind
                {
                    adaptiveSpeed = followSpeed * 2f; // Double speed for catch-up
                }
                else if (distance > 5f)
                {
                    adaptiveSpeed = followSpeed * 1.5f; // 50% faster for medium distances
                }

                // Smooth follow using Vector3.SmoothDamp with adaptive speed
                transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, 1f / adaptiveSpeed);
            }
            else
            {
                // Instant follow
                transform.position = targetPosition;
            }

            // Apply bounds if enabled
            if (useBounds)
            {
                ApplyBounds();
            }
        }

        private void ApplyBounds()
        {
            Vector3 pos = transform.position;
            
            // Calculate camera viewport boundaries
            float camHeight = cam.orthographicSize;
            float camWidth = camHeight * cam.aspect;
            
            // Clamp position to bounds
            pos.x = Mathf.Clamp(pos.x, minX + camWidth, maxX - camWidth);
            pos.y = Mathf.Clamp(pos.y, minY + camHeight, maxY - camHeight);
            
            transform.position = pos;
        }

        #endregion

        #region Camera Shake

        private void HandleCameraShake()
        {
            if (shakeDuration > 0)
            {
                // Generate random shake offset
                Vector3 shakeOffset = Random.insideUnitSphere * shakeIntensity;
                shakeOffset.z = 0; // Keep Z position unchanged
                
                transform.position += shakeOffset;
                shakeDuration -= Time.deltaTime;
            }
        }

        public void ShakeCamera(float duration, float intensity)
        {
            shakeDuration = duration;
            shakeIntensity = intensity;
        }

        #endregion

        #region Zoom Control

        private void HandleZoom()
        {
            if (cam != null)
            {
                // Smooth zoom transition
                cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, Time.deltaTime * zoomSpeed);
            }
        }

        public void SetZoom(float newZoom)
        {
            targetZoom = Mathf.Clamp(newZoom, minZoom, maxZoom);
        }

        public void ZoomIn(float amount = 1f)
        {
            SetZoom(targetZoom - amount);
        }

        public void ZoomOut(float amount = 1f)
        {
            SetZoom(targetZoom + amount);
        }

        public void ResetZoom()
        {
            SetZoom(defaultSize);
        }

        #endregion

        #region Public Methods

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public void SetBounds(float minX, float maxX, float minY, float maxY)
        {
            this.minX = minX;
            this.maxX = maxX;
            this.minY = minY;
            this.maxY = maxY;
        }

        public void SetFollowSpeed(float speed)
        {
            followSpeed = Mathf.Max(0.1f, speed);
        }

        public void EnableBounds(bool enable)
        {
            useBounds = enable;
        }

        #endregion

        #region Debug

        private void OnDrawGizmosSelected()
        {
            if (useBounds)
            {
                // Draw camera bounds in scene view
                Gizmos.color = Color.yellow;
                Vector3 center = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0);
                Vector3 size = new Vector3(maxX - minX, maxY - minY, 0);
                Gizmos.DrawWireCube(center, size);
            }

            // Draw camera viewport
            if (cam != null)
            {
                Gizmos.color = Color.cyan;
                float camHeight = cam.orthographicSize;
                float camWidth = camHeight * cam.aspect;
                Vector3 camSize = new Vector3(camWidth * 2, camHeight * 2, 0);
                Gizmos.DrawWireCube(transform.position, camSize);
            }
        }

        #endregion
    }
}