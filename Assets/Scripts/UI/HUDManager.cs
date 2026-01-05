using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the Heads-Up Display (HUD) showing health, flashlight battery, and timer.
/// </summary>
public class HUDManager : MonoBehaviour
{
    [Header("HUD Elements")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private Slider flashlightBatteryBar;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI batteryText;
    
    [Header("Flashlight Reference")]
    [SerializeField] private FlashlightController flashlightController;
    
    [Header("Timer Reference")]
    [SerializeField] private TimerManager timerManager;
    
    [Header("Player Health Reference")]
    [SerializeField] private PlayerHealth playerHealth;
    
    [Header("Visual Effects")]
    [SerializeField] private Image healthWarningOverlay;
    [SerializeField] private Image batteryWarningOverlay;
    [SerializeField] private Image timerWarningOverlay;
    
    [Header("Warning Thresholds")]
    [SerializeField] private float lowHealthThreshold = 30f;
    [SerializeField] private float lowBatteryThreshold = 20f;
    [SerializeField] private float lowTimeThreshold = 60f;

    private void Start()
    {
        if (playerHealth == null)
        {
            playerHealth = FindObjectOfType<PlayerHealth>();
        }
        
        if (flashlightController == null)
        {
            flashlightController = FindObjectOfType<FlashlightController>();
        }
        
        if (timerManager == null)
        {
            timerManager = FindObjectOfType<TimerManager>();
        }
        
        UpdateHUD();
    }
    
    private void Update()
    {
        UpdateHUD();
    }
    
    private void UpdateHUD()
    {
        // Update health bar and text
        if (playerHealth != null && healthBar != null)
        {
            float healthPercentage = (float)playerHealth.CurrentHealth / playerHealth.MaxHealth * 100f;
            healthBar.value = healthPercentage;
            
            if (healthText != null)
            {
                healthText.text = Mathf.RoundToInt(healthPercentage) + "%";
            }
            
            // Show warning if health is low
            if (healthWarningOverlay != null)
            {
                healthWarningOverlay.enabled = healthPercentage <= lowHealthThreshold;
            }
        }
        
        // Update flashlight battery bar and text
        if (flashlightController != null)
        {
            float batteryPercentage = flashlightController.BatteryPercentage;
            
            if (flashlightBatteryBar != null)
            {
                flashlightBatteryBar.value = batteryPercentage;
            }
            
            if (batteryText != null)
            {
                batteryText.text = Mathf.RoundToInt(batteryPercentage) + "%";
            }
            
            // Show warning if battery is low
            if (batteryWarningOverlay != null)
            {
                batteryWarningOverlay.enabled = flashlightController.IsBatteryLow;
            }
        }
        
        // Update timer text
        if (timerManager != null && timerText != null)
        {
            timerText.text = timerManager.GetFormattedTime();
            
            // Show warning if time is running low
            if (timerWarningOverlay != null)
            {
                timerWarningOverlay.enabled = timerManager.RemainingTime <= lowTimeThreshold && timerManager.IsRunning;
            }
        }
    }
}