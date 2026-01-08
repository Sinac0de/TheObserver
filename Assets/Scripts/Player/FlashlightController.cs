using UnityEngine;

/// <summary>
/// Manages the player's flashlight with battery consumption and flicker effects.
/// </summary>
public class FlashlightController : MonoBehaviour
{
    [Header("Flashlight Settings")]
    [SerializeField] private Light flashlight;
    [SerializeField] private float maxBattery = 100f;
    [SerializeField] private float batteryDrainRate = 10f; // per second when on
    [SerializeField] private float focusedBeamDrainMultiplier = 2f; // when using focused beam
    [SerializeField] private float flickerDuration = 0.5f;
    [SerializeField] private float flickerInterval = 5f; // How often to flicker randomly
    
    [Header("Flicker Effects")]
    [SerializeField] private float minFlickerIntensity = 0.2f;
    [SerializeField] private float maxFlickerIntensity = 1f;

    private float currentBattery;
    private bool isOn;
    private bool isFlickering;
    private float flickerTimer;
    private float nextFlickerTime;

    public float BatteryPercentage => (currentBattery / maxBattery) * 100f;
    public bool IsOn => isOn && currentBattery > 0f;
    public bool IsBatteryLow => currentBattery <= (maxBattery * 0.2f); // 20% or less

    private void Start()
    {
        currentBattery = maxBattery;
        isOn = true; // Start with flashlight on
        UpdateFlashlightState();
        SetNextFlickerTime();
    }

    private void Update()
    {
        if (isOn)
        {
            DrainBattery();
        }
        
        HandleFlicker();
        
        // Toggle flashlight with key (T by default)
        if (Input.GetKeyDown(KeyCode.F))
        {
            ToggleFlashlight();
        }
    }

    private void DrainBattery()
    {
        if (!MazeRoomController.Instance.GetIsRunning()) {
            float drainRate = batteryDrainRate * Time.deltaTime;


            currentBattery = Mathf.Clamp(currentBattery - drainRate, 0f, maxBattery);

            if (currentBattery <= 0f) {
                TurnOff();
            }

            UpdateFlashlightState();
        }
        
    }

    private void UpdateFlashlightState()
    {
        if (flashlight != null)
        {
            flashlight.enabled = IsOn;
            
            if (flashlight.enabled)
            {
                // Adjust intensity based on battery level
                float batteryRatio = currentBattery / maxBattery;
                flashlight.intensity = Mathf.Lerp(0.5f, 1f, batteryRatio);
            }
        }
    }

    private void HandleFlicker()
    {
        if (!isOn) return;
        
        flickerTimer += Time.deltaTime;
        
        if (flickerTimer >= nextFlickerTime)
        {
            StartFlicker();
        }
    }

    private void StartFlicker()
    {
        if (currentBattery > 20f) return; // Only flicker when battery is low
        
        isFlickering = true;
        Invoke(nameof(EndFlicker), flickerDuration);
        SetNextFlickerTime();
    }

    private void EndFlicker()
    {
        isFlickering = false;
        UpdateFlashlightState();
    }

    private void SetNextFlickerTime()
    {
        nextFlickerTime = Random.Range(flickerInterval * 0.5f, flickerInterval * 2f);
        flickerTimer = 0f;
    }

    /// <summary>
    /// Toggles the flashlight on/off.
    /// </summary>
    public void ToggleFlashlight()
    {
        if (isOn)
        {
            TurnOff();
        }
        else
        {
            TurnOn();
        }
    }

    /// <summary>
    /// Turns the flashlight on.
    /// </summary>
    public void TurnOn()
    {
        isOn = true;
        UpdateFlashlightState();
    }

    /// <summary>
    /// Turns the flashlight off.
    /// </summary>
    public void TurnOff()
    {
        isOn = false;
        UpdateFlashlightState();
    }

    /// <summary>
    /// Triggers a flicker effect, useful for jump scares or enemy proximity.
    /// </summary>
    public void TriggerFlicker()
    {
        if (!isFlickering)
        {
            isFlickering = true;
            Invoke(nameof(EndFlicker), flickerDuration);
        }
    }

    /// <summary>
    /// Refills the battery to full capacity.
    /// </summary>
    public void RefillBattery()
    {
        currentBattery = maxBattery;
    }

    /// <summary>
    /// Adds a specific amount to the battery.
    /// </summary>
    public void AddBattery(float amount)
    {
        currentBattery = Mathf.Clamp(currentBattery + amount, 0f, maxBattery);
    }
}