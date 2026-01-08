using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class GameInputManager : MonoBehaviour {
    public static GameInputManager Instance { get; private set; }

    private PlayerInputActions playerInputActions;

    // Input States
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool isSprinting;
    private bool isCrouching;
    private bool isJumpPressed;
    private bool isJumpHeld;

    // Input Events
    public event Action OnJump;
    public event Action OnJumpCanceled;
    public event Action OnInteract;
    public event Action OnShoot;
    public event Action OnSprintStart;
    public event Action OnSprintEnd;
    public event Action OnCrouchStart;
    public event Action OnCrouchEnd;
    
    // Menu Navigation Events
    public event Action OnNavigateUp;
    public event Action OnNavigateDown;
    public event Action OnNavigateLeft;
    public event Action OnNavigateRight;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        playerInputActions = new PlayerInputActions();
        InitializeInputActions();
    }

    private void OnEnable() {
        playerInputActions.Player.Enable();
    }

    private void OnDisable() {
        playerInputActions.Player.Disable();
    }

    private void OnDestroy() {
        if (Instance == this)
            Instance = null;
    }

    private void InitializeInputActions() {
        // Move
        playerInputActions.Player.Move.performed += ctx => {
            moveInput = ctx.ReadValue<Vector2>();
            HandleMoveInput(moveInput);
        };
        playerInputActions.Player.Move.canceled += ctx => {
            moveInput = Vector2.zero;
            HandleMoveInput(Vector2.zero);
        };

        // Look
        playerInputActions.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        playerInputActions.Player.Look.canceled += ctx => lookInput = Vector2.zero;

        // Jump
        playerInputActions.Player.Jump.started += ctx => {
            isJumpPressed = true;
            isJumpHeld = true;
            OnJump?.Invoke();
        };
        playerInputActions.Player.Jump.canceled += ctx => {
            isJumpHeld = false;
            OnJumpCanceled?.Invoke();
        };

        // Sprint
        playerInputActions.Player.Sprint.started += ctx => {
            isSprinting = true;
            OnSprintStart?.Invoke();
        };
        playerInputActions.Player.Sprint.canceled += ctx => {
            isSprinting = false;
            OnSprintEnd?.Invoke();
        };

        // Interact / Shoot
        playerInputActions.Player.Interact.performed += ctx => OnInteract?.Invoke();
        playerInputActions.Player.Shoot.performed += ctx => OnShoot?.Invoke();

        // Crouch
        playerInputActions.Player.Crouch.started += ctx => {
            isCrouching = true;
            OnCrouchStart?.Invoke();
        };
        playerInputActions.Player.Crouch.canceled += ctx => {
            isCrouching = false;
            OnCrouchEnd?.Invoke();
        };
    }

    // Public Accessors for PlayerController / CameraController
    public Vector2 GetMoveVector() => moveInput;
    public Vector2 GetLookVector() => lookInput;
    public bool GetSprintInput() => isSprinting;
    public bool GetCrouchInput() => isCrouching;

    // Jump Accessors
    public bool IsJumpPressed() => isJumpPressed;
    public bool IsJumpHeld() => isJumpHeld;

    // Reset Jump Press (after consuming for jump buffer)
    public void ConsumeJumpPress() => isJumpPressed = false;
    
    private void HandleMoveInput(Vector2 input)
    {
        // Only trigger navigation events when in menu states
        if (MenuManager.Instance != null && 
            (MenuManager.Instance.CurrentState == MenuManager.MenuState.MainMenu || 
             MenuManager.Instance.CurrentState == MenuManager.MenuState.Settings ||
             MenuManager.Instance.CurrentState == MenuManager.MenuState.Paused))
        {
            // Detect horizontal movement for left/right navigation
            if (input.x > 0.5f && input.x > Mathf.Abs(input.y))
            {
                OnNavigateRight?.Invoke();
            }
            else if (input.x < -0.5f && Mathf.Abs(input.x) > Mathf.Abs(input.y))
            {
                OnNavigateLeft?.Invoke();
            }
            // Detect vertical movement for up/down navigation
            else if (input.y > 0.5f && input.y > Mathf.Abs(input.x))
            {
                OnNavigateUp?.Invoke();
            }
            else if (input.y < -0.5f && Mathf.Abs(input.y) > Mathf.Abs(input.x))
            {
                OnNavigateDown?.Invoke();
            }
        }
    }
}
