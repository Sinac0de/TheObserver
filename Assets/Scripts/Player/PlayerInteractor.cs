using UnityEngine;

public class PlayerInteractor : MonoBehaviour {
    [Header("Interaction Settings")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactionLayerMask = ~0;

    [Header("Visual Feedback")]
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private float highlightIntensity = 1.5f;

    private Interactable currentTarget;
    private Renderer currentHighlightRenderer;
    private MaterialPropertyBlock propertyBlock;

    private Camera playerCamera;
    private bool isInteractionEnabled = true;

    // Reused raycast hit to avoid extra allocations
    private RaycastHit hitInfo;

    private void Awake() {
        // Cache camera reference once
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null) {
            Debug.LogWarning("[PlayerInteractor] No Camera found in children. Please assign a Camera under the Player.");
        }

        propertyBlock = new MaterialPropertyBlock();

    }

    private void Start() {

            GameInputManager.Instance.OnInteract += HandleInteractInput;
    }

    private void OnDestroy() {
        if (GameInputManager.Instance != null) {
            GameInputManager.Instance.OnInteract -= HandleInteractInput;
        }
        ClearHighlight();
    }

    private void Update() {
        if (!isInteractionEnabled || playerCamera == null)
            return;

        ScanForInteractable();
    }

    /// <summary>
    /// Optional external setup if you want to override the auto-found camera.
    /// </summary>
    public void SetCamera(Camera cam) {
        playerCamera = cam;
    }

    private void ScanForInteractable() {
        // Start from camera forward
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        // Clear previous highlight by default
        ClearHighlight();

        if (Physics.Raycast(ray, out hitInfo, interactionDistance, interactionLayerMask, QueryTriggerInteraction.Ignore)) {
            var interactable = hitInfo.collider.GetComponent<Interactable>();
            if (interactable != null) {
                currentTarget = interactable;
                ApplyHighlight(hitInfo.collider);
            } else {
                currentTarget = null;
            }
        } else {
            currentTarget = null;
        }
    }

    private void ApplyHighlight(Collider targetCollider) {
        // Try to get a Renderer either on this collider or its parent
        Renderer renderer = targetCollider.GetComponent<Renderer>();
        if (renderer == null) {
            renderer = targetCollider.GetComponentInParent<Renderer>();
        }

        if (renderer == null)
            return;

        currentHighlightRenderer = renderer;

        // Apply emission highlight via MaterialPropertyBlock
        propertyBlock.Clear();
        propertyBlock.SetColor("_EmissionColor", highlightColor * highlightIntensity);
        renderer.SetPropertyBlock(propertyBlock);
    }

    private void ClearHighlight() {
        if (currentHighlightRenderer != null) {
            currentHighlightRenderer.SetPropertyBlock(null);
            currentHighlightRenderer = null;
        }
    }

    private void HandleInteractInput() {
        if (!isInteractionEnabled)
            return;


        if (currentTarget != null) {
            Debug.Log("[currentTarget]");

            currentTarget.Interact(gameObject);
        }
    }

    /// <summary>
    /// Enables or disables interaction scanning and highlighting.
    /// </summary>
    public void EnableInteraction(bool enable) {
        isInteractionEnabled = enable;

        if (!enable) {
            ClearHighlight();
            currentTarget = null;
        }
    }
}
