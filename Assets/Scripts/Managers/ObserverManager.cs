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