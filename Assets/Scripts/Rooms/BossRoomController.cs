using UnityEngine;

public class BossRoomController : BaseRoomController
{
    [Header("Boss Settings")]
    public GameObject[] bossPrefabs;
    public Transform[] spawnPoints;
    public float spawnDelay = 2.0f;
    
    [Header("Hack Terminal")]
    public HackTerminalInteractable hackTerminal;
    
    [Header("Win Conditions")]
    public int bossesToDefeat = 3;
    private int bossesDefeated = 0;
    private bool isWon = false;

    protected override void ApplyAIDifficulty()
    {
        if (aiModel == null)
        {
            Debug.LogWarning("BossRoomController: AIModel not assigned in Initialize.");
            return;
        }

        float complexity = aiModel.CurrentComplexity;
        Debug.Log($"Boss room adapting to complexity: {complexity}");

        // TODO: Spawn different boss variants based on AI weights
        // - Low complexity: Basic drones
        // - Medium complexity: Girl + drones hybrid
        // - High complexity: Full mimic player behavior
        
        Invoke(nameof(SpawnInitialBosses), spawnDelay);
    }

    private void SpawnInitialBosses()
    {
        if (bossPrefabs.Length > 0 && spawnPoints.Length > 0)
        {
            // Spawn based on AI complexity
            int spawnCount = Mathf.Clamp(
                Mathf.RoundToInt(Mathf.Lerp(1, 3, aiModel.CurrentComplexity)), 
                1, 
                spawnPoints.Length
            );
            
            for (int i = 0; i < spawnCount && i < spawnPoints.Length; i++)
            {
                if (bossPrefabs[0] != null && spawnPoints[i] != null)
                {
                    Instantiate(bossPrefabs[0], spawnPoints[i].position, spawnPoints[i].rotation);
                }
            }
        }
    }

    public void OnBossDefeated()
    {
        bossesDefeated++;
        Debug.Log($"Boss defeated! {bossesDefeated}/{bossesToDefeat}");
        
        if (bossesDefeated >= bossesToDefeat && !isWon)
        {
            WinRoom();
        }
    }

    private void WinRoom()
    {
        isWon = true;
        Debug.Log("Boss room won! Returning to lab...");
        
        // TODO: Play victory sequence
        Invoke(nameof(ReturnToLab), 2.0f);
    }

    private void ReturnToLab()
    {
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.CompleteCurrentRoom(true);
        }
    }

    public override bool CanExit()
    {
        return isWon;
    }
}