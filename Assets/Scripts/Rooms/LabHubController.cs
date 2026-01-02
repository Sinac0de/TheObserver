using UnityEngine;

public class LabHubController : BaseRoomController
{
    [Header("Lab Hub Settings")]
    public ConsoleInteractable elevatorDoorConsole;
    public Animator elevatorDoorAnimator;
    public float initialAwakeningDelay = 3.0f;
    public string[] aiIntroLines;
    
    [Header("Elevator Settings")]
    public bool elevatorDoorInitiallyClosed = true;
    
    private bool playerAwakened = false;
    private bool elevatorReady = false;

    protected override void ApplyAIDifficulty()
    {
        // Lab hub doesn't adapt difficulty - it's the staging area
        Debug.Log("Lab Hub loaded. Player awakening sequence initiated.");
        
        if (!playerAwakened)
        {
            Invoke(nameof(StartAwakeningSequence), initialAwakeningDelay);
        }
    }

    private void StartAwakeningSequence()
    {
        playerAwakened = true;
        Debug.Log("Player awakening sequence started.");
        
        // Play AI introduction
        PlayAIBriefing();
        
        // After briefing, open elevator
        Invoke(nameof(OpenElevatorForAccess), 2.0f);
    }

    private void OpenElevatorForAccess()
    {
        elevatorReady = true;
        
        if (elevatorDoorAnimator != null)
        {
            elevatorDoorAnimator.SetTrigger("Open");
        }
        
        if (elevatorDoorConsole != null)
        {
            elevatorDoorConsole.GetComponent<Collider>().enabled = true;
            Debug.Log("Elevator door opened. Player can now access elevator.");
        }
    }

    private void PlayAIBriefing()
    {
        if (aiIntroLines.Length > 0)
        {
            string intro = aiIntroLines[Random.Range(0, aiIntroLines.Length)];
            Debug.Log($"AI BRIEFING: {intro}");
            // TODO: Play audio clip or show UI text
        }
        else
        {
            Debug.Log("AI BRIEFING: Welcome. I observe. Survive three trials. I learn from your failures.");
        }
    }

    public void EnterElevator()
    {
        if (!elevatorReady)
        {
            Debug.Log("Elevator is not ready yet.");
            return;
        }
        
        Debug.Log("Player entering elevator. Loading elevator room...");
        
        if (RoomManager.Instance != null)
        {
            // For now, we'll load the elevator as a special room type
            // In the actual implementation, this might be handled differently
            RoomManager.Instance.LoadNextRoom(); // This will load the elevator room
        }
    }

    public override bool CanExit()
    {
        // Player doesn't exit the lab hub normally - they enter the elevator
        return false;
    }
}