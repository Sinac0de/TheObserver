using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Controls a single maze run:
/// - Starts/stops timer
/// - Tracks mistakes
/// - Notifies AIModel about run result
/// - Handles success / failure / timeout flow
/// </summary>
public class MazeRoomController : MonoBehaviour {
    public static MazeRoomController Instance {  get; private set; }

    [Header("References")]
    [SerializeField] private MazeGenerator mazeGenerator;
    [SerializeField] private FloorElevatorController elevator;
    [SerializeField] private Transform playerTransform;

    [Header("Timer")]
    [SerializeField] private float baseTimeLimit = 300f; // 5 minutes
    private bool isRunning;

    [Header("Metrics")]
    [SerializeField] private int mistakes;
    private float roomStartTime;

    [Header("Observer Messages")]
    [SerializeField] private string[] observerDeathMessages;
    [SerializeField] private string[] observerTimeoutMessages;

    [Header("Events")]
    public UnityEvent<float> onTimeUpdate;
    public UnityEvent onTimeOut;
    public UnityEvent onPlayerDeath;

    private AIModel aiModel;

    private void Awake() {
        if (GameManager.Instance != null) {
            aiModel = GameManager.Instance.AIModel;
        }

        if (Instance != null) {
            Destroy(Instance);
        }

        Instance = this;

        SetupTimerEvents();
    }

    private void SetupTimerEvents() {
        if (TimerManager.Instance != null) {
            TimerManager.Instance.onTimeUpdate.AddListener(time => onTimeUpdate?.Invoke(time));
            TimerManager.Instance.onTimeWarning.AddListener(OnTimeWarning);
            TimerManager.Instance.onTimeCritical.AddListener(OnTimeCritical);
            TimerManager.Instance.onTimeOut.AddListener(OnTimeOut);
        }
    }

    private void OnTimeWarning() {
        Debug.Log("[MazeRoom] Time warning triggered!");
        // TODO: visual/audio cue for warning
    }

    private void OnTimeCritical() {
        Debug.Log("[MazeRoom] Time critical!");
        // TODO: visual/audio cue for critical time
    }

    private void Start() {
        isRunning = false;

        ApplyAIDifficulty();
        if (mazeGenerator != null) {
            mazeGenerator.GenerateMaze();
        }
    }

    /// <summary>
    /// Called by TimerManager when time reaches zero.
    /// </summary>
    private void OnTimeOut() {
        if (!isRunning) return;

        isRunning = false;
        float solveTime = Time.time - roomStartTime;

        RunMetrics runMetrics = BuildRunMetrics(solveTime, mistakes);

        Debug.Log($"[MazeRoom] TIMEOUT in {solveTime:F1}s, mistakes: {mistakes}");

        GameManager.Instance?.AIModel?.RegisterRunResult(RunResult.Timeout, runMetrics);
        GameManager.Instance?.RegisterDeath(); // if you treat timeout as a failed run

        // Observer line for timeout (if any)
        if (ObserverManager.Instance != null) {
            ObserverManager.Instance.PlayTimeoutMessage();
        }

        // Example behavior: mutate/regenerate maze in-place, then restart timer
        if (mazeGenerator != null) {
            mazeGenerator.GenerateMaze();
        }

        float timeLimit = GetTimeLimitFromAI();
        TimerManager.Instance.SetTimeLimit(timeLimit);
        TimerManager.Instance.StartTimer();
        isRunning = true;
        roomStartTime = Time.time;
        mistakes = 0;
    }

    /// <summary>
    /// Called when the player exits the elevator and the run officially begins.
    /// </summary>
    public void StartRoom() {
        if (isRunning) return;

        mistakes = 0;
        roomStartTime = Time.time;

        float timeLimit = GetTimeLimitFromAI();

        TimerManager.Instance.SetTimeLimit(timeLimit);
        TimerManager.Instance.StartTimer();
        isRunning = true;

        Debug.Log("[MazeRoom] Room started with time limit: " + timeLimit);
    }

    private float GetTimeLimitFromAI() {
        float timeLimit = baseTimeLimit;
        if (aiModel != null) {
            float c = aiModel.CurrentComplexity; // 0..1
            // Slightly more time for low complexity, slightly less for high
            timeLimit = Mathf.Lerp(baseTimeLimit * 1.2f, baseTimeLimit * 0.8f, c);
        }
        return timeLimit;
    }

    private void ApplyAIDifficulty() {
        if (aiModel == null || mazeGenerator == null) return;

        mazeGenerator.ApplyAIDifficulty();
    }

    /// <summary>
    /// Called when the player reaches the maze exit.
    /// </summary>
    public void Success() {
        if (!isRunning) return;

        TimerManager.Instance.StopTimer();
        isRunning = false;
        float solveTime = Time.time - roomStartTime;

        RunMetrics runMetrics = BuildRunMetrics(solveTime, mistakes);
        GameManager.Instance?.AIModel?.RegisterRunResult(RunResult.Success, runMetrics);
        GameManager.Instance?.RegisterRoomCompleted();

        Debug.Log($"[MazeRoom] SUCCESS in {solveTime:F1}s, mistakes: {mistakes}");

        // TODO: trigger final escape sequence / ending
    }

    /// <summary>
    /// Called when the player dies (enemy or lethal trap).
    /// </summary>
    public void Fail() {
        if (!isRunning) return;

        TimerManager.Instance.StopTimer();
        isRunning = false;
        float solveTime = Time.time - roomStartTime;

        RunMetrics runMetrics = BuildRunMetrics(solveTime, mistakes);

        Debug.Log($"[MazeRoom] FAIL in {solveTime:F1}s, mistakes: {mistakes}");

        GameManager.Instance?.AIModel?.RegisterRunResult(RunResult.Death, runMetrics);
        GameManager.Instance?.RegisterDeath();

        // Use DeathFlowController if present, otherwise fallback to simple respawn
        if (DeathFlowController.Instance != null && elevator != null && playerTransform != null) {
  
            DeathFlowController.Instance.HandleRoomFail(
                runMetrics,
                onAfterAIUpdatedAndBeforeRespawn: () => {
                    ObserverManager.Instance?.PlayDeathMessage();
                },
                onRespawn: () => {
                    elevator.RespawnPlayerInElevator(playerTransform);
                    TimerManager.Instance.ResetTimer();
                }
            );
        } else {
            if (elevator != null && playerTransform != null) {
                elevator.RespawnPlayerInElevator(playerTransform);
            }
        }

    }

    /// <summary>
    /// Called by hazards when the player triggers them (trap, non-lethal scare etc.).
    /// </summary>
    public void RegisterMistake() {
        mistakes++;
    }

    /// <summary>
    /// Called by PlayerHealth when the player actually dies.
    /// </summary>
    public void RegisterPlayerDeath() {
        onPlayerDeath?.Invoke();
        Fail();
    }

    /// <summary>
    /// Collects RunMetrics from player components to feed AIModel.
    /// </summary>
    private RunMetrics BuildRunMetrics(float solveTime, int mistakesCount) {
        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
        PlayerController playerController = FindObjectOfType<PlayerController>();

        return new RunMetrics {
            solveTimeSeconds = solveTime,
            mistakes = mistakesCount,
            enemyEncounters = playerHealth?.EnemyEncounters ?? 0,
            movementSpeed = playerController?.MovementSpeedTracker ?? 0.5f,
            standingStillTime = playerController?.StandingStillTime ?? 0f
        };
    }


    public bool GetIsRunning() {
        return isRunning;
    }
}
