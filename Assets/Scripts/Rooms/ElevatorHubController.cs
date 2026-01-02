using UnityEngine;

public class ElevatorHubController : BaseRoomController
{
    [Header("Elevator Configuration")]
    public ConsoleInteractable floor1Button; // Maze Floor
    public ConsoleInteractable floor2Button; // Horror Floor  
    public ConsoleInteractable floor3Button; // Boss Floor
    public Animator doorAnimator;
    
    [Header("Floor Prefabs")]
    public GameObject mazeFloorPrefab;
    public GameObject horrorFloorPrefab;
    public GameObject bossFloorPrefab;
    
    private bool[] floorsUnlocked = new bool[3]; // Floor 1, 2, 3 unlocked
    
    private int targetFloorNumber = 0; // Used for invoke callback
    
    protected override void ApplyAIDifficulty()
    {
        // Hub doesn't adapt, but shows AI status
        Debug.Log("Elevator Hub loaded. AI Complexity: " + (aiModel?.CurrentComplexity ?? 0f));
        
        // Unlock floors based on progress
        UpdateFloorAvailability();
    }
    
    private void Start()
    {
        SetupButtons();
        UpdateFloorAvailability();
    }
    
    private void SetupButtons()
    {
        if (floor1Button != null)
        {
            floor1Button.consolePrompt = "Maze Floor (1)";
            floor1Button.GetComponent<Collider>().enabled = true;
        }
        
        if (floor2Button != null)
        {
            floor2Button.consolePrompt = "Horror Floor (2)";
            floor2Button.GetComponent<Collider>().enabled = true;
        }
        
        if (floor3Button != null)
        {
            floor3Button.consolePrompt = "Boss Floor (3)";
            floor3Button.GetComponent<Collider>().enabled = true;
        }
    }
    
    private void UpdateFloorAvailability()
    {
        // Update button availability based on current progress
        int currentProgress = GameManager.Instance.RoomsCompleted;
        
        if (floor1Button != null)
            floor1Button.GetComponent<Collider>().enabled = (currentProgress == 0);
            
        if (floor2Button != null)
            floor2Button.GetComponent<Collider>().enabled = (currentProgress == 1);
            
        if (floor3Button != null)
            floor3Button.GetComponent<Collider>().enabled = (currentProgress == 2);
    }
    
    public void GoToFloor(int floorNumber)
    {
        if (floorNumber < 1 || floorNumber > 3) return;
        
        // Check if this is the NEXT floor in sequence
        int expectedFloor = GameManager.Instance.RoomsCompleted + 1;
        if (floorNumber != expectedFloor)
        {
            Debug.Log($"Cannot access floor {floorNumber}. Must complete floor {expectedFloor - 1} first!");
            return;
        }
        
        // Animate door closing, then load floor
        if (doorAnimator != null)
        {
            doorAnimator.SetTrigger("Close");
        }
        
        // Store the floor number in a temporary variable
        targetFloorNumber = floorNumber;
        Invoke(nameof(LoadSelectedFloorWithTarget), 1.0f);
    }
    
    private void LoadSelectedFloor(int floorNumber)
    {
        // Temporarily disable buttons during transition
        if (floor1Button != null) floor1Button.GetComponent<Collider>().enabled = false;
        if (floor2Button != null) floor2Button.GetComponent<Collider>().enabled = false;
        if (floor3Button != null) floor3Button.GetComponent<Collider>().enabled = false;
        
        switch (floorNumber)
        {
            case 1:
                LoadMazeFloor();
                break;
            case 2:
                LoadHorrorFloor();
                break;
            case 3:
                LoadBossFloor();
                break;
        }
    }
    
    private void LoadSelectedFloorWithTarget()
    {
        // Temporarily disable buttons during transition
        if (floor1Button != null) floor1Button.GetComponent<Collider>().enabled = false;
        if (floor2Button != null) floor2Button.GetComponent<Collider>().enabled = false;
        if (floor3Button != null) floor3Button.GetComponent<Collider>().enabled = false;
        
        switch (targetFloorNumber)
        {
            case 1:
                LoadMazeFloor();
                break;
            case 2:
                LoadHorrorFloor();
                break;
            case 3:
                LoadBossFloor();
                break;
        }
    }
    
    private void LoadNextRoom()
    {
        // Check if player has completed all rooms
        if (GameManager.Instance.RoomsCompleted >= 3)
        {
            Debug.Log("All rooms completed! Player wins!");
            // TODO: Trigger escape sequence
            return;
        }
        
        // Otherwise, return to elevator state
        if (doorAnimator != null)
        {
            doorAnimator.SetTrigger("Open");
        }
        
        // Re-enable appropriate floor button
        UpdateFloorAvailability();
    }
    
    // Called by RoomManager when returning from a room
    private void OnEnable()
    {
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.OnRoomFinished += OnRoomCompleted;
        }
    }
    
    private void OnDisable()
    {
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.OnRoomFinished -= OnRoomCompleted;
        }
    }
    
    private void OnRoomCompleted(IRoom room, bool success)
    {
        // After any room completion, return to elevator state
        Invoke(nameof(InvokeLoadNextRoom), 0.5f); // Small delay to allow for transition
    }
    
    private void LoadMazeFloor()
    {
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.LoadSpecificRoom(RoomManager.RoomType.Maze);
        }
    }
    
    private void LoadHorrorFloor()
    {
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.LoadSpecificRoom(RoomManager.RoomType.Horror);
        }
    }
    
    private void LoadBossFloor()
    {
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.LoadSpecificRoom(RoomManager.RoomType.Boss);
        }
    }
    
    private void InvokeLoadNextRoom()
    {
        LoadNextRoom();
    }
    
    public override bool CanExit()
    {
        // Player doesn't exit the hub normally - they go to floors
        return false;
    }
}