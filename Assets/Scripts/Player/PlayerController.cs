using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour {
    private CharacterController controller;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpHeight = 1.5f;
    private float gravity = -9.81f;
    private Vector3 velocity;

    [Header("Look")]
    public float mouseSensitivity = 2f;
    public Transform playerCamera;
    private float xRotation = 0f;

    private void Awake() {
        controller = GetComponent<CharacterController>();
    }

    private void OnEnable() {
        GameInputManager.Instance.OnJump += Jump;
        GameInputManager.Instance.OnInteract += Interact;
        GameInputManager.Instance.OnFire += Fire;
    }

    private void OnDisable() {
        GameInputManager.Instance.OnJump -= Jump;
        GameInputManager.Instance.OnInteract -= Interact;
        GameInputManager.Instance.OnFire -= Fire;
    }

    private void Update() {
        Move();
        Look();
    }

    private void Move() {
        Vector2 input = GameInputManager.Instance.MoveInput;
        Vector3 move = transform.right * input.x + transform.forward * input.y;
        controller.Move(move * moveSpeed * Time.deltaTime);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;
    }

    private void Look() {
        Vector2 look = GameInputManager.Instance.LookInput;
        xRotation -= look.y * mouseSensitivity;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        playerCamera.localRotation = Quaternion.Euler(xRotation, 0, 0);
        transform.Rotate(Vector3.up * look.x * mouseSensitivity);
    }

    private void Jump() {
        if (controller.isGrounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }

    private void Interact() {
        Debug.Log("Interact pressed");
    }

    private void Fire() {
        Debug.Log("Fire pressed");
    }
}
