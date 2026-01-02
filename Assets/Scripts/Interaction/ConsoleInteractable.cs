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

    [Header("Floor Selection")]
    public bool isFloorSelector = false;
    public int floorNumber = 1;
    
    public override void Interact(GameObject interactor)
    {
        if (isActivated) return;
        
        isActivated = true;
        
        if (isFloorSelector)
        {
            Debug.Log($"Floor {floorNumber} console activated...");
            
            // For elevator hub, call the hub controller
            ElevatorHubController hub = FindObjectOfType<ElevatorHubController>();
            if (hub != null)
            {
                hub.GoToFloor(floorNumber);
            }
            else
            {
                // Fallback: try to load specific room
                LoadSpecificRoom();
            }
        }
        else
        {
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
    }
    
    private void LoadNextRoom()
    {
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.LoadNextRoom();
        }
    }
    
    private void LoadSpecificRoom()
    {
        if (RoomManager.Instance != null)
        {
            RoomManager.RoomType roomType = RoomManager.RoomType.Maze;
            switch (floorNumber)
            {
                case 1:
                    roomType = RoomManager.RoomType.Maze;
                    break;
                case 2:
                    roomType = RoomManager.RoomType.Horror;
                    break;
                case 3:
                    roomType = RoomManager.RoomType.Boss;
                    break;
            }
            RoomManager.Instance.LoadSpecificRoom(roomType);
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