using UnityEngine;

public abstract class BaseRoomController : MonoBehaviour, IRoom {
    [Header("Room State")]
    [SerializeField] protected RoomState currentState = RoomState.Inactive;

    [Header("Metrics (runtime)")]
    [SerializeField] protected float roomStartTime;
    [SerializeField] protected int mistakes;
    [SerializeField] protected int detections;

    protected AIModel aiModel;

    public RoomState CurrentState => currentState;
    public int Mistakes => mistakes;
    public int Detections => detections;
    public float SolveTime => Mathf.Max(0f, Time.time - roomStartTime);

    public virtual void Initialize(AIModel model) {
        aiModel = model;
        currentState = RoomState.Initializing;

        ApplyAIDifficulty();

        roomStartTime = Time.time;
        mistakes = 0;
        detections = 0;

        currentState = RoomState.Active;
    }

    public virtual void OnPlayerEnter() {
        currentState = RoomState.Active;
    }

    public virtual void OnPlayerExit() {
        currentState = RoomState.Inactive;
    }

    public virtual void RegisterMistake() {
        mistakes++;
    }

    public virtual void RegisterDetection() {
        detections++;
    }

    public abstract bool CanExit();

    public float GetSolveTime() => SolveTime;

    /// <summary>
    /// Each room type applies AI difficulty differently (traps, speeds, etc.).
    /// </summary>
    protected abstract void ApplyAIDifficulty();
}
