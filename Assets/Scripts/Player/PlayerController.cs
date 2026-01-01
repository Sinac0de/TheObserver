using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour {
    [Header("Components")]
    private CharacterController controller;
    private FPSCameraController cameraController;

    [Header("Movement")]
    public float walkSpeed = 4f;
    public float sprintSpeed = 8f;
    public float acceleration = 15f;
    public float deceleration = 20f;
    public float airControl = 0.3f;
    public float slopeLimit = 45f;

    [Header("Jump")]
    public float jumpForce = 10f;
    public float coyoteTime = 0.2f;
    public float jumpBufferTime = 0.2f;
    public int maxAirJumps = 1;
    public float jumpCutMultiplier = 0.5f;  // For responsive jump feel when releasing jump button

    [Header("Gravity")]
    public float gravityMultiplier = 2f;  // Normal gravity multiplier when falling
    public float fallMultiplier = 3f;     // Extra gravity when falling down fast
    public float lowJumpMultiplier = 2f;  // Gravity when jump button is released early

    private Vector3 moveVelocity;
    private Vector3 smoothVelocity;
    private float verticalVelocity;
    private bool isGrounded;
    private float lastGroundedTime;
    private float lastJumpPressTime;
    private int airJumpsRemaining;
    private bool isSprinting;
    private bool isMoving;

    private Vector2 moveInput;
    private Vector2 lookInput;

    private void Awake() {
        controller = GetComponent<CharacterController>();
        controller.slopeLimit = slopeLimit;
        cameraController = GetComponentInChildren<FPSCameraController>();
    }

    private void Update() {
        HandleInput();
        GroundCheck();
        JumpBuffering();
        CalculateMovement();
        ApplyMovement();
        Look();
        UpdateCameraState();
    }

    private void HandleInput() {
        moveInput = GameInputManager.Instance.GetMoveVector();
        lookInput = GameInputManager.Instance.GetLookVector();
        isSprinting = GameInputManager.Instance.GetSprintInput() && moveInput.magnitude > 0.1f;

        if (GameInputManager.Instance.IsJumpPressed()) {
            lastJumpPressTime = Time.time;
            GameInputManager.Instance.ConsumeJumpPress();
        }
    }

    private void GroundCheck() {
        bool grounded = controller.isGrounded;
        if (grounded && !isGrounded)
            airJumpsRemaining = maxAirJumps;

        isGrounded = grounded;
        if (isGrounded) lastGroundedTime = Time.time;
        if (isGrounded && verticalVelocity < 0) verticalVelocity = -2f;
    }

    private void JumpBuffering() {
        bool canJump = (isGrounded || Time.time - lastGroundedTime <= coyoteTime) && verticalVelocity <= 0;
        bool hasAirJumps = !isGrounded && airJumpsRemaining > 0;
        bool bufferActive = Time.time - lastJumpPressTime <= jumpBufferTime;

        if ((canJump || hasAirJumps) && bufferActive) {
            // Add a small boost to the jump to make it feel snappier
            verticalVelocity = jumpForce;
            // Apply a small horizontal boost on ground jumps to feel more responsive
            if (isGrounded) {
                moveVelocity.y = 0f; // Reset vertical velocity to ensure consistent jump height
            }
            if (!isGrounded) airJumpsRemaining--;
            cameraController?.AddShake(0.1f, 0.1f);
            lastJumpPressTime = 0f;
        }
    }

    private void CalculateMovement() {
        float targetSpeed = isSprinting ? sprintSpeed : walkSpeed;
        Vector3 inputDir = new Vector3(moveInput.x, 0, moveInput.y).normalized;
        Vector3 targetVelocity = transform.TransformDirection(inputDir) * targetSpeed;

        // Apply air control but maintain more momentum
        if (!isGrounded) {
            // Blend current horizontal velocity with target velocity for better air control
            Vector3 currentHorizontalVelocity = new Vector3(moveVelocity.x, 0, moveVelocity.z);
            Vector3 targetHorizontalVelocity = new Vector3(targetVelocity.x, 0, targetVelocity.z);
            
            // Use air control to blend between current and target horizontal velocity
            Vector3 newHorizontalVelocity = Vector3.Lerp(currentHorizontalVelocity, targetHorizontalVelocity, airControl * Time.deltaTime * 10f);
            targetVelocity = newHorizontalVelocity + new Vector3(0, targetVelocity.y, 0);
        }

        moveVelocity = Vector3.SmoothDamp(moveVelocity, targetVelocity, ref smoothVelocity,
            moveInput.magnitude > 0 ? 1 / acceleration : 1 / deceleration);
        isMoving = inputDir.magnitude > 0;
    }

    private void ApplyMovement() {
        float gravity = Physics.gravity.y;
        if (verticalVelocity < 0) {
            // Apply fast fall when falling down
            verticalVelocity += gravity * fallMultiplier * Time.deltaTime;
        }
        else if (verticalVelocity > 0 && !GameInputManager.Instance.IsJumpHeld()) {
            // Apply jump cut when releasing jump button mid-air
            verticalVelocity += gravity * jumpCutMultiplier * Time.deltaTime;
        }
        else {
            // Apply normal gravity
            verticalVelocity += gravity * gravityMultiplier * Time.deltaTime;
        }

        Vector3 finalVelocity = moveVelocity;
        finalVelocity.y = verticalVelocity;

        controller.Move(finalVelocity * Time.deltaTime);

        if (isGrounded && verticalVelocity < 0) verticalVelocity = -2f;
    }

    private void Look() {
        cameraController?.HandleLook(lookInput);
    }

    private void UpdateCameraState() {
        if (cameraController != null) {
            cameraController.SetSprinting(isSprinting);
            cameraController.SetMovementState(isMoving);
        }
    }
}
