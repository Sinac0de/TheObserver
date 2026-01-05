using UnityEngine;

/// <summary>
/// Handles the breathing audio effect during the intro sequence.
/// Creates a subtle, rhythmic breathing sound to enhance the awakening feeling.
/// </summary>
public class BreathingAudioController : MonoBehaviour
{
    [Header("Breathing Settings")]
    [SerializeField] private float breathInterval = 3f;
    [SerializeField] private float breathVolume = 0.3f;
    [SerializeField] private AudioClip[] breathingSounds;
    
    [Header("Random Variation")]
    [SerializeField] private float intervalVariation = 0.5f;
    [SerializeField] private float volumeVariation = 0.1f;
    
    private AudioSource audioSource;
    private float nextBreathTime;
    private bool shouldPlayBreathing = true;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.volume = breathVolume;
        
        // Schedule first breath after a short delay
        nextBreathTime = Time.time + Random.Range(0.5f, 1.5f);
    }

    private void Update()
    {
        if (shouldPlayBreathing && Time.time >= nextBreathTime && breathingSounds.Length > 0)
        {
            PlayBreath();
        }
    }

    private void PlayBreath()
    {
        AudioClip clip = breathingSounds[Random.Range(0, breathingSounds.Length)];
        if (clip != null)
        {
            audioSource.clip = clip;
            audioSource.volume = breathVolume + Random.Range(-volumeVariation, volumeVariation);
            audioSource.Play();
            
            // Schedule next breath
            float interval = breathInterval + Random.Range(-intervalVariation, intervalVariation);
            nextBreathTime = Time.time + interval;
        }
    }

    /// <summary>
    /// Stops the breathing audio
    /// </summary>
    public void StopBreathing()
    {
        shouldPlayBreathing = false;
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    /// <summary>
    /// Starts/resumes the breathing audio
    /// </summary>
    public void StartBreathing()
    {
        shouldPlayBreathing = true;
        nextBreathTime = Time.time + Random.Range(0.5f, 1.5f);
    }

    /// <summary>
    /// Called when the intro sequence is complete
    /// </summary>
    public void OnIntroComplete()
    {
        StopBreathing();
    }
}