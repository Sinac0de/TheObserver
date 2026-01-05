using UnityEngine;

/// <summary>
/// Manages the AI Observer voice system that provides feedback and commentary.
/// </summary>
public class ObserverManager : MonoBehaviour
{
    [Header("Observer Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] introClips;
    [SerializeField] private AudioClip[] deathClips;
    [SerializeField] private AudioClip[] timeoutClips;
    [SerializeField] private AudioClip[] successClips;
    [SerializeField] private AudioClip[] generalComments;
    
    [Header("Volume & Pitch")]
    [SerializeField] private float voiceVolume = 1f;
    [SerializeField] private float voicePitch = 1f;
    [SerializeField] private float minPitchVariation = 0.95f;
    [SerializeField] private float maxPitchVariation = 1.05f;

    private static ObserverManager instance;
    
    public static ObserverManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ObserverManager>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("ObserverManager");
                    instance = obj.AddComponent<ObserverManager>();
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.volume = voiceVolume;
            audioSource.pitch = voicePitch;
        }
    }

    /// <summary>
    /// Plays the intro message when the player starts the game.
    /// </summary>
    public void PlayIntroMessage()
    {
        PlayRandomClipFrom(introClips, "Intro");
    }

    /// <summary>
    /// Plays the full intro sequence with progressive dialogue.
    /// </summary>
    public void PlayFullIntroSequence()
    {
        StartCoroutine(IntroSequenceCoroutine());
    }

    private System.Collections.IEnumerator IntroSequenceCoroutine()
    {
        // Line 1: Wake up
        yield return PlayIntroLine("Wake up.");
        yield return new WaitForSeconds(1f);
        
        // Line 2: Location
        yield return PlayIntroLine("You are in Service Elevator 13, deep beneath the facility.");
        yield return new WaitForSeconds(1f);
        
        // Line 3: Elevator safety
        yield return PlayIntroLine("This elevator is your only safe place between test runs.");
        yield return new WaitForSeconds(1f);
        
        // Line 4: Maze explanation
        yield return PlayIntroLine("When the doors open, you'll enter a procedurally generated maze filled with traps and an entity that hunts in the dark.");
        yield return new WaitForSeconds(1f);
        
        // Line 5: Timer explanation
        yield return PlayIntroLine("A timer will start as soon as you step out. When it hits zero, the floor becomes lethal.");
        yield return new WaitForSeconds(1f);
        
        // Line 6: Exit rule
        yield return PlayIntroLine("Find the EXIT sign on each floor to proceed.");
        yield return new WaitForSeconds(1f);
        
        // Line 7: Entity warning
        yield return PlayIntroLine("Avoid things that still move in the dark. If lights flicker, hide immediately.");
        yield return new WaitForSeconds(1f);
        
        // Line 8: Adaptive system
        yield return PlayIntroLine("Every move you make is recorded. Every mistake feeds the maze.");
        yield return new WaitForSeconds(1f);
        
        // Line 9: Behavior adaptation
        yield return PlayIntroLine("If you rush, it chases faster. If you freeze, it closes in slower... but it will still find you.");
        yield return new WaitForSeconds(1f);
        
        // Line 10: Final ominous line
        yield return PlayIntroLine("When the doors open: move. The elevator always brings you back, but it never makes it easier.");
    }

    private System.Collections.IEnumerator PlayIntroLine(string text)
    {
        // This would integrate with text-to-speech or pre-recorded audio
        Debug.Log($"[Observer] {text}");
        
        // If using audio clips, play from introClips array
        // For now, we'll just log it - in a real implementation you'd have specific audio clips
        yield return new WaitForSeconds(text.Length / 20f); // Approximate speaking time
    }

    /// <summary>
    /// Plays a death message based on player behavior.
    /// </summary>
    public void PlayDeathMessage()
    {
        PlayRandomClipFrom(deathClips, "Death");
    }

    /// <summary>
    /// Plays a timeout message when the timer expires.
    /// </summary>
    public void PlayTimeoutMessage()
    {
        PlayRandomClipFrom(timeoutClips, "Timeout");
    }

    /// <summary>
    /// Plays a success message when the player completes a challenge.
    /// </summary>
    public void PlaySuccessMessage()
    {
        PlayRandomClipFrom(successClips, "Success");
    }

    /// <summary>
    /// Plays a general comment during gameplay.
    /// </summary>
    public void PlayGeneralComment()
    {
        PlayRandomClipFrom(generalComments, "General");
    }

    /// <summary>
    /// Plays a specific message about player behavior.
    /// </summary>
    public void PlayBehaviorComment(string behaviorType)
    {
        // This would be expanded with specific behavior-based messages
        PlayGeneralComment();
    }

    private void PlayRandomClipFrom(AudioClip[] clips, string clipType)
    {
        if (clips == null || clips.Length == 0)
        {
            Debug.LogWarning($"[ObserverManager] No {clipType} clips assigned!");
            return;
        }

        int randomIndex = Random.Range(0, clips.Length);
        AudioClip clip = clips[randomIndex];
        
        if (clip != null)
        {
            // Apply random pitch variation for more natural feel
            audioSource.pitch = voicePitch * Random.Range(minPitchVariation, maxPitchVariation);
            audioSource.PlayOneShot(clip, voiceVolume);
        }
        else
        {
            Debug.LogWarning($"[ObserverManager] {clipType} clip at index {randomIndex} is null!");
        }
    }

    /// <summary>
    /// Plays a custom message with provided text (for dynamic AI responses).
    /// </summary>
    public void PlayCustomMessage(string message)
    {
        // This would integrate with text-to-speech or pre-recorded dynamic phrases
        Debug.Log($"[Observer] {message}");
    }

    /// <summary>
    /// Stops any currently playing audio.
    /// </summary>
    public void StopCurrentMessage()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    /// <summary>
    /// Checks if the observer is currently speaking.
    /// </summary>
    public bool IsSpeaking => audioSource != null && audioSource.isPlaying;
}