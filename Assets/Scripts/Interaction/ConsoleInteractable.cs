using UnityEngine;

public class ConsoleInteractable : Interactable
{
    [Header("Console Settings")]
    public string consolePrompt = "Start Next Trial";
    public float activationDelay = 1.0f;
    
    [Header("Visual Effects")]
    public ParticleSystem activationParticles;
    public Light consoleLight;
    public AudioClip activationSound;
    
    private AudioSource audioSource;
    private bool isActivated = false;

    private void Awake()
    {
        prompt = consolePrompt;
        audioSource = GetComponent<AudioSource>();
    }

    public override void Interact(GameObject interactor)
    {
        if (isActivated) return;
        
        isActivated = true;
        Debug.Log("Console activated - Loading next room...");
        
        // Visual feedback
        if (activationParticles != null)
            activationParticles.Play();
            
        if (consoleLight != null)
        {
            consoleLight.color = Color.green;
            consoleLight.intensity *= 2f;
        }
        
        if (audioSource != null && activationSound != null)
            audioSource.PlayOneShot(activationSound);
        
        // Load next room after delay
        Invoke(nameof(LoadNextRoom), activationDelay);
    }

    private void LoadNextRoom()
    {
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.LoadNextRoom();
        }
    }

    public void ResetConsole()
    {
        isActivated = false;
        if (consoleLight != null)
        {
            consoleLight.color = Color.blue;
            consoleLight.intensity /= 2f;
        }
    }
}