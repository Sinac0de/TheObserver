using UnityEngine;

public class HackTerminalInteractable : Interactable
{
    [Header("Hack Settings")]
    public string hackPrompt = "Hack Terminal";
    public float hackDuration = 3.0f;
    public float stunDuration = 5.0f;
    
    [Header("Visual Feedback")]
    public ParticleSystem hackParticles;
    public Light terminalLight;
    public AudioClip hackSound;
    public AudioClip successSound;
    
    private AudioSource audioSource;
    private bool isHacked = false;
    private bool isBeingHacked = false;

    private void Awake()
    {
        prompt = hackPrompt;
        audioSource = GetComponent<AudioSource>();
    }

    public override void Interact(GameObject interactor)
    {
        if (isHacked || isBeingHacked) return;
        
        isBeingHacked = true;
        Debug.Log("Starting hack sequence...");
        
        // Visual feedback
        if (hackParticles != null)
            hackParticles.Play();
            
        if (terminalLight != null)
        {
            terminalLight.color = Color.yellow;
        }
        
        if (audioSource != null && hackSound != null)
            audioSource.PlayOneShot(hackSound);
        
        // Start hack sequence
        Invoke(nameof(CompleteHack), hackDuration);
    }

    private void CompleteHack()
    {
        isHacked = true;
        isBeingHacked = false;
        
        Debug.Log("Hack successful! Stunned enemies.");
        
        // Success effects
        if (terminalLight != null)
        {
            terminalLight.color = Color.green;
        }
        
        if (audioSource != null && successSound != null)
            audioSource.PlayOneShot(successSound);
        
        // TODO: Notify boss room to stun enemies
        // BossRoomController bossRoom = FindObjectOfType<BossRoomController>();
        // if (bossRoom != null)
        // {
        //     bossRoom.StunEnemies(stunDuration);
        // }
    }

    public bool IsHacked => isHacked;
    public bool IsBeingHacked => isBeingHacked;
}