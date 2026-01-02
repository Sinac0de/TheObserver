using UnityEngine;

public class MazeExitElevator : Interactable
{
    [Header("Elevator Settings")]
    public string elevatorPrompt = "Enter Elevator";
    public Animator doorAnimator;
    public float doorCloseDelay = 1.0f;
    
    private bool canEnter = false;

    private void Start()
    {
        prompt = elevatorPrompt;
    }

    public override void Interact(GameObject interactor)
    {
        if (!canEnter) return;
        
        Debug.Log("Entering elevator to next floor...");
        
        // Animate door closing
        if (doorAnimator != null)
        {
            doorAnimator.SetTrigger("Close");
        }
        
        // Complete the maze room to progress to next floor
        MazeRoomController mazeRoom = FindObjectOfType<MazeRoomController>();
        if (mazeRoom != null)
        {
            mazeRoom.CompleteMaze();
        }
    }

    public void EnableElevator()
    {
        canEnter = true;
        Debug.Log("Maze exit elevator activated!");
    }

    public void DisableElevator()
    {
        canEnter = false;
    }
}