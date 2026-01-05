using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Central HUD controller for health, flashlight battery, and run timer,
/// with subtle warning overlays for low resources.
/// </summary>
public class HUDManager : MonoBehaviour {
    [Header("HUD Elements")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private Slider flashlightBatteryBar;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI batteryText;

    [Header("Gameplay References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private FlashlightController flashlightController;
    [SerializeField] private TimerManager timerManager;

    [Header("Warning Overlays")]
    [SerializeField] private Image healthWarningOverlay;
    [SerializeField] private Image batteryWarningOverlay;
    [SerializeField] private Image timerWarningOverlay;

    [Header("Warning Thresholds")]
    [SerializeField] private float lowHealthThreshold = 30f;   // percent
    [SerializeField] private float lowBatteryThreshold = 20f;  // percent
    [SerializeField] private float lowTimeThreshold = 60f;     // seconds

    private void Start() {
        CacheReferencesIfMissing();
        UpdateHUD();
    }

    private void Update() {
        UpdateHUD();
    }

    private void CacheReferencesIfMissing() {
        if (playerHealth == null)
            playerHealth = FindObjectOfType<PlayerHealth>();

        if (flashlightController == null)
            flashlightController = FindObjectOfType<FlashlightController>();

        if (timerManager == null)
            timerManager = FindObjectOfType<TimerManager>();
    }

    private void UpdateHUD() {
        UpdateHealthSection();
        UpdateBatterySection();
        UpdateTimerSection();
    }

    private void UpdateHealthSection() {
        if (playerHealth == null || healthBar == null)
            return;

        float healthPercent = (float)playerHealth.CurrentHealth / playerHealth.MaxHealth * 100f;
        healthBar.value = healthPercent;

        if (healthText != null)
            healthText.text = $"{Mathf.RoundToInt(healthPercent)}%";

        if (healthWarningOverlay != null)
            healthWarningOverlay.enabled = healthPercent <= lowHealthThreshold;
    }

    private void UpdateBatterySection() {
        if (flashlightController == null)
            return;

        float batteryPercent = flashlightController.BatteryPercentage;

        if (flashlightBatteryBar != null)
            flashlightBatteryBar.value = batteryPercent;

        if (batteryText != null)
            batteryText.text = $"{Mathf.RoundToInt(batteryPercent)}%";

        if (batteryWarningOverlay != null)
            batteryWarningOverlay.enabled = flashlightController.IsBatteryLow;
    }

    private void UpdateTimerSection() {
        if (timerManager == null)
            return;

        if (timerText != null)
            timerText.text = timerManager.GetFormattedTime();

        if (timerWarningOverlay != null) {
            bool isLowTime = timerManager.RemainingTime <= lowTimeThreshold && timerManager.IsRunning;
            timerWarningOverlay.enabled = isLowTime;
        }
    }
}
