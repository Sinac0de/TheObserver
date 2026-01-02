using UnityEngine;

public class ExitDoor : Interactable
{
    [Header("Exit Door Settings")]
    public string exitPrompt = "Exit Room";
    public bool requireRoomCompletion = true;
    
    private bool canExit = false;
    private IRoom currentRoom;

    private void Awake()
    {
        prompt = exitPrompt;
        currentRoom = GetComponentInParent<IRoom>();
    }

    private void Start()
    {
        if (!requireRoomCompletion)
        {
            canExit = true;
        }
    }

    public override void Interact(GameObject interactor)
    {
        if (!canExit) return;
        
        Debug.Log("Exiting room successfully!");
        
        if (currentRoom != null && RoomManager.Instance != null)
        {
            RoomManager.Instance.CompleteCurrentRoom(true);
        }
    }

    public void EnableExit()
    {
        canExit = true;
        Debug.Log("Exit door now available!");
    }

    private void OnTriggerEnter(Collider other)
    {
        // Legacy trigger support
        if (!other.CompareTag("Player") || !canExit) return;
        
        if (currentRoom != null && RoomManager.Instance != null)
        {
            RoomManager.Instance.CompleteCurrentRoom(true);
        }
    }
}
