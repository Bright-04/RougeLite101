using UnityEngine;
using UnityEngine.InputSystem;
using RougeLite.Events;

namespace RougeLite.Player
{
    /// <summary>
    /// Simple player movement controller for testing infinite world generation
    /// Use WASD or Arrow Keys to move around and explore the infinite world
    /// Now handles all player movement, animation, and movement event broadcasting
    /// </summary>
    public class SimplePlayerMovement : EventBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 10f;
        [SerializeField] private float fastMoveSpeed = 50f; // For quick exploration
        [SerializeField] private bool useRigidbody = true;

        [Header("Input Settings")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference fastMoveAction;
        [SerializeField] private KeyCode fastMoveKey = KeyCode.LeftShift; // Fallback
        [SerializeField] private bool useArrowKeys = true;
        [SerializeField] private bool useWASD = true;

        [Header("Debug Info")]
#if RL_DEBUG_UI
        [SerializeField] private bool showPosition = true;
        [SerializeField] private bool showChunkInfo = true;
#endif

        private Rigidbody2D rb;
        private Animator animator;
        private Vector3 lastPosition;
        private float distanceTraveled = 0f;

        #region Unity Lifecycle

        protected override void Awake()
        {
            // Call base class Awake to initialize event system
            base.Awake();
            
            // Enable input actions
            EnableInputActions();
        }

        private void OnEnable()
        {
            EnableInputActions();
        }

        private void OnDisable()
        {
            DisableInputActions();
        }

        private void Start()
        {
            // Get or add Rigidbody2D if using physics movement
            if (useRigidbody)
            {
                rb = GetComponent<Rigidbody2D>();
                if (rb == null)
                {
                    rb = gameObject.AddComponent<Rigidbody2D>();
                    rb.gravityScale = 0f; // No gravity for top-down movement
                    rb.linearDamping = 2f; // Some drag to make movement feel better
                }
            }

            // Get Animator component for animation updates
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogWarning($"SimplePlayerMovement: No Animator component found on {gameObject.name}. Character animations will not work.");
            }

            lastPosition = transform.position;

            // Add player tag if not already set
            if (!gameObject.CompareTag("Player"))
            {
                gameObject.tag = "Player";
            }

            Debug.Log("🎮 Simple Player Movement Controls:");
            Debug.Log("• WASD or Arrow Keys - Move");
            Debug.Log("• Hold Left Shift - Fast Movement");
            Debug.Log("• Perfect for testing infinite world generation!");
        }

        private void Update()
        {
            HandleMovementInput();
            UpdateDebugInfo();
        }

        #endregion

        #region Input System Management

        private void EnableInputActions()
        {
            if (moveAction?.action != null)
            {
                moveAction.action.Enable();
            }
            
            if (fastMoveAction?.action != null)
            {
                fastMoveAction.action.Enable();
            }
        }

        private void DisableInputActions()
        {
            if (moveAction?.action != null)
            {
                moveAction.action.Disable();
            }
            
            if (fastMoveAction?.action != null)
            {
                fastMoveAction.action.Disable();
            }
        }

        #endregion

        #region Movement

        private void HandleMovementInput()
        {
            Vector2 moveInput = GetMovementInput();
            
            // Update animator parameters if animator exists
            if (animator != null)
            {
                animator.SetFloat("moveX", moveInput.x);
                animator.SetFloat("moveY", moveInput.y);
            }
            
            if (moveInput.magnitude > 0)
            {
                bool isFastMove = IsFastMovePressed();
                float currentSpeed = isFastMove ? fastMoveSpeed : moveSpeed;
                
                Vector2 previousPosition = transform.position;
                
                if (useRigidbody && rb != null)
                {
                    // Physics-based movement
                    rb.linearVelocity = moveInput * currentSpeed;
                }
                else
                {
                    // Transform-based movement
                    Vector3 movement = moveInput * currentSpeed * Time.deltaTime;
                    transform.Translate(movement);
                }
                
                // Broadcast movement event for game systems that need to know about player movement
                BroadcastMovementEvent(moveInput, currentSpeed, previousPosition);
            }
            else if (useRigidbody && rb != null)
            {
                // Stop smoothly when no input
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.deltaTime * 5f);
            }
        }

        private bool IsFastMovePressed()
        {
            // Try to use New Input System first
            if (fastMoveAction != null && fastMoveAction.action != null)
            {
                return fastMoveAction.action.IsPressed();
            }
            
            // Fallback to new Input System keyboard check
            if (Keyboard.current != null)
            {
                return Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
            }
            
            // Final fallback to legacy Input system using configured key
            return Input.GetKey(fastMoveKey);
        }

        private Vector2 GetMovementInput()
        {
            Vector2 input = Vector2.zero;

            // Try to use New Input System first
            if (moveAction != null && moveAction.action != null)
            {
                input = moveAction.action.ReadValue<Vector2>();
            }
            else
            {
                // Fallback to old input system for backward compatibility
                // WASD Input
                if (useWASD)
                {
                    if (Keyboard.current != null)
                    {
                        if (Keyboard.current.wKey.isPressed) input.y += 1f;
                        if (Keyboard.current.sKey.isPressed) input.y -= 1f;
                        if (Keyboard.current.aKey.isPressed) input.x -= 1f;
                        if (Keyboard.current.dKey.isPressed) input.x += 1f;
                    }
                }

                // Arrow Keys Input using new Input System
                if (useArrowKeys && Keyboard.current != null)
                {
                    if (Keyboard.current.upArrowKey.isPressed) input.y += 1f;
                    if (Keyboard.current.downArrowKey.isPressed) input.y -= 1f;
                    if (Keyboard.current.leftArrowKey.isPressed) input.x -= 1f;
                    if (Keyboard.current.rightArrowKey.isPressed) input.x += 1f;
                }
            }

            // Normalize to prevent faster diagonal movement
            return input.normalized;
        }

        #endregion

        #region Debug Info

        private void UpdateDebugInfo()
        {
            // Track distance traveled
            float frameDistance = Vector3.Distance(transform.position, lastPosition);
            distanceTraveled += frameDistance;
            lastPosition = transform.position;
        }
#if RL_DEBUG_UI

        private void OnGUI()
        {
            if (!showPosition && !showChunkInfo) return;

            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.fontSize = 14;
            style.normal.textColor = Color.white;

            string debugText = "🗺️ Infinite World Explorer\n";
            
            if (showPosition)
            {
                debugText += $"Position: ({transform.position.x:F1}, {transform.position.y:F1})\n";
                debugText += $"Distance Traveled: {distanceTraveled:F1}m\n";
            }

            if (showChunkInfo)
            {
                Vector2Int currentChunk = GetCurrentChunk();
                debugText += $"Current Chunk: ({currentChunk.x}, {currentChunk.y})\n";
            }

            debugText += "\nControls:\n";
            debugText += "WASD/Arrows - Move\n";
            debugText += "Shift - Fast Move";

            GUI.Box(new Rect(10, 10, 250, 120), debugText, style);
        }

        private Vector2Int GetCurrentChunk()
        {
            // Assuming chunk size of 50 (should match your InfiniteWorldGenerator)
            int chunkSize = 50;
            int chunkX = Mathf.FloorToInt(transform.position.x / chunkSize);
            int chunkY = Mathf.FloorToInt(transform.position.y / chunkSize);
            return new Vector2Int(chunkX, chunkY);
        }

        #endif
        #endregion

        #region Public Methods

        /// <summary>
        /// Teleport player to a specific world position
        /// </summary>
        public void TeleportTo(Vector3 position)
        {
            transform.position = position;
            if (useRigidbody && rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
            Debug.Log($"🚀 Teleported to: {position}");
        }

        /// <summary>
        /// Teleport to a specific chunk
        /// </summary>
        public void TeleportToChunk(Vector2Int chunkCoords)
        {
            int chunkSize = 50;
            Vector3 chunkCenter = new Vector3(
                chunkCoords.x * chunkSize + chunkSize * 0.5f,
                chunkCoords.y * chunkSize + chunkSize * 0.5f,
                transform.position.z
            );
            TeleportTo(chunkCenter);
        }

        /// <summary>
        /// Get current movement speed
        /// </summary>
        public float GetCurrentSpeed()
        {
            return IsFastMovePressed() ? fastMoveSpeed : moveSpeed;
        }

        /// <summary>
        /// Set movement speeds
        /// </summary>
        public void SetSpeeds(float normal, float fast)
        {
            moveSpeed = normal;
            fastMoveSpeed = fast;
        }

        /// <summary>
        /// Broadcast movement event to game systems
        /// </summary>
        private void BroadcastMovementEvent(Vector2 moveInput, float currentSpeed, Vector2 previousPosition)
        {
            var movementData = new PlayerMovementData(
                player: gameObject,
                velocity: moveInput * currentSpeed,
                position: transform.position,
                previousPosition: previousPosition
            );
            
            var movementEvent = new PlayerMovementEvent(movementData, gameObject);
            BroadcastEvent(movementEvent);
        }

        #endregion
    }
}
