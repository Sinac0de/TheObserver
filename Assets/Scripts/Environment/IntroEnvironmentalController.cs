using UnityEngine;

/// <summary>
/// Manages environmental effects during the intro sequence.
/// Handles flickering lights, maintenance notes, and other atmospheric elements.
/// </summary>
public class IntroEnvironmentalController : MonoBehaviour
{
    [Header("Environmental Elements")]
    [SerializeField] private GameObject[] maintenanceNotes; // Visible notes/messags on walls
    [SerializeField] private Light[] flickeringLights;
    [SerializeField] private Renderer[] scratchedWalls; // Walls with scratched messages
    [SerializeField] private GameObject timerDisplay; // Timer display above door
    [SerializeField] private GameObject exitSign; // Exit sign for visual cues
    [SerializeField] private GameObject[] warningStickers; // Warning stickers on control panel
    
    [Header("Flicker Settings")]
    [SerializeField] private float flickerSpeed = 10f;
    [SerializeField] private float flickerIntensity = 0.5f;
    
    [Header("Scratched Messages")]
    [TextArea] [SerializeField] private string[] scratchedMessages = {
        "DON'T DIE THE SAME WAY TWICE",
        "IT LEARNS YOU",
        "DATA CAPTURED",
        "PATTERN RECOGNIZED",
        "ADAPTATION PROTOCOL ENGAGED"
    };

    private bool isIntroActive = true;
    private bool isFlickering = false;

    private void Start()
    {
        InitializeEnvironment();
    }

    private void InitializeEnvironment()
    {
        // Set up scratched messages on walls
        SetupScratchedMessages();
        
        // Initially hide timer and exit sign
        if (timerDisplay != null) timerDisplay.SetActive(false);
        if (exitSign != null) exitSign.SetActive(false);
    }

    private void SetupScratchedMessages()
    {
        // Apply random scratched messages to wall materials
        if (scratchedWalls != null && scratchedWalls.Length > 0)
        {
            foreach (Renderer wall in scratchedWalls)
            {
                if (wall != null && wall.material != null)
                {
                    // In a real implementation, you might use texture overlays or text meshes
                    // For now, we'll just log that these should have messages
                    int randomMessageIndex = Random.Range(0, scratchedMessages.Length);
                    Debug.Log($"[Environment] Wall has message: '{scratchedMessages[randomMessageIndex]}'");
                }
            }
        }
    }

    /// <summary>
    /// Triggers flickering lights for environmental cue
    /// </summary>
    public void TriggerFlickeringEffect(float duration = 1f)
    {
        if (!isIntroActive) return;
        
        StartCoroutine(FlickerLightsCoroutine(duration));
    }

    private System.Collections.IEnumerator FlickerLightsCoroutine(float duration)
    {
        if (flickeringLights == null || flickeringLights.Length == 0) yield break;
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            foreach (Light flickeringLight in flickeringLights)
            {
                if (flickeringLight != null)
                {
                    // Randomly flicker lights
                    float randomIntensity = Random.Range(0f, flickerIntensity);
                    flickeringLight.intensity = 1f * randomIntensity;
                }
            }
            
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        
        // Restore normal light intensity
        foreach (Light flickeringLight in flickeringLights)
        {
            if (flickeringLight != null)
            {
                flickeringLight.intensity = 1f; // Assuming default intensity
            }
        }
    }

    /// <summary>
    /// Shows the timer display for visual cue
    /// </summary>
    public void ShowTimerDisplay()
    {
        if (timerDisplay != null) timerDisplay.SetActive(true);
    }

    /// <summary>
    /// Shows the exit sign for visual cue
    /// </summary>
    public void ShowExitSign()
    {
        if (exitSign != null) exitSign.SetActive(true);
    }

    /// <summary>
    /// Briefly highlights maintenance notes for visual cue
    /// </summary>
    public void HighlightMaintenanceNotes(float duration = 2f)
    {
        if (maintenanceNotes == null || maintenanceNotes.Length == 0) return;
        
        // Briefly highlight notes
        foreach (GameObject note in maintenanceNotes)
        {
            if (note != null)
            {
                // In a real implementation, you might change material properties or add glow
                // For now, just a visual indicator
            }
        }
        
        Invoke(nameof(RestoreMaintenanceNotes), duration);
    }

    private void RestoreMaintenanceNotes()
    {
        if (maintenanceNotes == null || maintenanceNotes.Length == 0) return;
        
        foreach (GameObject note in maintenanceNotes)
        {
            if (note != null)
            {
                // Restore original appearance
            }
        }
    }

    /// <summary>
    /// Called when intro sequence completes
    /// </summary>
    public void OnIntroComplete()
    {
        isIntroActive = false;
        // Stop any ongoing environmental effects
    }
}