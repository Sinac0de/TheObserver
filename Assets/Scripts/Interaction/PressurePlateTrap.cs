using UnityEngine;

/// <summary>
/// A pressure plate trap that triggers when the player steps on it.
/// </summary>
public class PressurePlateTrap : Trap
{
    [Header("Pressure Plate Settings")]
    [SerializeField] private float detectionRadius = 1f;
    [SerializeField] private LayerMask playerLayer = 1 << 0; // Default layer
    
    [Header("Visual Feedback")]
    [SerializeField] private Renderer plateRenderer;
    [SerializeField] private Color normalColor = Color.gray;
    [SerializeField] private Color triggeredColor = Color.red;
    [SerializeField] private float triggerAnimationDuration = 0.2f;

    private Material plateMaterial;
    private Color originalColor;

    protected override void Start()
    {
        base.Start();
        
        if (plateRenderer != null)
        {
            plateMaterial = plateRenderer.material;
            originalColor = plateMaterial.color;
        }
    }

    private void Update()
    {
        // Check if player is standing on the pressure plate
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius, playerLayer);
        
        foreach (Collider collider in hitColliders)
        {
            // Check if the collider belongs to the player
            if (collider.CompareTag("Player"))
            {
                // Trigger the trap if player is on the plate and it can be triggered
                if (CanTrigger())
                {
                    Trigger(collider.gameObject);
                }
                break; // Only need to check one player collider
            }
        }
    }

    protected override void OnTrapTriggered(GameObject player)
    {
        Debug.Log("[PressurePlateTrap] Triggered by player!", this);
        
        // Visual feedback - change color temporarily
        if (plateMaterial != null)
        {
            plateMaterial.color = triggeredColor;
            Invoke(nameof(ResetPlateColor), triggerAnimationDuration);
        }
        
        // Register mistake with maze room
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth?.currentMazeRoom != null)
        {
            playerHealth.currentMazeRoom.RegisterMistake();
        }
    }

    private void ResetPlateColor()
    {
        if (plateMaterial != null)
        {
            plateMaterial.color = originalColor;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw detection radius gizmo
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}