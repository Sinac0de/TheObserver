using UnityEngine;

/// <summary>
/// Base class for traps in the maze that can damage the player and trigger various effects.
/// </summary>
public abstract class Trap : Interactable
{
    [Header("Trap Settings")]
    [SerializeField] protected int damageAmount = 25;
    [SerializeField] protected float triggerCooldown = 2f;
    [SerializeField] protected bool canTriggerMultipleTimes = true;
    
    [Header("Effects")]
    [SerializeField] protected bool triggerFlashlightFlicker = true;
    [SerializeField] protected float flashlightFlickerDuration = 0.5f;
    [SerializeField] protected bool causeCameraShake = true;
    [SerializeField] protected float cameraShakeIntensity = 0.5f;
    [SerializeField] protected float cameraShakeDuration = 0.3f;

    protected bool isTriggered = false;
    protected float lastTriggerTime = -1000f; // Initialize to long ago

    protected virtual void Start()
    {
        lastTriggerTime = -1000f; // Ensure trap is ready to trigger
    }

    public override void Interact(GameObject interactor)
    {
        // Check if trap can be triggered (cooldown, one-time use, etc.)
        if (CanTrigger())
        {
            Trigger(interactor);
        }
    }

    protected bool CanTrigger()
    {
        // Check cooldown
        if (Time.time - lastTriggerTime < triggerCooldown)
        {
            return false;
        }

        // Check if it's a one-time trap
        if (!canTriggerMultipleTimes && isTriggered)
        {
            return false;
        }

        return true;
    }

    protected virtual void Trigger(GameObject player)
    {
        lastTriggerTime = Time.time;
        isTriggered = true;

        // Apply damage to player
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.ApplyTrapDamageAndImmune();
        }

        // Apply effects
        ApplyEffects(player);

        // Call specific trap behavior
        OnTrapTriggered(player);
    }

    protected virtual void ApplyEffects(GameObject player)
    {
        // Trigger flashlight flicker
        if (triggerFlashlightFlicker)
        {
            FlashlightController flashlight = player.GetComponentInChildren<FlashlightController>();
            if (flashlight != null)
            {
                flashlight.TriggerFlicker();
            }
        }

        // Cause camera shake
        if (causeCameraShake)
        {
            FPSCameraController cameraController = player.GetComponentInChildren<FPSCameraController>();
            if (cameraController != null)
            {
                cameraController.AddShake(cameraShakeIntensity, cameraShakeDuration);
            }
        }
    }

    /// <summary>
    /// Override this method to implement specific trap behavior.
    /// </summary>
    protected abstract void OnTrapTriggered(GameObject player);
}