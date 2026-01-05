using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages the countdown timer for the maze challenge.
/// Handles time limits, warnings, and timeout events.
/// </summary>
public class TimerManager : MonoBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private float initialTimeLimit = 300f; // 5 minutes default
    [SerializeField] private float warningThreshold = 60f; // 1 minute warning
    [SerializeField] private float criticalThreshold = 10f; // 10 seconds critical
    
    [Header("Events")]
    public UnityEvent onTimerStart;
    public UnityEvent<float> onTimeUpdate; // Time remaining
    public UnityEvent onTimeWarning; // When time reaches warning threshold
    public UnityEvent onTimeCritical; // When time reaches critical threshold
    public UnityEvent onTimeOut; // When timer reaches zero

    private float remainingTime;
    private bool isRunning;
    private bool isWarningTriggered;
    private bool isCriticalTriggered;

    public float RemainingTime => remainingTime;
    public bool IsRunning => isRunning;
    public bool IsTimeOut => remainingTime <= 0f;

    private void Start()
    {
        ResetTimer();
    }

    private void Update()
    {
        if (!isRunning) return;
        
        remainingTime -= Time.deltaTime;
        
        // Trigger warning events
        if (!isWarningTriggered && remainingTime <= warningThreshold)
        {
            isWarningTriggered = true;
            onTimeWarning?.Invoke();
        }
        
        if (!isCriticalTriggered && remainingTime <= criticalThreshold)
        {
            isCriticalTriggered = true;
            onTimeCritical?.Invoke();
        }
        
        onTimeUpdate?.Invoke(remainingTime);
        
        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            StopTimer();
            onTimeOut?.Invoke();
        }
    }

    /// <summary>
    /// Starts the timer with the current time limit.
    /// </summary>
    public void StartTimer()
    {
        if (isRunning) return;
        
        isRunning = true;
        isWarningTriggered = false;
        isCriticalTriggered = false;
        onTimerStart?.Invoke();
    }

    /// <summary>
    /// Stops the timer without resetting it.
    /// </summary>
    public void StopTimer()
    {
        isRunning = false;
    }

    /// <summary>
    /// Resets the timer to the initial time limit and stops it.
    /// </summary>
    public void ResetTimer()
    {
        remainingTime = initialTimeLimit;
        StopTimer();
        isWarningTriggered = false;
        isCriticalTriggered = false;
    }

    /// <summary>
    /// Sets a new time limit. If timer is not running, updates the remaining time as well.
    /// </summary>
    public void SetTimeLimit(float newTimeLimit)
    {
        initialTimeLimit = newTimeLimit;
        if (!isRunning)
        {
            remainingTime = newTimeLimit;
        }
    }

    /// <summary>
    /// Gets the formatted time string (MM:SS).
    /// </summary>
    public string GetFormattedTime()
    {
        int minutes = Mathf.FloorToInt(remainingTime / 60f);
        int seconds = Mathf.FloorToInt(remainingTime % 60f);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    /// <summary>
    /// Gets the normalized time (0-1 ratio of remaining time to initial time).
    /// </summary>
    public float GetNormalizedTime()
    {
        return remainingTime / initialTimeLimit;
    }
}