using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FPSCameraController : MonoBehaviour {
    [Header("Camera Settings")]
    public float mouseSensitivity = 2f;
    public float rotationSmoothTime = 0.05f;

    [Header("Rotation Constraints")]
    public float minXRotation = -90f;
    public float maxXRotation = 90f;

    [Header("FOV")]
    public float baseFOV = 60f;
    public float sprintFOV = 75f;
    public float aimFOV = 55f;
    public float fovLerpSpeed = 8f;

    [Header("Head Bob")]
    public bool enableHeadBob = true;
    public float bobFrequency = 1.5f; // per step
    public float bobAmplitude = 0.05f;
    public AnimationCurve bobCurve = AnimationCurve.Linear(0, 0, 1, 1);
    private float headbobMultiplier = 1f;

    [Header("Camera Sway")]
    public float swayAmount = 0.02f;
    public float swaySmooth = 4f;

    [Header("Shake")]
    public bool enableShake = true;
    private Vector3 shakeOffset;
    private float shakeIntensity;
    private float shakeDuration;

    // Private
    private Camera cam;
    private Vector2 currentMouseDelta;
    private Vector2 currentMouseDeltaVelocity;

    private float xRotation;
    private float yRotation;

    private Vector3 initialLocalPos;
    private float currentFOV;

    // Head bob
    private float bobTimer;

    // State
    private bool isMoving;
    private bool isSprinting;
    private bool isCrouching;
    private Vector3 crouchOffset = Vector3.zero;
    private Vector3 targetCrouchOffset = Vector3.zero;

    private void Awake() {
        cam = GetComponent<Camera>();
        initialLocalPos = transform.localPosition;
        currentFOV = baseFOV;
        cam.fieldOfView = currentFOV;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void HandleLook(Vector2 lookInput) {
        // Smooth input
        currentMouseDelta = Vector2.SmoothDamp(currentMouseDelta, lookInput, ref currentMouseDeltaVelocity, rotationSmoothTime);

        yRotation += currentMouseDelta.x * mouseSensitivity;
        xRotation -= currentMouseDelta.y * mouseSensitivity;
        xRotation = Mathf.Clamp(xRotation, minXRotation, maxXRotation);
    }

    public void SetMovementState(bool moving) => isMoving = moving;
    public void SetSprinting(bool sprinting) => isSprinting = sprinting;
    public void SetCrouching(bool crouching) => isCrouching = crouching;
    public void SetHeadbobMultiplier(float multiplier) => headbobMultiplier = multiplier;

    public void AddShake(float intensity, float duration) {
        if (!enableShake) return;
        shakeIntensity = Mathf.Max(shakeIntensity, intensity);
        shakeDuration = Mathf.Max(shakeDuration, duration);
    }

    private void LateUpdate() {
        // Rotation
        transform.parent.localRotation = Quaternion.Euler(0, yRotation, 0);
        transform.localRotation = Quaternion.Euler(xRotation, 0, 0);

        // FOV
        float targetFOV = baseFOV;
        if (isSprinting) targetFOV = sprintFOV;
        currentFOV = Mathf.Lerp(currentFOV, targetFOV, fovLerpSpeed * Time.deltaTime);
        cam.fieldOfView = currentFOV;

        // Crouch offset calculation
        targetCrouchOffset = isCrouching ? Vector3.down * 0.3f : Vector3.zero;
        crouchOffset = Vector3.Lerp(crouchOffset, targetCrouchOffset, 10f * Time.deltaTime);

        // Head bob
        Vector3 bobOffset = Vector3.zero;
        if (enableHeadBob && isMoving) {
            bobTimer += Time.deltaTime * bobFrequency * (isSprinting ? 1.5f : 1f);
            float curveValue = bobCurve.Evaluate(Mathf.PingPong(bobTimer, 1f));
            bobOffset.y = Mathf.Sin(bobTimer * Mathf.PI * 2f) * bobAmplitude * curveValue * headbobMultiplier;
            bobOffset.x = Mathf.Cos(bobTimer * Mathf.PI * 2f) * bobAmplitude * 0.5f * headbobMultiplier;
        }

        // Camera sway
        Vector3 swayOffset = new Vector3(-currentMouseDelta.x, -currentMouseDelta.y, 0) * swayAmount;
        swayOffset = Vector3.Lerp(Vector3.zero, swayOffset, Time.deltaTime * swaySmooth);

        // Shake
        if (enableShake && shakeDuration > 0) {
            shakeDuration -= Time.deltaTime;
            float s = Mathf.Clamp01(shakeDuration);
            float px = (Mathf.PerlinNoise(Time.time * 10f, 0f) - 0.5f) * 2f;
            float py = (Mathf.PerlinNoise(0f, Time.time * 10f) - 0.5f) * 2f;
            shakeOffset = new Vector3(px, py, 0) * shakeIntensity * s * 0.05f;
        } else {
            shakeOffset = Vector3.zero;
        }

        transform.localPosition = initialLocalPos + crouchOffset + bobOffset + swayOffset + shakeOffset;
    }
}
