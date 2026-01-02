using UnityEngine;

public class LabHubController : BaseRoomController
{
    [Header("Lab Hub Settings")]
    public ConsoleInteractable console;
    public float readyDelay = 1.0f;
    
    [Header("AI Feedback")]
    public string[] aiFeedbackLines;
    
    protected override void ApplyAIDifficulty()
    {
        // Lab hub doesn't adapt difficulty - it's the staging area
        Debug.Log("Lab Hub loaded. Welcome to The Observer facility.");
        
        if (console != null)
        {
            Invoke(nameof(EnableConsole), readyDelay);
        }
        
        // Play AI feedback based on performance
        PlayAIFeedback();
    }

    private void EnableConsole()
    {
        if (console != null)
        {
            console.ResetConsole();
            Debug.Log("Lab console ready. Press E to start next trial.");
        }
    }

    private void PlayAIFeedback()
    {
        if (aiModel == null || aiFeedbackLines.Length == 0) return;
        
        string feedback = "";
        float complexity = aiModel.CurrentComplexity;
        
        if (aiModel.TotalFailures > aiModel.TotalRoomsCompleted)
        {
            // Player struggling
            feedback = aiFeedbackLines[Mathf.Min(0, aiFeedbackLines.Length - 1)];
        }
        else if (complexity > 0.7f)
        {
            // High difficulty
            feedback = aiFeedbackLines[Mathf.Min(2, aiFeedbackLines.Length - 1)];
        }
        else
        {
            // Neutral/adapting
            feedback = aiFeedbackLines[Mathf.Min(1, aiFeedbackLines.Length - 1)];
        }
        
        Debug.Log($"AI FEEDBACK: {feedback}");
        // TODO: Play audio clip or show UI text
    }

    public override bool CanExit()
    {
        // Lab hub is exited via console interaction, not automatic
        return false;
    }
}