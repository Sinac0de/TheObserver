using UnityEngine;

//[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour {
    [Header("Components")]
    private CharacterController controller;
    private FPSCameraController cameraController;

    [Header("Movement")]
    public float walkSpeed = 4f;
    public float sprintSpeed = 8f;
    public float crouchSpeed = 2f;
    public float acceleration = 15f;
    public float deceleration = 20f;
    public float airControl = 0.3f;
    public float slopeLimit = 45f;

    [Header("Jump")]
    public float jumpForce = 10f;
    public float crouchJumpForce = 8f;
    public float coyoteTime = 0.2f;
    public float jumpBufferTime = 0.2f;
    public int maxAirJumps = 1;
    public float jumpCutMultiplier = 0.5f;  // For responsive jump feel when releasing jump button

    [Header("Crouch")]
    public float standingHeight = 2f;
    public float crouchingHeight = 1f;
    public float crouchTransitionSpeed = 10f;
    public float headbobReduction = 0.5f;

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

    [Header("Crouch State")]
    private bool isCrouching;
    private bool isCrouchPressed;
    private bool isAttemptingToStand;
    private float currentHeight;
    private float targetHeight;
    private float lastCrouchPressTime;

    private void Awake() {
        controller = GetComponent<CharacterController>();
        controller.slopeLimit = slopeLimit;
        cameraController = GetComponentInChildren<FPSCameraController>();
    }

    private void Update() {
        HandleInput();
        GroundCheck();
        JumpBuffering();
        HandleCrouch();
        CalculateMovement();
        ApplyMovement();
        Look();
        UpdateCameraState();
    }

    private void HandleInput() {
        moveInput = GameInputManager.Instance.GetMoveVector();
        lookInput = GameInputManager.Instance.GetLookVector();
        isSprinting = GameInputManager.Instance.GetSprintInput() && moveInput.magnitude > 0.1f;

        // Only handle jump input if not in a room that restricts it
        if (GameInputManager.Instance.IsJumpPressed() && CanJump()) {
            lastJumpPressTime = Time.time;
            GameInputManager.Instance.ConsumeJumpPress();
        }

        // Handle crouch input
        bool crouchInput = GameInputManager.Instance.GetCrouchInput();
        
        // Toggle crouch on press (not hold)
        if (crouchInput && !isCrouchPressed && CanCrouch()) {
            isCrouchPressed = true;
            lastCrouchPressTime = Time.time;
            ToggleCrouch();
        }
        else if (!crouchInput) {
            isCrouchPressed = false;
        }
    }
    
    private bool CanJump()
    {
        // Check if current room restricts jumping
        if (currentRoom is HorrorRoomController)
        {
            return false; // Horror rooms disable jumping
        }
        return true;
    }
    
    private bool CanCrouch()
    {
        // Check if current room restricts crouching
        if (currentRoom is HorrorRoomController)
        {
            return false; // Horror rooms disable crouching
        }
        return true;
    }
    
    private IRoom currentRoom; // Reference to current room for restrictions

    private void GroundCheck() {
        bool grounded = controller.isGrounded;
        if (grounded && !isGrounded)
            airJumpsRemaining = maxAirJumps;

        isGrounded = grounded;
        if (isGrounded) lastGroundedTime = Time.time;
        if (isGrounded && verticalVelocity < 0) verticalVelocity = -2f;

        // Reset crouch attempt when grounded
        if (isGrounded) isAttemptingToStand = false;
    }

    private void JumpBuffering() {
        bool canJump = (isGrounded || Time.time - lastGroundedTime <= coyoteTime) && verticalVelocity <= 0;
        bool hasAirJumps = !isGrounded && airJumpsRemaining > 0;
        bool bufferActive = Time.time - lastJumpPressTime <= jumpBufferTime;

        if ((canJump || hasAirJumps) && bufferActive) {
            // Use appropriate jump force based on crouch state
            float actualJumpForce = isCrouching ? crouchJumpForce : jumpForce;
            verticalVelocity = actualJumpForce;
            
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
        // Determine target speed based on movement states
        float targetSpeed = walkSpeed;
        
        if (isCrouching) {
            targetSpeed = crouchSpeed;
        }
        else if (isSprinting) {
            targetSpeed = sprintSpeed;
        }
        
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
            cameraController.SetCrouching(isCrouching);
            cameraController.SetHeadbobMultiplier(isCrouching ? headbobReduction : 1f);
        }
    }

    /// <summary>
    /// Handles the crouching logic including height transitions and collision detection
    /// </summary>
    private void HandleCrouch() {
        // Set target height based on crouch state
        targetHeight = isCrouching ? crouchingHeight : standingHeight;
        
        // Smoothly interpolate to target height
        currentHeight = Mathf.Lerp(controller.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);
        
        // Check if we can stand up (only when attempting to stand)
        if (isAttemptingToStand && !isCrouching) {
            if (CanStandUp()) {
                isAttemptingToStand = false;
            } else {
                // Keep crouching if not enough space
                currentHeight = crouchingHeight;
                targetHeight = crouchingHeight;
            }
        }
        
        // Apply height change to controller
        if (Mathf.Abs(controller.height - currentHeight) > 0.01f) {
            AdjustControllerHeight(currentHeight);
        }
    }

    /// <summary>
    /// Toggles the crouch state
    /// </summary>
    private void ToggleCrouch() {
        if (isCrouching) {
            // Attempt to stand up
            if (CanStandUp()) {
                isCrouching = false;
                isAttemptingToStand = false;
            } else {
                // Can't stand up due to ceiling, stay crouched
                isAttemptingToStand = true;
            }
        } else {
            // Start crouching
            isCrouching = true;
            isAttemptingToStand = false;
        }
    }

    /// <summary>
    /// Checks if there's enough clearance to stand up
    /// </summary>
    /// <returns>True if player can stand up, false otherwise</returns>
    private bool CanStandUp() {
        Vector3 capsuleTop = transform.position + Vector3.up * (standingHeight / 2f);
        float radius = controller.radius;
        
        // Check for obstacles above the player
        Collider[] hits = Physics.OverlapCapsule(
            transform.position + Vector3.up * (crouchingHeight / 2f),
            capsuleTop,
            radius,
            Physics.AllLayers,
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider hit in hits) {
            if (hit.gameObject != gameObject && !hit.isTrigger) {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Adjusts the character controller height and position smoothly
    /// </summary>
    /// <param name="newHeight">Target height for the controller</param>
    private void AdjustControllerHeight(float newHeight) {
        float heightDifference = newHeight - controller.height;
        
        // Adjust controller height
        controller.height = newHeight;
        
        // Adjust center to maintain foot position
        controller.center = Vector3.up * (newHeight / 2f);
        
        // Move player vertically to maintain ground contact
        transform.position += Vector3.up * (heightDifference / 2f);
    }

    private void OnEnable() {
        // Subscribe to room events
        RoomManager.Instance.OnRoomLoaded += OnRoomEntered;
        RoomManager.Instance.OnTransitionStarted += OnTransitionStart;
    }

    private void OnDisable() {
        RoomManager.Instance.OnRoomLoaded -= OnRoomEntered;
        RoomManager.Instance.OnTransitionStarted -= OnTransitionStart;
    }

    private void OnRoomEntered(IRoom room) {
        // Set current room reference
        currentRoom = room;
        
        // Room-specific player constraints are handled by input methods
        // Jump/crouch restrictions are applied at input time
    }
    
    private void EnableAllMovement() {
        // Ensure all movement is enabled in maze/boss rooms
        // This overrides any previous restrictions
    }
    
    private void DisableJump() {
        // In horror rooms, jump is disabled
        // We can implement this by setting jump force to 0
        // or by not processing jump input
        // For now, we'll just log it
        Debug.Log("Jump disabled in this room type");
    }

    private void OnTransitionStart() {
        // Handle room transition start
        // Reset any player states that need to be reset during transitions
        if (isCrouching) {
            // Make sure player can stand during transitions
            isAttemptingToStand = true;
        }
    }


}
