using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Main controller for the elevator intro sequence.
/// Manages the complete flow: darkness -> awakening -> observer briefing -> playable state.
/// </summary>
public class ElevatorIntroSequence : MonoBehaviour
{
    [Header("Player Setup")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform playerSpawnPoint;
    
    [Header("Intro Sequence Timing")]
    [SerializeField] private float darknessDuration = 2f; // Time in darkness before red light
    [SerializeField] private float headControlUnlockDelay = 4f; // After voice starts
    [SerializeField] private float fullMovementUnlockDelay = 9f; // Before doors open
    [SerializeField] private float doorOpenDelay = 14f; // Total time before doors open
    
    [Header("Visual Elements")]
    [SerializeField] private Light emergencyLight; // Red emergency light
    [SerializeField] private Animator elevatorDoorsAnimator; // Door animation controller
    [SerializeField] private GameObject timerDisplay; // Timer display above door
    [SerializeField] private GameObject exitSign; // Exit sign for visual cue
    [SerializeField] private GameObject[] flickeringLights; // Lights that flicker during warnings
    [SerializeField] private GameObject fadePanel; // UI panel for fade effects
    
    [Header("Audio")]
    [SerializeField] private AudioSource ambientAudioSource;
    [SerializeField] private AudioClip[] metalCreaks;
    [SerializeField] private AudioClip[] machineryHum;
    [SerializeField] private AudioClip playerBreathing;
    [SerializeField] private AudioClip elevatorDing;
    
    [Header("Environmental Controller")]
    [SerializeField] private IntroEnvironmentalController environmentalController;
    
    [Header("Player Controllers")]
    private PlayerController playerController;
    private FPSCameraController cameraController;
    private GameObject playerInstance;
    private bool isIntroPlaying = true;

    private void Start()
    {
        InitializeIntroSequence();
    }

    private void InitializeIntroSequence()
    {
        // Create player at spawn point
        playerInstance = Instantiate(playerPrefab, playerSpawnPoint.position, playerSpawnPoint.rotation);
        playerController = playerInstance.GetComponent<PlayerController>();
        cameraController = playerInstance.GetComponentInChildren<FPSCameraController>();
        
        // Configure player for intro state
        if (playerController != null)
        {
            playerController.SetIntroState(true);
            playerController.SetIntroMovementState(false); // No movement initially
        }
        
        // Set up the environment
        SetupEnvironment();
        
        // Start the intro coroutine
        StartCoroutine(IntroSequence());
    }

    private void SetupEnvironment()
    {
        // Initially disable emergency light
        if (emergencyLight != null) 
        {
            emergencyLight.enabled = false;
            emergencyLight.intensity = 0.5f; // Lower intensity for subtle effect
        }
        
        // Setup audio sources
        if (ambientAudioSource != null)
        {
            ambientAudioSource.loop = true;
            ambientAudioSource.volume = 0.3f;
            PlayRandomAmbientSound();
        }
        
        // Hide timer display initially
        if (timerDisplay != null) timerDisplay.SetActive(false);
        
        // Hide exit sign initially
        if (exitSign != null) exitSign.SetActive(false);
        
        // Start breathing sound
        StartCoroutine(PlayBreathingSound());
    }

    private IEnumerator PlayBreathingSound()
    {
        yield return new WaitForSeconds(darknessDuration + 0.5f); // Slightly after lights come on
        
        if (playerBreathing != null)
        {
            AudioSource.PlayClipAtPoint(playerBreathing, playerInstance.transform.position, 0.7f);
        }
    }

    private void PlayRandomAmbientSound()
    {
        if (metalCreaks.Length > 0 && Random.value > 0.5f)
        {
            AudioClip clip = metalCreaks[Random.Range(0, metalCreaks.Length)];
            ambientAudioSource.PlayOneShot(clip, 0.4f);
        }
        else if (machineryHum.Length > 0)
        {
            AudioClip clip = machineryHum[Random.Range(0, machineryHum.Length)];
            ambientAudioSource.PlayOneShot(clip, 0.3f);
        }
    }

    private IEnumerator IntroSequence()
    {
        // Phase 1: Darkness with audio only
        yield return new WaitForSeconds(darknessDuration);
        
        // Turn on emergency red light
        if (emergencyLight != null) 
        {
            emergencyLight.enabled = true;
            // Add subtle flicker effect
            StartCoroutine(FlickerLight(emergencyLight, 0.5f, 3));
        }
        
        // Play breathing sound
        if (playerBreathing != null)
        {
            AudioSource.PlayClipAtPoint(playerBreathing, playerInstance.transform.position, 0.8f);
        }
        
        // Start observer voice - play the full intro sequence
        if (ObserverManager.Instance != null)
        {
            ObserverManager.Instance.PlayFullIntroSequence();
        }
        
        // Phase 2: Unlock head look control
        yield return new WaitForSeconds(headControlUnlockDelay);
        UnlockHeadLookControl();
        
        // Phase 3: Show environmental cues as observer explains rules
        StartCoroutine(ShowEnvironmentalCues());
        
        // Phase 4: Unlock full movement
        yield return new WaitForSeconds(fullMovementUnlockDelay - headControlUnlockDelay);
        UnlockFullMovementControl();
        
        // Phase 5: Show timer and open doors
        yield return new WaitForSeconds(doorOpenDelay - fullMovementUnlockDelay - headControlUnlockDelay);
        
        // Show timer display
        ShowTimerDisplay();
        
        // Play elevator ding
        if (elevatorDing != null)
        {
            AudioSource.PlayClipAtPoint(elevatorDing, playerInstance.transform.position);
        }
        
        // Open doors with animation
        if (elevatorDoorsAnimator != null)
        {
            elevatorDoorsAnimator.SetBool("IsOpen", true);
        }
        
        // Final transition to playable state
        yield return new WaitForSeconds(0.5f);
        CompleteIntroSequence();
    }

    private IEnumerator FlickerLight(Light light, float duration, int flickerCount)
    {
        float flickerInterval = duration / flickerCount;
        for (int i = 0; i < flickerCount; i++)
        {
            light.enabled = !light.enabled;
            yield return new WaitForSeconds(flickerInterval * 0.3f);
            light.enabled = !light.enabled;
            yield return new WaitForSeconds(flickerInterval * 0.7f);
        }
    }

    private void UnlockHeadLookControl()
    {
        // Allow limited head movement initially
        if (playerController != null)
        {
            // Keep movement disabled but allow looking
            playerController.SetIntroLookState(true);
            playerController.SetIntroMovementState(false);
        }
    }

    private void UnlockFullMovementControl()
    {
        // Enable full player movement
        if (playerController != null)
        {
            playerController.SetIntroMovementState(true);
            playerController.SetIntroState(false); // Exit intro state
        }
    }

    private IEnumerator ShowEnvironmentalCues()
    {
        // Wait for observer to start explaining rules
        yield return new WaitForSeconds(2f);
        
        // Show timer display when voice mentions timer
        if (timerDisplay != null)
        {
            timerDisplay.SetActive(true);
            yield return new WaitForSeconds(1.5f);
            timerDisplay.SetActive(false);
            yield return new WaitForSeconds(0.5f);
            timerDisplay.SetActive(true); // Keep it on after this point
        }
        
        // Show exit sign when voice mentions exit
        if (exitSign != null)
        {
            exitSign.SetActive(true);
            yield return new WaitForSeconds(1.5f);
            exitSign.SetActive(false);
            yield return new WaitForSeconds(0.3f);
            exitSign.SetActive(true); // Keep it on after this point
        }
        
        // Flicker lights when voice mentions hiding (around 8-9s into sequence)
        yield return new WaitForSeconds(6f); // From start of cues
        
        if (flickeringLights != null && flickeringLights.Length > 0)
        {
            foreach (GameObject lightObj in flickeringLights)
            {
                if (lightObj != null)
                {
                    Light light = lightObj.GetComponent<Light>();
                    if (light != null)
                    {
                        StartCoroutine(FlickerLight(light, 1f, 5));
                    }
                }
            }
        }
    }

    private void ShowTimerDisplay()
    {
        // Initialize timer to 5 minutes and show it
        if (TimerManager.Instance != null)
        {
            TimerManager.Instance.SetTimeLimit(300f); // 5 minutes
            TimerManager.Instance.StartTimer();
        }
        
        if (timerDisplay != null)
        {
            timerDisplay.SetActive(true);
        }
    }

    private void CompleteIntroSequence()
    {
        isIntroPlaying = false;
        
        // Notify environmental controller that intro is complete
        if (environmentalController != null)
        {
            environmentalController.OnIntroComplete();
        }
        
        // Any other cleanup or setup for normal gameplay
        Debug.Log("[IntroSequence] Intro sequence completed. Player is now fully controllable.");
    }

    // This method would be called when player dies to respawn them
    public void RespawnPlayer()
    {
        if (!isIntroPlaying)
        {
            // Reload the scene to reset to elevator
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    private void OnDisable()
    {
        // Cleanup any ongoing coroutines if needed
    }
}