using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class GameInputManager : MonoBehaviour {
    public static GameInputManager Instance { get; private set; }

    private PlayerInputActions playerInputActions;

    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }

    // Events
    public event Action OnJump;
    public event Action OnInteract;
    public event Action OnFire;

    private void Awake() {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;

        playerInputActions = new PlayerInputActions();
    }

    private void OnEnable() {
        playerInputActions.Player.Enable();

        playerInputActions.Player.Move.performed += ctx => MoveInput = ctx.ReadValue<Vector2>();
        playerInputActions.Player.Move.canceled += ctx => MoveInput = Vector2.zero;

        playerInputActions.Player.Look.performed += ctx => LookInput = ctx.ReadValue<Vector2>();
        playerInputActions.Player.Look.canceled += ctx => LookInput = Vector2.zero;

        playerInputActions.Player.Jump.performed += ctx => OnJump?.Invoke();
        playerInputActions.Player.Interact.performed += ctx => OnInteract?.Invoke();
        playerInputActions.Player.Fire.performed += ctx => OnFire?.Invoke();
    }

    private void OnDisable() {
        playerInputActions.Player.Disable();
    }
}
