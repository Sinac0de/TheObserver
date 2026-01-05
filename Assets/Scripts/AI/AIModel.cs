using UnityEngine;

/// <summary>
/// ScriptableObject definition for the AI Observer configuration.
/// Implements the "Living Maze Director" system that observes player behavior and adapts the maze.
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
    public float solveTimeWeight = 0.3f;

    [Tooltip("Weight of mistakes (traps, hazards triggered).")]
    public float mistakesWeight = 0.2f;

    [Tooltip("Weight of enemy encounters.")]
    public float enemyEncountersWeight = 0.2f;

    [Tooltip("Weight of movement speed (running vs walking).")]
    public float movementSpeedWeight = 0.15f;

    [Tooltip("Weight of time spent standing still.")]
    public float standingStillWeight = 0.15f;

    [Header("Expected Target Metrics")]
    [Tooltip("Expected solve time in seconds for a neutral difficulty (5 minutes for GDD).")]
    public float targetSolveTime = 300f; // 5 minutes

    [Tooltip("Expected mistakes per room at neutral difficulty.")]
    public float targetMistakes = 3f;

    [Tooltip("Expected enemy encounters at neutral difficulty.")]
    public float targetEnemyEncounters = 2f;

    [Tooltip("Expected time spent standing still in seconds.")]
    public float targetStandingStillTime = 30f;

    [Tooltip("Expected average movement speed (0-1 scale).")]
    public float targetMovementSpeed = 0.6f;

    [Header("Runtime State (Read-only in inspector)")]
    [SerializeField, Range(0f, 1f)]
    private float currentComplexity = 0.3f;

    [Header("Player Behavior Analysis")]
    [SerializeField]
    private float skillScore = 0.5f; // How effectively the player navigates and survives (0-1)

    [SerializeField]
    private float riskProfile = 0.5f; // Tendency to rush vs explore (0-1)

    [SerializeField]
    private float fearBias = 0.5f; // Chase pressure vs suspense pressure (0-1)

    [SerializeField]
    private int totalRoomsCompleted;

    [SerializeField]
    private int totalFailures;

    [SerializeField]
    private int totalDeaths;

    [SerializeField]
    private int totalTimeouts;

    [Header("Last Run Metrics")]
    [SerializeField]
    private float lastRunSolveTime;

    [SerializeField]
    private int lastRunMistakes;

    [SerializeField]
    private int lastRunEnemyEncounters;

    [SerializeField]
    private float lastRunMovementSpeed;

    [SerializeField]
    private float lastRunStandingStillTime;

    [SerializeField]
    private bool lastRunSuccess;

    [Header("Derived Output (Runtime)")]
    [SerializeField, Range(0f, 1f)]
    private float enemyAggressionFactor = 0.5f;

    [SerializeField, Range(0f, 1f)]
    private float trapIntensityFactor = 0.5f;

    [SerializeField, Range(0f, 1f)]
    private float pacingFactor = 0.5f; // 0 = calm, 1 = constant pressure

    private const string PrefKeyPrefix = "TheObserver_AI_";

    public float CurrentComplexity => currentComplexity;
    public float SkillScore => skillScore;
    public float RiskProfile => riskProfile;
    public float FearBias => fearBias;
    public int TotalRoomsCompleted => totalRoomsCompleted;
    public int TotalFailures => totalFailures;
    public int TotalDeaths => totalDeaths;
    public int TotalTimeouts => totalTimeouts;
    public float LastRunSolveTime => lastRunSolveTime;
    public int LastRunMistakes => lastRunMistakes;
    public int LastRunEnemyEncounters => lastRunEnemyEncounters;
    public float LastRunMovementSpeed => lastRunMovementSpeed;
    public float LastRunStandingStillTime => lastRunStandingStillTime;
    public bool LastRunSuccess => lastRunSuccess;

    public float EnemyAggressionFactor => enemyAggressionFactor;
    public float TrapIntensityFactor => trapIntensityFactor;
    public float PacingFactor => pacingFactor;

    public void LoadFromPrefs() {
        currentComplexity = PlayerPrefs.GetFloat(PrefKey("CurrentComplexity"), baseComplexity);
        skillScore = PlayerPrefs.GetFloat(PrefKey("SkillScore"), 0.5f);
        riskProfile = PlayerPrefs.GetFloat(PrefKey("RiskProfile"), 0.5f);
        fearBias = PlayerPrefs.GetFloat(PrefKey("FearBias"), 0.5f);
        totalRoomsCompleted = PlayerPrefs.GetInt(PrefKey("TotalRoomsCompleted"), 0);
        totalFailures = PlayerPrefs.GetInt(PrefKey("TotalFailures"), 0);
        totalDeaths = PlayerPrefs.GetInt(PrefKey("TotalDeaths"), 0);
        totalTimeouts = PlayerPrefs.GetInt(PrefKey("TotalTimeouts"), 0);
        lastRunSolveTime = PlayerPrefs.GetFloat(PrefKey("LastRunSolveTime"), 0f);
        lastRunMistakes = PlayerPrefs.GetInt(PrefKey("LastRunMistakes"), 0);
        lastRunEnemyEncounters = PlayerPrefs.GetInt(PrefKey("LastRunEnemyEncounters"), 0);
        lastRunMovementSpeed = PlayerPrefs.GetFloat(PrefKey("LastRunMovementSpeed"), 0f);
        lastRunStandingStillTime = PlayerPrefs.GetFloat(PrefKey("LastRunStandingStillTime"), 0f);
        lastRunSuccess = PlayerPrefs.GetInt(PrefKey("LastRunSuccess"), 0) == 1;
    }

    public void SaveToPrefs() {
        PlayerPrefs.SetFloat(PrefKey("CurrentComplexity"), currentComplexity);
        PlayerPrefs.SetFloat(PrefKey("SkillScore"), skillScore);
        PlayerPrefs.SetFloat(PrefKey("RiskProfile"), riskProfile);
        PlayerPrefs.SetFloat(PrefKey("FearBias"), fearBias);
        PlayerPrefs.SetInt(PrefKey("TotalRoomsCompleted"), totalRoomsCompleted);
        PlayerPrefs.SetInt(PrefKey("TotalFailures"), totalFailures);
        PlayerPrefs.SetInt(PrefKey("TotalDeaths"), totalDeaths);
        PlayerPrefs.SetInt(PrefKey("TotalTimeouts"), totalTimeouts);
        PlayerPrefs.SetFloat(PrefKey("LastRunSolveTime"), lastRunSolveTime);
        PlayerPrefs.SetInt(PrefKey("LastRunMistakes"), lastRunMistakes);
        PlayerPrefs.SetInt(PrefKey("LastRunEnemyEncounters"), lastRunEnemyEncounters);
        PlayerPrefs.SetFloat(PrefKey("LastRunMovementSpeed"), lastRunMovementSpeed);
        PlayerPrefs.SetFloat(PrefKey("LastRunStandingStillTime"), lastRunStandingStillTime);
        PlayerPrefs.SetInt(PrefKey("LastRunSuccess"), lastRunSuccess ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Call when a run is completed (either success, death, or timeout).
    /// </summary>
    public void RegisterRunResult(RunResult result, RunMetrics metrics) {
        // Store last run metrics
        lastRunSolveTime = Mathf.Max(0f, metrics.solveTimeSeconds);
        lastRunMistakes = Mathf.Max(0, metrics.mistakes);
        lastRunEnemyEncounters = Mathf.Max(0, metrics.enemyEncounters);
        lastRunMovementSpeed = Mathf.Clamp01(metrics.movementSpeed);
        lastRunStandingStillTime = Mathf.Max(0f, metrics.standingStillTime);
        lastRunSuccess = result == RunResult.Success;

        // Update counters
        if (result == RunResult.Success) {
            totalRoomsCompleted++;
        } else {
            totalFailures++;
            if (result == RunResult.Death) {
                totalDeaths++;
            } else if (result == RunResult.Timeout) {
                totalTimeouts++;
            }
        }

        // Update behavior analysis
        UpdateBehaviorAnalysis(metrics, result);

        // Calculate new complexity based on performance
        float targetComplexity = CalculateTargetComplexity(metrics, result);
        currentComplexity = Mathf.Lerp(
            currentComplexity,
            Mathf.Clamp(targetComplexity, minComplexity, maxComplexity),
            adaptationLerpSpeed
        );

        // Update derived output factors (for maze/enemy tuning)
        UpdateOutputFactors(metrics, result);

        SaveToPrefs();
    }

    private void UpdateBehaviorAnalysis(RunMetrics metrics, RunResult result) {
        // Update skill score based on success/failure and efficiency
        float performanceScore = CalculatePerformanceScore(metrics, result);
        skillScore = Mathf.Clamp01(skillScore * 0.8f + performanceScore * 0.2f); // Slow adjustment

        // Update risk profile based on movement patterns
        riskProfile = Mathf.Clamp01(metrics.movementSpeed);

        // Update fear bias based on death vs timeout patterns
        if (totalDeaths + totalTimeouts > 0) {
            fearBias = (float)totalDeaths / (totalDeaths + totalTimeouts);
        } else {
            fearBias = 0.5f; // Neutral if no failures yet
        }
    }

    private float CalculateTargetComplexity(RunMetrics metrics, RunResult result) {
        float baseAdjustment = baseComplexity;

        // If player is too successful, increase difficulty
        if (result == RunResult.Success) {
            baseAdjustment = Mathf.Min(maxComplexity, baseAdjustment + 0.1f);
        } else {
            // If player is failing, adjust based on failure type
            if (result == RunResult.Death) {
                if (metrics.enemyEncounters > targetEnemyEncounters * 1.5f) {
                    baseAdjustment += 0.05f;
                } else if (metrics.mistakes > targetMistakes * 1.5f) {
                    baseAdjustment += 0.05f;
                }
            } else if (result == RunResult.Timeout) {
                baseAdjustment += 0.05f;
            }
        }

        // Adjust based on movement patterns
        if (metrics.movementSpeed > targetMovementSpeed * 1.2f) {
            baseAdjustment += 0.05f;
        }

        if (metrics.standingStillTime > targetStandingStillTime * 1.5f) {
            baseAdjustment += 0.03f;
        }

        return Mathf.Clamp01(baseAdjustment);
    }

    private float CalculatePerformanceScore(RunMetrics metrics, RunResult result) {
        float solveRatio = targetSolveTime <= 0.1f ? 1f : Mathf.Clamp01(targetSolveTime / Mathf.Max(targetSolveTime, metrics.solveTimeSeconds));
        float mistakesRatio = targetMistakes <= 0.1f ? 1f : Mathf.Clamp01(targetMistakes / Mathf.Max(targetMistakes, metrics.mistakes));
        float enemyRatio = targetEnemyEncounters <= 0.1f ? 1f : Mathf.Clamp01(targetEnemyEncounters / Mathf.Max(targetEnemyEncounters, metrics.enemyEncounters));
        float movementRatio = Mathf.Clamp01(metrics.movementSpeed / targetMovementSpeed);
        float stillRatio = targetStandingStillTime <= 0.1f ? 1f : Mathf.Clamp01(targetStandingStillTime / Mathf.Max(targetStandingStillTime, metrics.standingStillTime));

        float performance = solveTimeWeight * solveRatio +
                            mistakesWeight * mistakesRatio +
                            enemyEncountersWeight * enemyRatio +
                            movementSpeedWeight * movementRatio +
                            standingStillWeight * stillRatio;

        if (result != RunResult.Success) {
            performance *= 0.6f; // Lower performance for failures
        }

        return Mathf.Clamp01(performance);
    }

    private void UpdateOutputFactors(RunMetrics metrics, RunResult result) {
        float c = currentComplexity;

        // Enemy aggression
        float deathRatio = (totalFailures > 0) ? (float)totalDeaths / totalFailures : 0.5f;
        float enemyPressure = Mathf.Clamp01(
            0.5f * c +
            0.3f * fearBias +
            0.2f * (metrics.enemyEncounters / Mathf.Max(1f, targetEnemyEncounters))
        );
        if (deathRatio > 0.7f) {
            enemyPressure *= 0.8f;
        }
        enemyAggressionFactor = Mathf.Clamp01(enemyPressure);

        // Trap intensity
        float mistakesRatio = metrics.mistakes / Mathf.Max(1f, targetMistakes);
        float trapBase = Mathf.Clamp01(
            0.4f * c +
            0.3f * mistakesRatio +
            0.3f * riskProfile
        );
        if (mistakesRatio > 1.5f) {
            trapBase *= 0.7f;
        }
        trapIntensityFactor = Mathf.Clamp01(trapBase);

        // Pacing factor (time pressure / quiet vs constant tension)
        float timeoutRatio = (totalFailures > 0) ? (float)totalTimeouts / totalFailures : 0f;
        float stillRatio = metrics.standingStillTime / Mathf.Max(1f, targetStandingStillTime);

        float pacing = 0.5f * c +
                       0.25f * timeoutRatio +
                       0.25f * stillRatio;

        pacingFactor = Mathf.Clamp01(pacing);
    }

    private string PrefKey(string field) => PrefKeyPrefix + field;
}

[System.Serializable]
public struct RunMetrics {
    public float solveTimeSeconds;
    public int mistakes;
    public int enemyEncounters;
    public float movementSpeed; // 0-1 scale, average movement speed
    public float standingStillTime; // Time spent standing still in seconds
}

public enum RunResult {
    Success,
    Death,
    Timeout
}
