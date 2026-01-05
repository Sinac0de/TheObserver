using UnityEngine;

/// <summary>
/// An audio-focused threat that mimics player sounds and creates tension through audio.
/// Less visually present, more about sound: footsteps mimicking the player, whispers, etc.
/// </summary>
public class EchoEntity : MonoBehaviour
{
    [Header("Echo Settings")]
    [SerializeField] private float hearingRange = 15f; // Range to detect player
    [SerializeField] private float mimicDistance = 8f; // Distance to stay from player
    [SerializeField] private float lingerTime = 3f; // How long to stay after player leaves area
    [SerializeField] private float idleWaitTime = 5f; // How long to wait in idle state
    
    [Header("Audio Settings")]
    [SerializeField] private AudioClip[] mimicFootsteps; // Footstep sounds to mimic player
    [SerializeField] private AudioClip[] whispers; // Whisper sounds
    [SerializeField] private float whisperVolume = 0.3f;
    [SerializeField] private float mimicVolume = 0.5f;
    
    [Header("AI Model Integration")]
    [SerializeField] private float aiModelFrequencyMultiplier = 1f;
    [SerializeField] private float aiModelIntensityMultiplier = 1f;
    
    private Transform player;
    private AudioSource audioSource;
    private bool isPlayerInHearingRange;
    private bool isPlayingMimic;
    private float lastPlayerPositionTime;
    private Vector3 lastPlayerPosition;
    private float idleTimer;
    
    private enum EchoState { Idle, Mimicking, Lurking, Following }
    private EchoState currentState;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        
        if (player == null)
        {
            Debug.LogWarning("EchoEntity: No player found with tag 'Player'", this);
        }
        
        currentState = EchoState.Idle;
        lastPlayerPositionTime = -1000f; // Initialize to long ago
        idleTimer = 0f;
    }

    private void Update()
    {
        if (player == null) return;
        
        UpdateAIBasedOnModel();
        UpdateHearingDetection();
        UpdateState();
    }

    private void UpdateAIBasedOnModel()
    {
        if (GameManager.Instance?.AIModel != null)
        {
            float complexity = GameManager.Instance.AIModel.CurrentComplexity;
            
            // Adjust behavior based on AI model complexity
            aiModelFrequencyMultiplier = 1f + (complexity * 0.7f); // More frequent with complexity
            aiModelIntensityMultiplier = 1f + (complexity * 0.5f); // More intense with complexity
        }
    }

    private void UpdateHearingDetection()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        isPlayerInHearingRange = distanceToPlayer <= hearingRange;
        
        if (isPlayerInHearingRange)
        {
            lastPlayerPosition = player.position;
            lastPlayerPositionTime = Time.time;
        }
    }

    private void UpdateState()
    {
        float timeSincePlayer = Time.time - lastPlayerPositionTime;
        
        switch (currentState)
        {
            case EchoState.Idle:
                idleTimer += Time.deltaTime;
                
                // Check if player is nearby to start mimicking
                if (isPlayerInHearingRange)
                {
                    currentState = EchoState.Mimicking;
                    PlayRandomMimicSound();
                }
                // Or check if player just left area to start lurking
                else if (timeSincePlayer < lingerTime)
                {
                    currentState = EchoState.Lurking;
                }
                else if (idleTimer > idleWaitTime)
                {
                    // Randomly play whispers when idle
                    if (Random.value < (0.1f / idleWaitTime) * Time.deltaTime * aiModelFrequencyMultiplier)
                    {
                        PlayRandomWhisper();
                        idleTimer = 0f;
                    }
                }
                break;

            case EchoState.Mimicking:
                // Continue mimicking while player is in range
                if (isPlayerInHearingRange)
                {
                    // Randomly play mimic sounds at intervals
                    if (Random.value < (0.3f * Time.deltaTime) * aiModelFrequencyMultiplier)
                    {
                        PlayRandomMimicSound();
                    }
                }
                else if (timeSincePlayer > lingerTime)
                {
                    currentState = EchoState.Idle;
                    idleTimer = 0f;
                }
                break;

            case EchoState.Lurking:
                // Stay in this state for lingerTime after player leaves
                if (timeSincePlayer > lingerTime)
                {
                    currentState = EchoState.Idle;
                    idleTimer = 0f;
                }
                else if (isPlayerInHearingRange)
                {
                    // Player returned, go back to mimicking
                    currentState = EchoState.Mimicking;
                    PlayRandomMimicSound();
                }
                break;

            case EchoState.Following:
                // For future implementation - following player at a distance
                if (timeSincePlayer > lingerTime)
                {
                    currentState = EchoState.Idle;
                    idleTimer = 0f;
                }
                break;
        }
    }

    private void PlayRandomMimicSound()
    {
        if (mimicFootsteps != null && mimicFootsteps.Length > 0)
        {
            int index = Random.Range(0, mimicFootsteps.Length);
            audioSource.PlayOneShot(mimicFootsteps[index], mimicVolume * aiModelIntensityMultiplier);
        }
    }

    private void PlayRandomWhisper()
    {
        if (whispers != null && whispers.Length > 0)
        {
            int index = Random.Range(0, whispers.Length);
            audioSource.PlayOneShot(whispers[index], whisperVolume * aiModelIntensityMultiplier);
        }
    }

    /// <summary>
    /// Called when player stands still for too long to trigger audio threat.
    /// </summary>
    public void TriggerOnPlayerStillness()
    {
        if (currentState == EchoState.Idle)
        {
            PlayRandomWhisper();
            currentState = EchoState.Mimicking;
            lastPlayerPositionTime = Time.time;
            lastPlayerPosition = player.position;
        }
    }

    /// <summary>
    /// Called by AI model when difficulty changes to update behavior parameters.
    /// </summary>
    public void UpdateDifficulty(float complexity)
    {
        aiModelFrequencyMultiplier = 1f + (complexity * 0.7f);
        aiModelIntensityMultiplier = 1f + (complexity * 0.5f);
    }

    private void OnDrawGizmosSelected()
    {
        // Draw hearing range gizmo
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, hearingRange);
        
        // Draw mimic distance gizmo
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, mimicDistance);
    }
}