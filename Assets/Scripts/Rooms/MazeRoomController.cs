using UnityEngine;

public class MazeRoomController : MonoBehaviour {
    [Header("References")]
    [SerializeField] private MazeGenerator mazeGenerator;
    [SerializeField] private FloorElevatorController elevator;   // entry elevator for this floor
    [SerializeField] private Transform playerTransform;

    [Header("Timer")]
    [SerializeField] private float baseTimeLimit = 120f;
    private float remainingTime;
    private bool isRunning;

    [Header("Metrics")]
    [SerializeField] private int mistakes;
    private float roomStartTime;

    private AIModel aiModel;

    private void Awake() {
        if (GameManager.Instance != null) {
            aiModel = GameManager.Instance.AIModel;
        }
    }

    private void Start() {
        // Room is idle; player is in the elevator
        isRunning = false;
        remainingTime = 0f;

        // Generate the maze ONCE when the scene loads, using current AI difficulty
        ApplyAIDifficulty();
        if (mazeGenerator != null) {
            mazeGenerator.GenerateMaze();
        }
    }

    private void Update() {
        if (!isRunning) return;

        remainingTime -= Time.deltaTime;
        if (remainingTime <= 0f) {
            remainingTime = 0f;
            Fail();
        }
    }

    /// <summary>
    /// Called by FloorElevatorController when the player exits the elevator.
    /// Starts the timer and metrics; does NOT regenerate the maze.
    /// </summary>
    public void StartRoom() {
        if (isRunning) return;

        mistakes = 0;
        roomStartTime = Time.time;

        float timeLimit = baseTimeLimit;
        if (aiModel != null) {
            float c = aiModel.CurrentComplexity; // 0..1
            timeLimit = Mathf.Lerp(baseTimeLimit * 1.2f, baseTimeLimit * 0.8f, c);
        }

        remainingTime = timeLimit;
        isRunning = true;

        Debug.Log("[MazeRoom] Room started with time limit: " + timeLimit);
    }

    private void ApplyAIDifficulty() {
        if (aiModel == null || mazeGenerator == null) return;

        float c = aiModel.CurrentComplexity; // 0..1

        // Example mapping from complexity to hazard density
        mazeGenerator.enemyDensityOnPath = Mathf.Lerp(0.02f, 0.08f, c);
        mazeGenerator.trapDensityOnPath = Mathf.Lerp(0.03f, 0.10f, c);
    }

    /// <summary>
    /// Called by the exit elevator when the player reaches it.
    /// </summary>
    public void Success() {
        if (!isRunning) return;

        isRunning = false;
        float solveTime = Time.time - roomStartTime;

        RoomMetrics metrics = new RoomMetrics {
            roomType = RoomType.Maze,
            solveTimeSeconds = solveTime,
            mistakes = mistakes,
            detections = 0
        };

        if (aiModel != null) {
            aiModel.RegisterRoomResult(
                success: true,
                solveTimeSeconds: metrics.solveTimeSeconds,
                mistakes: metrics.mistakes,
                detections: metrics.detections
            );
        }

        if (GameManager.Instance != null) {
            GameManager.Instance.RegisterRoomCompleted();
        }

        Debug.Log("[MazeRoom] SUCCESS in " + solveTime + "s, mistakes: " + mistakes);

        // TODO: trigger transition to the next floor / inter-floor elevator
    }

    public void Fail() {
        if (!isRunning) return;

        isRunning = false;
        float solveTime = Time.time - roomStartTime;

        RoomMetrics metrics = new RoomMetrics {
            roomType = RoomType.Maze,
            solveTimeSeconds = solveTime,
            mistakes = mistakes,
            detections = 0
        };

        Debug.Log("[MazeRoom] FAIL in " + solveTime + "s, mistakes: " + mistakes);

        // Use DeathFlowController to handle AI + respawn logic
        if (DeathFlowController.Instance != null && elevator != null && playerTransform != null) {
            DeathFlowController.Instance.HandleRoomFail(
                RoomType.Maze,
                metrics,
                onAfterAIUpdatedAndBeforeRespawn: () => {
                    // Here later you will choose and queue an Observer message
                    // and eventually trigger black-screen UI
                },
                onRespawn: () => {
                    elevator.RespawnPlayerInElevator(playerTransform);
                }
            );
        } else {
            // Fallback: just respawn if DeathFlow is missing
            if (GameManager.Instance != null) {
                GameManager.Instance.RegisterDeath();
            }
            if (GameManager.Instance != null && GameManager.Instance.AIModel != null) {
                GameManager.Instance.AIModel.RegisterRoomResult(
                    success: false,
                    solveTimeSeconds: metrics.solveTimeSeconds,
                    mistakes: metrics.mistakes,
                    detections: metrics.detections
                );
            }

            if (elevator != null && playerTransform != null) {
                elevator.RespawnPlayerInElevator(playerTransform);
            }
        }
    }


    /// <summary>
    /// Called by hazards (enemies, traps) when player triggers them.
    /// </summary>
    public void RegisterMistake() {
        mistakes++;
    }
}
