using UnityEngine;

/// <summary>
/// ScriptableObject definition for the AI Observer configuration.
/// Runtime values are loaded/saved via PlayerPrefs, not persisted in the asset itself.
/// </summary>
[CreateAssetMenu(fileName = "AIObserverModel", menuName = "TheObserver/AI Observer Model")]
public class AIModel : ScriptableObject {
    [Header("Base Difficulty Settings")]
    [Range(0f, 1f)]
    public float baseComplexity = 0.3f; // Starting complexity

    [Range(0f, 1f)]
    public float minComplexity = 0.1f;

    [Range(0f, 1f)]
    public float maxComplexity = 1.0f;

    [Tooltip("How aggressively the AI adapts to player performance.")]
    [Range(0.01f, 1f)]
    public float adaptationLerpSpeed = 0.2f;

    [Header("Performance Weights")]
    [Tooltip("Weight of solve time vs. expected time.")]
    public float solveTimeWeight = 0.5f;

    [Tooltip("Weight of mistakes (traps, hazards triggered).")]
    public float mistakesWeight = 0.3f;

    [Tooltip("Weight of detection counts (e.g., cameras, lasers).")]
    public float detectionWeight = 0.2f;

    [Header("Expected Target Metrics")]
    [Tooltip("Expected solve time in seconds for a neutral difficulty.")]
    public float targetSolveTime = 60f;

    [Tooltip("Expected mistakes per room at neutral difficulty.")]
    public float targetMistakes = 2f;

    [Tooltip("Expected detection count at neutral difficulty.")]
    public float targetDetections = 1f;

    [Header("Runtime State (Read-only in inspector)")]
    [SerializeField, Range(0f, 1f)]
    private float currentComplexity = 0.3f;

    [SerializeField]
    private int totalRoomsCompleted;

    [SerializeField]
    private int totalFailures;

    [SerializeField]
    private float lastRoomSolveTime;

    [SerializeField]
    private int lastRoomMistakes;

    [SerializeField]
    private int lastRoomDetections;

    private const string PrefKeyPrefix = "TheObserver_AI_";

    public float CurrentComplexity => currentComplexity;
    public int TotalRoomsCompleted => totalRoomsCompleted;
    public int TotalFailures => totalFailures;
    public float LastRoomSolveTime => lastRoomSolveTime;
    public int LastRoomMistakes => lastRoomMistakes;
    public int LastRoomDetections => lastRoomDetections;

    public void LoadFromPrefs() {
        currentComplexity = PlayerPrefs.GetFloat(PrefKey("CurrentComplexity"), baseComplexity);
        totalRoomsCompleted = PlayerPrefs.GetInt(PrefKey("TotalRoomsCompleted"), 0);
        totalFailures = PlayerPrefs.GetInt(PrefKey("TotalFailures"), 0);
        lastRoomSolveTime = PlayerPrefs.GetFloat(PrefKey("LastRoomSolveTime"), 0f);
        lastRoomMistakes = PlayerPrefs.GetInt(PrefKey("LastRoomMistakes"), 0);
        lastRoomDetections = PlayerPrefs.GetInt(PrefKey("LastRoomDetections"), 0);
    }

    public void SaveToPrefs() {
        PlayerPrefs.SetFloat(PrefKey("CurrentComplexity"), currentComplexity);
        PlayerPrefs.SetInt(PrefKey("TotalRoomsCompleted"), totalRoomsCompleted);
        PlayerPrefs.SetInt(PrefKey("TotalFailures"), totalFailures);
        PlayerPrefs.SetFloat(PrefKey("LastRoomSolveTime"), lastRoomSolveTime);
        PlayerPrefs.SetInt(PrefKey("LastRoomMistakes"), lastRoomMistakes);
        PlayerPrefs.SetInt(PrefKey("LastRoomDetections"), lastRoomDetections);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Call when a room is completed (either success or failure).
    /// </summary>
    public void RegisterRoomResult(bool success, float solveTimeSeconds, int mistakes, int detections) {
        lastRoomSolveTime = Mathf.Max(0f, solveTimeSeconds);
        lastRoomMistakes = Mathf.Max(0, mistakes);
        lastRoomDetections = Mathf.Max(0, detections);

        if (success) {
            totalRoomsCompleted++;
        } else {
            totalFailures++;
        }

        float normalizedPerformance = ComputePerformanceScore(success, lastRoomSolveTime, lastRoomMistakes, lastRoomDetections);
        float targetComplexity = Mathf.Clamp01(baseComplexity + (normalizedPerformance - 0.5f));

        currentComplexity = Mathf.Lerp(currentComplexity, Mathf.Clamp(targetComplexity, minComplexity, maxComplexity), adaptationLerpSpeed);

        SaveToPrefs();
    }

    private float ComputePerformanceScore(bool success, float solveTime, int mistakes, int detections) {
        float solveRatio = targetSolveTime <= 0.1f ? 1f : Mathf.Clamp01(targetSolveTime / Mathf.Max(targetSolveTime, solveTime));
        float mistakesRatio = targetMistakes <= 0.1f ? 1f : Mathf.Clamp01(targetMistakes / Mathf.Max(targetMistakes, mistakes));
        float detectionsRatio = targetDetections <= 0.1f ? 1f : Mathf.Clamp01(targetDetections / Mathf.Max(targetDetections, detections));

        float performance = solveTimeWeight * solveRatio +
                            mistakesWeight * mistakesRatio +
                            detectionWeight * detectionsRatio;

        if (!success) {
            performance *= 0.6f;
        }

        return Mathf.Clamp01(performance);
    }

    private string PrefKey(string field) => PrefKeyPrefix + field;
}
