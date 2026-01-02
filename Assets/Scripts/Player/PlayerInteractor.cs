using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerInteractor : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactionDistance = 3f;
    public LayerMask interactionLayerMask = ~0;
    public KeyCode interactKey = KeyCode.E;
    
    [Header("Visual Feedback")]
    public Color highlightColor = Color.yellow;
    public float highlightIntensity = 1.5f;
    
    private Interactable currentTarget;
    private Renderer currentHighlightRenderer;
    private Material currentMaterial;
    private MaterialPropertyBlock propertyBlock;
    
    private Camera playerCamera;
    private bool isInteractionEnabled = true;

    private void Awake()
    {
        playerCamera = GetComponentInChildren<Camera>();
        propertyBlock = new MaterialPropertyBlock();
        
        if (GameInputManager.Instance != null)
        {
            GameInputManager.Instance.OnInteract += HandleInteractInput;
        }
    }

    private void Update()
    {
        if (!isInteractionEnabled) return;
        
        CheckForInteractable();
    }

    private void CheckForInteractable()
    {
        // Clear previous highlight
        ClearHighlight();
        
        if (playerCamera == null) return;
        
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;
        
        // Visual debug ray
        Debug.DrawRay(ray.origin, ray.direction * interactionDistance, Color.blue, 0.1f);
        
        if (Physics.Raycast(ray, out hit, interactionDistance, interactionLayerMask))
        {
            Interactable interactable = hit.collider.GetComponent<Interactable>();
            if (interactable != null)
            {
                currentTarget = interactable;
                HighlightTarget(hit.collider);
                
                // Show interaction prompt in UI (TODO: Add UI system)
                // Debug.Log($"Press [{interactKey}] to {interactable.prompt}");
            }
        }
        else
        {
            currentTarget = null;
        }
    }

    private void HighlightTarget(Collider targetCollider)
    {
        Renderer renderer = targetCollider.GetComponent<Renderer>();
        if (renderer != null)
        {
            currentHighlightRenderer = renderer;
            currentMaterial = renderer.material;
            
            // Apply highlight effect
            propertyBlock.SetColor("_EmissionColor", highlightColor * highlightIntensity);
            renderer.SetPropertyBlock(propertyBlock);
        }
    }

    private void ClearHighlight()
    {
        if (currentHighlightRenderer != null)
        {
            currentHighlightRenderer.SetPropertyBlock(null);
            currentHighlightRenderer = null;
        }
        currentTarget = null;
    }

    private void HandleInteractInput()
    {
        if (currentTarget != null && isInteractionEnabled)
        {
            currentTarget.Interact(gameObject);
        }
    }

    public void EnableInteraction(bool enable)
    {
        isInteractionEnabled = enable;
        if (!enable)
        {
            ClearHighlight();
        }
    }

    private void OnDestroy()
    {
        if (GameInputManager.Instance != null)
        {
            GameInputManager.Instance.OnInteract -= HandleInteractInput;
        }
        ClearHighlight();
    }
}