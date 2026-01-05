using UnityEngine;
using System;
using UnityEngine.SceneManagement;

public class DeathFlowController : MonoBehaviour {
    public static DeathFlowController Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private bool logMessages = true;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Handles a room failure: updates AI, logs, and respawns player via callback.
    /// Later this will also handle black-screen + Observer message.
    /// </summary>
    public void HandleRoomFail(
        RoomType roomType,
        RoomMetrics metrics,
        Action onAfterAIUpdatedAndBeforeRespawn,
        Action onRespawn
    ) {
        // 1) Update AIModel
        if (GameManager.Instance != null && GameManager.Instance.AIModel != null) {
            var ai = GameManager.Instance.AIModel;
            ai.RegisterRoomResult(
                success: false,
                solveTimeSeconds: metrics.solveTimeSeconds,
                mistakes: metrics.mistakes,
                detections: metrics.detections
            );
        }

        // 2) Register global death stats
        if (GameManager.Instance != null) {
            GameManager.Instance.RegisterDeath();
        }

        if (logMessages) {
            Debug.Log($"[DeathFlow] Room fail recorded. Type={roomType}, time={metrics.solveTimeSeconds}, mistakes={metrics.mistakes}, detections={metrics.detections}");
        }

        // 3) Play Observer message before respawn
        if (ObserverManager.Instance != null) {
            // For now, we'll assume it's always a death message since timeout is handled separately
            // In a full implementation, we'd pass information about the type of failure
            ObserverManager.Instance.PlayDeathMessage();
        }
        
        // 4) Optional hook: e.g. choose additional Observer message, etc.
        onAfterAIUpdatedAndBeforeRespawn?.Invoke();

        // 5) Respawn callback (e.g. put player back into elevator)
        onRespawn?.Invoke();
    }
}
