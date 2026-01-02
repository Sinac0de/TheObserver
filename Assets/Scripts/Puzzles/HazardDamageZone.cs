using UnityEngine;

public class HazardDamageZone : MonoBehaviour 
{
    [Header("Hazard Settings")]
    [Tooltip("Damage per second applied to the player while inside this zone.")]
    public float damagePerSecond = 10f;
    
    [Tooltip("Minimum time between mistake registrations (seconds)")]
    public float mistakeCooldown = 1.0f;
    
    private IRoom currentRoom;
    private float lastMistakeTime = -1f;
    private bool playerInside = false;

    private void Awake()
    {
        currentRoom = GetComponentInParent<IRoom>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        
        playerInside = true;
        RegisterMistake();
        InvokeRepeating(nameof(ApplyDamage), 0f, 1f);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        
        playerInside = false;
        CancelInvoke(nameof(ApplyDamage));
    }

    private void RegisterMistake()
    {
        if (currentRoom == null) return;
        
        float currentTime = Time.time;
        if (currentTime - lastMistakeTime >= mistakeCooldown)
        {
            currentRoom.RegisterMistake();
            lastMistakeTime = currentTime;
            Debug.Log($"Hazard triggered! Mistakes: {currentRoom.Mistakes}");
        }
    }

    private void ApplyDamage()
    {
        if (!playerInside) return;
        
        // TODO: Apply actual damage to player health system
        Debug.Log($"Taking {damagePerSecond} damage from hazard");
        
        // Re-register mistake periodically while in hazard
        RegisterMistake();
    }

    private void OnDestroy()
    {
        CancelInvoke(nameof(ApplyDamage));
    }
}
