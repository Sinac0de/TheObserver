using UnityEngine;

public class DollInteractable : Interactable
{
    [Header("Doll Settings")]
    public string dollPrompt = "Collect Doll";
    
    [Header("Effects")]
    public ParticleSystem collectionParticles;
    public AudioClip collectionSound;
    
    private AudioSource audioSource;
    private bool isCollected = false;

    private void Awake()
    {
        prompt = dollPrompt;
        audioSource = GetComponent<AudioSource>();
    }

    public override void Interact(GameObject interactor)
    {
        if (isCollected) return;
        
        isCollected = true;
        Debug.Log("Doll collected!");
        
        // Visual/audio feedback
        if (collectionParticles != null)
            collectionParticles.Play();
            
        if (audioSource != null && collectionSound != null)
            audioSource.PlayOneShot(collectionSound);
        
        // Notify horror room controller
        HorrorRoomController horrorRoom = FindObjectOfType<HorrorRoomController>();
        if (horrorRoom != null)
        {
            horrorRoom.OnDollCollected();
        }
        
        // Disable this interactable
        gameObject.SetActive(false);
    }
}