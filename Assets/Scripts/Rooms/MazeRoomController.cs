using UnityEngine;

public class MazeRoomController : BaseRoomController
{
    [Header("Maze Configuration")]
    public MazeGenerator mazeGenerator;
    public float regenerationDelay = 5.0f;
    public int regenerationLimit = 3; // Max times to regenerate after deaths
    
    [Header("Maze Difficulty")]
    public float wallDensityMultiplier = 1.0f;
    public float enemySpawnMultiplier = 1.0f;
    public float trapSpawnMultiplier = 1.0f;
    
    private int regenerationCount = 0;
    private bool exitReached = false;
    
    protected override void ApplyAIDifficulty()
    {
        if (aiModel == null)
        {
            Debug.LogWarning("MazeRoomController: AIModel not assigned in Initialize.");
            return;
        }

        float complexity = aiModel.CurrentComplexity;
        Debug.Log($"Maze room adapting to complexity: {complexity}");
        
        // Adjust maze parameters based on AI complexity
        wallDensityMultiplier = Mathf.Lerp(0.7f, 1.5f, complexity);
        enemySpawnMultiplier = Mathf.Lerp(0.5f, 2.0f, complexity);
        trapSpawnMultiplier = Mathf.Lerp(0.5f, 2.0f, complexity);
        
        // Generate the maze with current difficulty
        GenerateMaze();
    }

    private void GenerateMaze()
    {
        if (mazeGenerator == null)
        {
            Debug.LogError("MazeGenerator not assigned!");
            return;
        }
        
        // Modify maze parameters based on difficulty multipliers
        float baseEnemyChance = 0.1f * enemySpawnMultiplier;
        float baseTrapChance = 0.15f * trapSpawnMultiplier;
        
        // Apply difficulty adjustments
        mazeGenerator.enemySpawnChance = Mathf.Clamp(baseEnemyChance, 0.05f, 0.5f);
        mazeGenerator.trapSpawnChance = Mathf.Clamp(baseTrapChance, 0.05f, 0.4f);
        
        mazeGenerator.GenerateMaze();
        Debug.Log("Maze generated with current difficulty settings");
    }

    public override void OnPlayerEnter()
    {
        base.OnPlayerEnter();
        
        if (mazeGenerator != null)
        {
            // Position player at maze start
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                player.transform.position = mazeGenerator.GetStartPosition();
            }
        }
    }

    public void CompleteMaze()
    {
        if (exitReached) return;
        
        exitReached = true;
        Debug.Log("Player reached maze exit!");
        
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.CompleteCurrentRoom(true);
        }
    }

    public void RegenerateMazeAfterDeath()
    {
        if (regenerationCount >= regenerationLimit)
        {
            // If max regenerations reached, return to elevator
            Debug.Log("Max maze regenerations reached. Returning to elevator.");
            if (RoomManager.Instance != null)
            {
                // Complete the room with failure to return to elevator
                RoomManager.Instance.CompleteCurrentRoom(false);
            }
            return;
        }
            
        regenerationCount++; 
        Debug.Log($"Regenerating maze after death #{regenerationCount}");
            
        // Increase difficulty based on deaths
        float deathPenalty = regenerationCount * 0.1f;
        aiModel.RegisterRoomResult(false, GetSolveTime(), Mistakes + 1, Detections); // Register death as mistake
            
        Invoke(nameof(InvokeGenerateMaze), regenerationDelay);
    }

    public override bool CanExit()
    {
        return exitReached;
    }
    
    private void InvokeGenerateMaze()
    {
        GenerateMaze();
    }
    
    // Method for external systems (like MazeExitElevator) to trigger completion
    public void TriggerMazeCompletion()
    {
        if (!exitReached)
        {
            CompleteMaze();
        }
    }


}