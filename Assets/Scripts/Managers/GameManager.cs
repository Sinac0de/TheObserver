using UnityEngine;

/// <summary>
/// Global game coordinator for THE MAZE.
/// Holds references to core systems and the AIObserverModel.
/// </summary>
public class GameManager : MonoBehaviour {
    public static GameManager Instance { get; private set; }

    [Header("AI")]
    [SerializeField] private AIModel aiModel; // Assign AIObserverModel asset in Inspector

    [Header("Progress")]
    [SerializeField] private int roomsCompleted;
    [SerializeField] private int deaths;
    [SerializeField] private int currentRunScore;

    // Properties to allow external access for save system
    public int RoomsCompleted
    {
        get => roomsCompleted;
        set => roomsCompleted = value;
    }
    
    public int Deaths
    {
        get => deaths;
        set => deaths = value;
    }
    
    public int CurrentRunScore
    {
        get => currentRunScore;
        set => currentRunScore = value;
    }

    public AIModel AIModel => aiModel;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (aiModel != null) {
            aiModel.LoadFromPrefs();
        } else {
            Debug.LogWarning("GameManager: AIModel reference is missing. Please assign AIObserverModel in the inspector.");
        }
        
        // Subscribe to menu events to handle game state properly
        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.OnPauseStateChanged += OnPauseStateChanged;
        }
    }
    
    private void OnPauseStateChanged(bool isPaused)
    {
        // GameManager doesn't need special handling for pause
        // But we can add any game-specific pause logic here if needed
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.OnPauseStateChanged -= OnPauseStateChanged;
        }
    }

    /// <summary>
    /// Called when a maze run is successfully completed.
    /// </summary>
    public void RegisterRoomCompleted(int scoreGain = 100) {
        roomsCompleted++;
        currentRunScore += scoreGain;

        Debug.Log("PLAYER WINS! All 3 maze runs completed.");
        // TODO: Trigger final escape sequence / ending.
    }

    /// <summary>
    /// Called when the player dies or fails a run.
    /// </summary>
    public void RegisterDeath(int scorePenalty = 0) {
        deaths++;
        currentRunScore = Mathf.Max(0, currentRunScore - scorePenalty);
    }

    /// <summary>
    /// Call this when starting a fresh run.
    /// </summary>
    public void ResetRun() {
        roomsCompleted = 0;
        currentRunScore = 0;
    }
}
