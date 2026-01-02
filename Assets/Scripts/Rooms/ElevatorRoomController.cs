using UnityEngine;

public class ElevatorRoomController : BaseRoomController
{
    [Header("Elevator Settings")]
    public ConsoleInteractable floorButton; // The "Enter Next Trial" button
    public Animator doorAnimator;
    public float doorOpenDelay = 1.0f;
    
    [Header("Prompt Settings")]
    public string defaultPrompt = "Enter Next Trial";
    public string completedAllPrompt = "All Trials Completed!";
    
    private bool canEnterTrial = false;

    protected override void ApplyAIDifficulty()
    {
        // Elevator room doesn't adapt, but shows current progress
        Debug.Log($"Elevator loaded. Current progress: {GameManager.Instance.RoomsCompleted}/3 rooms completed");
        
        if (floorButton != null)
        {
            // Set appropriate prompt based on progress
            if (GameManager.Instance.RoomsCompleted >= 3)
            {
                floorButton.consolePrompt = completedAllPrompt;
            }
            else
            {
                floorButton.consolePrompt = defaultPrompt;
            }
            
            // Enable button after a short delay to allow for animations
            Invoke(nameof(EnableFloorButton), doorOpenDelay);
        }
        
        // Open elevator doors
        if (doorAnimator != null)
        {
            doorAnimator.SetTrigger("Open");
        }
    }

    private void EnableFloorButton()
    {
        if (floorButton != null)
        {
            floorButton.GetComponent<Collider>().enabled = true;
            canEnterTrial = true;
        }
    }

    public override void OnPlayerEnter()
    {
        base.OnPlayerEnter();
        Debug.Log("Player entered elevator room.");
    }

    public void EnterNextTrial()
    {
        if (!canEnterTrial) return;
        
        if (GameManager.Instance.RoomsCompleted >= 3)
        {
            // All rooms completed - win condition
            Debug.Log("All trials completed! Player wins!");
            // TODO: Trigger escape sequence
            return;
        }
        
        // Load the next appropriate room based on progress
        if (RoomManager.Instance != null)
        {
            int nextRoomIndex = GameManager.Instance.RoomsCompleted;
            
            switch (nextRoomIndex)
            {
                case 0:
                    RoomManager.Instance.LoadSpecificRoom(RoomManager.RoomType.Maze);
                    break;
                case 1:
                    RoomManager.Instance.LoadSpecificRoom(RoomManager.RoomType.Horror);
                    break;
                case 2:
                    RoomManager.Instance.LoadSpecificRoom(RoomManager.RoomType.Boss);
                    break;
                default:
                    Debug.Log("All trials completed!");
                    break;
            }
        }
    }

    public override bool CanExit()
    {
        // Player doesn't normally exit - they enter trials
        return false;
    }
}