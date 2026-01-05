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
    public float jumpCutMultiplier = 0.5f;

    [Header("Crouch")]
    public float standingHeight = 2f;
    public float crouchingHeight = 1f;
    public float crouchTransitionSpeed = 10f;
    public float headbobReduction = 0.5f;

    [Header("Gravity")]
    public float gravityMultiplier = 2f;
    public float fallMultiplier = 3f;
    public float lowJumpMultiplier = 2f;

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
    
    // Metrics for AI analysis
    private float movementSpeedTracker = 0f;
    private float standingStillTime = 0f;
    private float lastMovementTime = 0f;
    private Vector3 lastPosition = Vector3.zero;
    private PlayerHealth playerHealth;
    
    [Header("Intro State")]
    private bool isIntroSequence = false;
    private bool canMoveDuringIntro = false;
    private bool canLookDuringIntro = true;
    
    public float MovementSpeedTracker => movementSpeedTracker;
    public float StandingStillTime => standingStillTime;

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

        // Ensure we start in a stable grounded state (no initial pop)
        verticalVelocity = -2f;
    }



    private void Update() {
        if (!controller.enabled) return;  // skip movement when controller is disabled

        HandleInput();
        GroundCheck();
        JumpBuffering();
        HandleCrouch();
        CalculateMovement();
        ApplyMovement();
        Look();
        UpdateCameraState();
        TrackPlayerMetrics();
    }
    
    private void TrackPlayerMetrics() {
        // Track movement speed
        Vector3 currentPos = transform.position;
        float distance = Vector3.Distance(lastPosition, currentPos);
        float deltaTime = Time.deltaTime;
        
        if (deltaTime > 0) {
            float currentSpeed = distance / deltaTime;
            
            // Calculate a normalized movement speed (0-1 range based on walk/sprint speeds)
            float maxSpeed = Mathf.Max(walkSpeed, sprintSpeed);
            movementSpeedTracker = Mathf.Clamp01(currentSpeed / maxSpeed);
            
            // Update standing still time
            if (currentSpeed < 0.1f) { // Considered standing still if moving very slowly
                standingStillTime += deltaTime;
            } else {
                standingStillTime = 0f; // Reset when player moves
            }
        }
        
        lastPosition = currentPos;
    }

    private void Start() {
        playerHealth = GetComponent<PlayerHealth>();
        lastPosition = transform.position;
        lastMovementTime = Time.time;
    }
    
    // Add methods to handle intro state
    public void SetIntroState(bool introState)
    {
        isIntroSequence = introState;
        if (introState)
        {
            // Disable movement initially during intro
            canMoveDuringIntro = false;
            canLookDuringIntro = true; // Always allow looking during intro
        }
        else
        {
            // Enable full controls after intro
            canMoveDuringIntro = true;
            canLookDuringIntro = true;
        }
    }

    public void SetIntroMovementState(bool canMove)
    {
        canMoveDuringIntro = canMove;
    }

    public void SetIntroLookState(bool canLook)
    {
        canLookDuringIntro = canLook;
    }

    private void HandleInput() {
        if (isIntroSequence)
        {
            // Handle intro-specific input restrictions
            HandleIntroInput();
            return;
        }

        // Original input handling for normal gameplay
        moveInput = GameInputManager.Instance.GetMoveVector();
        lookInput = GameInputManager.Instance.GetLookVector();

        bool sprintAllowed = true; 
        isSprinting = sprintAllowed && GameInputManager.Instance.GetSprintInput() && moveInput.magnitude > 0.1f;

        // Jump input
        if (GameInputManager.Instance.IsJumpPressed()) {
            lastJumpPressTime = Time.time;
            GameInputManager.Instance.ConsumeJumpPress();
        }

        // Crouch input
        bool crouchInput = GameInputManager.Instance.GetCrouchInput();

        if (crouchInput && !isCrouchPressed) {
            isCrouchPressed = true;
            lastCrouchPressTime = Time.time;
            ToggleCrouch();
        } else if (!crouchInput) {
            isCrouchPressed = false;
        }
    }
    
    private void HandleIntroInput()
    {
        if (canLookDuringIntro)
        {
            lookInput = GameInputManager.Instance.GetLookVector();
        }
        else
        {
            lookInput = Vector2.zero;
        }

        if (canMoveDuringIntro)
        {
            moveInput = GameInputManager.Instance.GetMoveVector();
            isSprinting = GameInputManager.Instance.GetSprintInput() && moveInput.magnitude > 0.1f;

            // Jump input during intro
            if (GameInputManager.Instance.IsJumpPressed()) {
                lastJumpPressTime = Time.time;
                GameInputManager.Instance.ConsumeJumpPress();
            }

            // Crouch input during intro
            bool crouchInput = GameInputManager.Instance.GetCrouchInput();
            if (crouchInput && !isCrouchPressed) {
                isCrouchPressed = true;
                lastCrouchPressTime = Time.time;
                ToggleCrouch();
            } else if (!crouchInput) {
                isCrouchPressed = false;
            }
        }
        else
        {
            moveInput = Vector2.zero;
            isSprinting = false;
        }
    }

    private void GroundCheck() {
        bool grounded = controller.isGrounded;
        if (grounded && !isGrounded)
            airJumpsRemaining = maxAirJumps;

        isGrounded = grounded;
        if (isGrounded) lastGroundedTime = Time.time;
        if (isGrounded && verticalVelocity < 0) verticalVelocity = -2f;

        if (isGrounded) isAttemptingToStand = false;
    }

    private void JumpBuffering() {
        bool canJump = (isGrounded || Time.time - lastGroundedTime <= coyoteTime) && verticalVelocity <= 0;
        bool hasAirJumps = !isGrounded && airJumpsRemaining > 0;
        bool bufferActive = Time.time - lastJumpPressTime <= jumpBufferTime;

        if ((canJump || hasAirJumps) && bufferActive) {
            float actualJumpForce = isCrouching ? crouchJumpForce : jumpForce;
            verticalVelocity = actualJumpForce;

            if (isGrounded) {
                moveVelocity.y = 0f;
            }
            if (!isGrounded) airJumpsRemaining--;
            cameraController?.AddShake(0.1f, 0.1f);
            lastJumpPressTime = 0f;
        }
    }

    private void CalculateMovement() {
        float targetSpeed = walkSpeed;

        if (isCrouching) {
            targetSpeed = crouchSpeed;
        } else if (isSprinting) {
            targetSpeed = sprintSpeed;
        }

        Vector3 inputDir = new Vector3(moveInput.x, 0, moveInput.y).normalized;
        Vector3 targetVelocity = transform.TransformDirection(inputDir) * targetSpeed;

        if (!isGrounded) {
            Vector3 currentHorizontalVelocity = new Vector3(moveVelocity.x, 0, moveVelocity.z);
            Vector3 targetHorizontalVelocity = new Vector3(targetVelocity.x, 0, targetVelocity.z);

            Vector3 newHorizontalVelocity = Vector3.Lerp(
                currentHorizontalVelocity,
                targetHorizontalVelocity,
                airControl * Time.deltaTime * 10f
            );
            targetVelocity = newHorizontalVelocity + new Vector3(0, targetVelocity.y, 0);
        }

        moveVelocity = Vector3.SmoothDamp(
            moveVelocity,
            targetVelocity,
            ref smoothVelocity,
            moveInput.magnitude > 0 ? 1 / acceleration : 1 / deceleration
        );
        isMoving = inputDir.magnitude > 0;
    }

    private void ApplyMovement() {
        float gravity = Physics.gravity.y;
        if (verticalVelocity < 0) {
            verticalVelocity += gravity * fallMultiplier * Time.deltaTime;
        } else if (verticalVelocity > 0 && !GameInputManager.Instance.IsJumpHeld()) {
            verticalVelocity += gravity * jumpCutMultiplier * Time.deltaTime;
        } else {
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

    private void HandleCrouch() {
        targetHeight = isCrouching ? crouchingHeight : standingHeight;

        currentHeight = Mathf.Lerp(controller.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);

        if (isAttemptingToStand && !isCrouching) {
            if (CanStandUp()) {
                isAttemptingToStand = false;
            } else {
                currentHeight = crouchingHeight;
                targetHeight = crouchingHeight;
            }
        }

        if (Mathf.Abs(controller.height - currentHeight) > 0.01f) {
            AdjustControllerHeight(currentHeight);
        }
    }

    private void ToggleCrouch() {
        if (isCrouching) {
            if (CanStandUp()) {
                isCrouching = false;
                isAttemptingToStand = false;
            } else {
                isAttemptingToStand = true;
            }
        } else {
            isCrouching = true;
            isAttemptingToStand = false;
        }
    }

    private bool CanStandUp() {
        Vector3 capsuleTop = transform.position + Vector3.up * (standingHeight / 2f);
        float radius = controller.radius;

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

    private void AdjustControllerHeight(float newHeight) {
        float heightDifference = newHeight - controller.height;

        controller.height = newHeight;
        controller.center = Vector3.up * (newHeight / 2f);
        transform.position += Vector3.up * (heightDifference / 2f);
    }

    // --- Public APIs ---


    public void ForceStandUp() {
        // standing up in elevator or other forced scenarios
        if (isCrouching) {
            isCrouching = false;
            isAttemptingToStand = true;
        }
    }

    /// <summary>
    /// Resets movement-related state when respawning (e.g. in elevator).
    /// </summary>
    public void ResetMovementState() {
        moveVelocity = Vector3.zero;
        smoothVelocity = Vector3.zero;
        verticalVelocity = -2f; // small downward vel so controller stays grounded
        isSprinting = false;
        isMoving = false;
    }

    public void TeleportTo(Vector3 position, Quaternion rotation) {
        StartCoroutine(TeleportRoutine(position, rotation));
    }

    private System.Collections.IEnumerator TeleportRoutine(Vector3 position, Quaternion rotation) {
        var cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        transform.SetPositionAndRotation(position, rotation);

        ResetMovementState();
        ForceStandUp();

        yield return null; // wait 1 frame

        if (cc != null) cc.enabled = true;
    }

}
