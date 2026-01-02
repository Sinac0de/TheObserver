using UnityEngine;

/// <summary>
/// Global game coordinator for The Observer.
/// Holds references to core systems and the AIObserverModel.
/// </summary>
public class GameManager : MonoBehaviour {
    public static GameManager Instance { get; private set; }

    [Header("AI")]
    [SerializeField] private AIModel aiModel;          // Assign AIObserverModel in Inspector

    [Header("Progress")]
    [SerializeField] private int roomsCompleted;
    [SerializeField] private int deaths;
    [SerializeField] private int currentRunScore;

    public AIModel AIModel => aiModel;
    public int RoomsCompleted => roomsCompleted;
    public int Deaths => deaths;
    public int CurrentRunScore => currentRunScore;

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
    }

    /// <summary>
    /// Called by RoomManager when a room is fully completed (success).
    /// </summary>
    public void RegisterRoomCompleted(int scoreGain = 100) {
        roomsCompleted++;
        currentRunScore += scoreGain;
    }

    /// <summary>
    /// Called by RoomManager when the player dies/fails.
    /// </summary>
    public void RegisterDeath(int scorePenalty = 0) {
        deaths++;
        currentRunScore = Mathf.Max(0, currentRunScore - scorePenalty);
    }

    /// <summary>
    /// Call this when starting a fresh run from the Lab Hub.
    /// </summary>
    public void ResetRun() {
        roomsCompleted = 0;
        currentRunScore = 0;
    }
}
