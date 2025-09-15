using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RougeLite.Managers
{
    /// <summary>
    /// Input Manager centralizes input handling across different input systems
    /// Supports both old Input Manager and new Input System
    /// Provides input buffering and customizable key bindings
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        #region Singleton
        
        private static InputManager _instance;
        public static InputManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<InputManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("InputManager");
                        _instance = go.AddComponent<InputManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Input Settings

        [Header("Input Settings")]
        [SerializeField] private bool useNewInputSystem = true;
        [SerializeField] private float inputBufferTime = 0.2f;
        [SerializeField] private float mouseSensitivity = 1f;
        [SerializeField] private bool invertMouseY = false;

        [Header("Key Bindings")]
        [SerializeField] private KeyCode moveUpKey = KeyCode.W;
        [SerializeField] private KeyCode moveDownKey = KeyCode.S;
        [SerializeField] private KeyCode moveLeftKey = KeyCode.A;
        [SerializeField] private KeyCode moveRightKey = KeyCode.D;
        [SerializeField] private KeyCode jumpKey = KeyCode.Space;
        [SerializeField] private KeyCode attackKey = KeyCode.Mouse0;
        [SerializeField] private KeyCode defendKey = KeyCode.Mouse1;
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
        [SerializeField] private KeyCode inventoryKey = KeyCode.Tab;

        #endregion

        #region Input State

        private Vector2 movementInput;
        private Vector2 mouseInput;
        private Vector2 mouseDelta;
        private Dictionary<string, InputBuffer> inputBuffers;
        private Dictionary<string, bool> inputStates;
        private Dictionary<string, float> inputValues;

        // New Input System (if available)
        private PlayerInput playerInput;
        private InputActionMap currentActionMap;

        #endregion

        #region Input Actions

        public System.Action OnPausePressed;
        public System.Action OnAttackPressed;
        public System.Action OnDefendPressed;
        public System.Action OnInteractPressed;
        public System.Action OnInventoryPressed;
        public System.Action OnJumpPressed;

        public System.Action<Vector2> OnMovementInput;
        public System.Action<Vector2> OnMouseInput;

        #endregion

        #region Properties

        public float InputBufferTime 
        { 
            get => inputBufferTime; 
            set => inputBufferTime = Mathf.Max(0f, value); 
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Singleton pattern
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeInputManager();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            LoadInputSettings();
        }

        private void Update()
        {
            if (useNewInputSystem)
            {
                HandleNewInputSystem();
            }
            else
            {
                HandleLegacyInputSystem();
            }

            UpdateInputBuffers();
            ProcessInputEvents();
        }

        private void OnDestroy()
        {
            SaveInputSettings();
        }

        #endregion

        #region Initialization

        private void InitializeInputManager()
        {
            Debug.Log("InputManager: Initializing...");

            // Initialize input buffers and states
            inputBuffers = new Dictionary<string, InputBuffer>();
            inputStates = new Dictionary<string, bool>();
            inputValues = new Dictionary<string, float>();

            // Initialize input actions
            InitializeInputActions();

            // Try to set up new input system
            if (useNewInputSystem)
            {
                SetupNewInputSystem();
            }

            Debug.Log("InputManager: Initialization complete");
        }

        private void InitializeInputActions()
        {
            string[] inputActions = {
                "Move", "Attack", "Defend", "Jump", "Interact", 
                "Pause", "Inventory", "Mouse", "Scroll"
            };

            foreach (string action in inputActions)
            {
                inputBuffers[action] = new InputBuffer(inputBufferTime);
                inputStates[action] = false;
                inputValues[action] = 0f;
            }
        }

        private void SetupNewInputSystem()
        {
            try
            {
                // Try to find existing PlayerInput component
                playerInput = GetComponent<PlayerInput>();
                
                if (playerInput == null)
                {
                    // Create PlayerInput component if it doesn't exist
                    playerInput = gameObject.AddComponent<PlayerInput>();
                }

                if (playerInput != null)
                {
                    currentActionMap = playerInput.currentActionMap;
                    Debug.Log("InputManager: New Input System initialized");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"InputManager: New Input System not available, falling back to legacy input: {e.Message}");
                useNewInputSystem = false;
            }
        }

        #endregion

        #region Legacy Input System

        private void HandleLegacyInputSystem()
        {
            // Movement input
            float horizontal = 0f;
            float vertical = 0f;

            if (Input.GetKey(moveLeftKey)) horizontal -= 1f;
            if (Input.GetKey(moveRightKey)) horizontal += 1f;
            if (Input.GetKey(moveUpKey)) vertical += 1f;
            if (Input.GetKey(moveDownKey)) vertical -= 1f;

            movementInput = new Vector2(horizontal, vertical).normalized;

            // Mouse input
            mouseInput = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            mouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * mouseSensitivity;
            if (invertMouseY) mouseDelta.y = -mouseDelta.y;

            // Button inputs
            UpdateButtonInput("Attack", Input.GetKey(attackKey), Input.GetKeyDown(attackKey), Input.GetKeyUp(attackKey));
            UpdateButtonInput("Defend", Input.GetKey(defendKey), Input.GetKeyDown(defendKey), Input.GetKeyUp(defendKey));
            UpdateButtonInput("Jump", Input.GetKey(jumpKey), Input.GetKeyDown(jumpKey), Input.GetKeyUp(jumpKey));
            UpdateButtonInput("Interact", Input.GetKey(interactKey), Input.GetKeyDown(interactKey), Input.GetKeyUp(interactKey));
            UpdateButtonInput("Pause", Input.GetKey(pauseKey), Input.GetKeyDown(pauseKey), Input.GetKeyUp(pauseKey));
            UpdateButtonInput("Inventory", Input.GetKey(inventoryKey), Input.GetKeyDown(inventoryKey), Input.GetKeyUp(inventoryKey));

            // Scroll input
            inputValues["Scroll"] = Input.GetAxis("Mouse ScrollWheel");
        }

        #endregion

        #region New Input System

        private void HandleNewInputSystem()
        {
            if (playerInput == null || currentActionMap == null) return;

            try
            {
                // Movement input
                var moveAction = currentActionMap.FindAction("Move");
                if (moveAction != null)
                {
                    movementInput = moveAction.ReadValue<Vector2>();
                }

                // Mouse input
                var mouseAction = currentActionMap.FindAction("Mouse");
                if (mouseAction != null)
                {
                    mouseDelta = mouseAction.ReadValue<Vector2>() * mouseSensitivity;
                    if (invertMouseY) mouseDelta.y = -mouseDelta.y;
                }

                // Button inputs
                UpdateNewInputAction("Attack");
                UpdateNewInputAction("Defend");
                UpdateNewInputAction("Jump");
                UpdateNewInputAction("Interact");
                UpdateNewInputAction("Pause");
                UpdateNewInputAction("Inventory");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"InputManager: Error reading new input system: {e.Message}");
            }
        }

        private void UpdateNewInputAction(string actionName)
        {
            var action = currentActionMap?.FindAction(actionName);
            if (action != null)
            {
                bool isPressed = action.IsPressed();
                bool wasTriggered = action.WasPressedThisFrame();
                bool wasReleased = action.WasReleasedThisFrame();
                
                UpdateButtonInput(actionName, isPressed, wasTriggered, wasReleased);
            }
        }

        #endregion

        #region Input Processing

        private void UpdateButtonInput(string inputName, bool isHeld, bool wasPressed, bool wasReleased)
        {
            if (inputBuffers.ContainsKey(inputName))
            {
                inputStates[inputName] = isHeld;
                
                if (wasPressed)
                {
                    inputBuffers[inputName].SetPressed();
                }
                
                if (wasReleased)
                {
                    inputBuffers[inputName].SetReleased();
                }
            }
        }

        private void UpdateInputBuffers()
        {
            foreach (var buffer in inputBuffers.Values)
            {
                buffer.Update();
            }
        }

        private void ProcessInputEvents()
        {
            // Movement
            OnMovementInput?.Invoke(movementInput);
            OnMouseInput?.Invoke(mouseDelta);

            // Buttons
            if (GetButtonDown("Attack")) OnAttackPressed?.Invoke();
            if (GetButtonDown("Defend")) OnDefendPressed?.Invoke();
            if (GetButtonDown("Jump")) OnJumpPressed?.Invoke();
            if (GetButtonDown("Interact")) OnInteractPressed?.Invoke();
            if (GetButtonDown("Pause")) OnPausePressed?.Invoke();
            if (GetButtonDown("Inventory")) OnInventoryPressed?.Invoke();
        }

        #endregion

        #region Public Input API

        public Vector2 GetMovementInput()
        {
            return movementInput;
        }

        public Vector2 GetMouseInput()
        {
            return mouseInput;
        }

        public Vector2 GetMouseDelta()
        {
            return mouseDelta;
        }

        public bool GetButton(string inputName)
        {
            return inputStates.ContainsKey(inputName) && inputStates[inputName];
        }

        public bool GetButtonDown(string inputName)
        {
            return inputBuffers.ContainsKey(inputName) && inputBuffers[inputName].WasPressed();
        }

        public bool GetButtonUp(string inputName)
        {
            return inputBuffers.ContainsKey(inputName) && inputBuffers[inputName].WasReleased();
        }

        public float GetAxisValue(string inputName)
        {
            return inputValues.ContainsKey(inputName) ? inputValues[inputName] : 0f;
        }

        public bool IsInputBuffered(string inputName)
        {
            return inputBuffers.ContainsKey(inputName) && inputBuffers[inputName].IsBuffered();
        }

        public void ConsumeBufferedInput(string inputName)
        {
            if (inputBuffers.ContainsKey(inputName))
            {
                inputBuffers[inputName].Consume();
            }
        }

        #endregion

        #region Settings

        public void SetMouseSensitivity(float sensitivity)
        {
            mouseSensitivity = Mathf.Clamp(sensitivity, 0.1f, 5f);
        }

        public void SetInvertMouseY(bool invert)
        {
            invertMouseY = invert;
        }

        public void SetKeyBinding(string inputName, KeyCode keyCode)
        {
            switch (inputName.ToLower())
            {
                case "moveup": moveUpKey = keyCode; break;
                case "movedown": moveDownKey = keyCode; break;
                case "moveleft": moveLeftKey = keyCode; break;
                case "moveright": moveRightKey = keyCode; break;
                case "jump": jumpKey = keyCode; break;
                case "attack": attackKey = keyCode; break;
                case "defend": defendKey = keyCode; break;
                case "interact": interactKey = keyCode; break;
                case "pause": pauseKey = keyCode; break;
                case "inventory": inventoryKey = keyCode; break;
            }
        }

        public KeyCode GetKeyBinding(string inputName)
        {
            return inputName.ToLower() switch
            {
                "moveup" => moveUpKey,
                "movedown" => moveDownKey,
                "moveleft" => moveLeftKey,
                "moveright" => moveRightKey,
                "jump" => jumpKey,
                "attack" => attackKey,
                "defend" => defendKey,
                "interact" => interactKey,
                "pause" => pauseKey,
                "inventory" => inventoryKey,
                _ => KeyCode.None
            };
        }

        #endregion

        #region Settings Persistence

        private void LoadInputSettings()
        {
            mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 1f);
            invertMouseY = PlayerPrefs.GetInt("InvertMouseY", 0) == 1;
            
            // Load key bindings
            moveUpKey = (KeyCode)PlayerPrefs.GetInt("MoveUpKey", (int)KeyCode.W);
            moveDownKey = (KeyCode)PlayerPrefs.GetInt("MoveDownKey", (int)KeyCode.S);
            moveLeftKey = (KeyCode)PlayerPrefs.GetInt("MoveLeftKey", (int)KeyCode.A);
            moveRightKey = (KeyCode)PlayerPrefs.GetInt("MoveRightKey", (int)KeyCode.D);
            jumpKey = (KeyCode)PlayerPrefs.GetInt("JumpKey", (int)KeyCode.Space);
            attackKey = (KeyCode)PlayerPrefs.GetInt("AttackKey", (int)KeyCode.Mouse0);
            defendKey = (KeyCode)PlayerPrefs.GetInt("DefendKey", (int)KeyCode.Mouse1);
            interactKey = (KeyCode)PlayerPrefs.GetInt("InteractKey", (int)KeyCode.E);
            pauseKey = (KeyCode)PlayerPrefs.GetInt("PauseKey", (int)KeyCode.Escape);
            inventoryKey = (KeyCode)PlayerPrefs.GetInt("InventoryKey", (int)KeyCode.Tab);
        }

        public void SaveInputSettings()
        {
            PlayerPrefs.SetFloat("MouseSensitivity", mouseSensitivity);
            PlayerPrefs.SetInt("InvertMouseY", invertMouseY ? 1 : 0);
            
            // Save key bindings
            PlayerPrefs.SetInt("MoveUpKey", (int)moveUpKey);
            PlayerPrefs.SetInt("MoveDownKey", (int)moveDownKey);
            PlayerPrefs.SetInt("MoveLeftKey", (int)moveLeftKey);
            PlayerPrefs.SetInt("MoveRightKey", (int)moveRightKey);
            PlayerPrefs.SetInt("JumpKey", (int)jumpKey);
            PlayerPrefs.SetInt("AttackKey", (int)attackKey);
            PlayerPrefs.SetInt("DefendKey", (int)defendKey);
            PlayerPrefs.SetInt("InteractKey", (int)interactKey);
            PlayerPrefs.SetInt("PauseKey", (int)pauseKey);
            PlayerPrefs.SetInt("InventoryKey", (int)inventoryKey);
            
            PlayerPrefs.Save();
        }

        #endregion

        #region Debug

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;

        private void OnGUI()
        {
            if (!showDebugInfo) return;

            GUI.Box(new Rect(10, 620, 300, 160), "Input Manager Debug");
            GUI.Label(new Rect(20, 640, 280, 20), $"Input System: {(useNewInputSystem ? "New" : "Legacy")}");
            GUI.Label(new Rect(20, 660, 280, 20), $"Movement: {movementInput}");
            GUI.Label(new Rect(20, 680, 280, 20), $"Mouse Delta: {mouseDelta}");
            GUI.Label(new Rect(20, 700, 280, 20), $"Attack: {(GetButton("Attack") ? "HELD" : "Released")}");
            GUI.Label(new Rect(20, 720, 280, 20), $"Mouse Sensitivity: {mouseSensitivity:F1}");

            if (GUI.Button(new Rect(20, 740, 100, 20), "Reset Bindings"))
            {
                // Reset to defaults
                moveUpKey = KeyCode.W;
                moveDownKey = KeyCode.S;
                moveLeftKey = KeyCode.A;
                moveRightKey = KeyCode.D;
                SaveInputSettings();
            }

            if (GUI.Button(new Rect(130, 740, 100, 20), "Toggle System"))
            {
                useNewInputSystem = !useNewInputSystem;
                if (useNewInputSystem)
                {
                    SetupNewInputSystem();
                }
            }
        }

        #endregion
    }

    #region Input Buffer Class

    public class InputBuffer
    {
        private bool wasPressed;
        private bool wasReleased;
        private float pressTime;
        private float releaseTime;
        private readonly float bufferDuration;

        public InputBuffer(float bufferDuration = 0.2f)
        {
            this.bufferDuration = bufferDuration;
        }

        public void SetPressed()
        {
            wasPressed = true;
            pressTime = Time.time;
        }

        public void SetReleased()
        {
            wasReleased = true;
            releaseTime = Time.time;
        }

        public bool WasPressed()
        {
            return wasPressed && (Time.time - pressTime) <= bufferDuration;
        }

        public bool WasReleased()
        {
            return wasReleased && (Time.time - releaseTime) <= bufferDuration;
        }

        public bool IsBuffered()
        {
            return WasPressed();
        }

        public void Consume()
        {
            wasPressed = false;
            wasReleased = false;
        }

        public void Update()
        {
            // Clear old inputs
            if (wasPressed && (Time.time - pressTime) > bufferDuration)
            {
                wasPressed = false;
            }
            
            if (wasReleased && (Time.time - releaseTime) > bufferDuration)
            {
                wasReleased = false;
            }
        }
    }

    #endregion
}