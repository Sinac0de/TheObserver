using UnityEngine;
using System;

public class GameManager : MonoBehaviour {
    public static GameManager Instance { get; private set; }

    public enum GameState {
        MainMenu,
        Running,
        Paused,
        GameOver
    }

    public GameState CurrentState { get; private set; }

    // Events
    public event Action<GameState> OnGameStateChanged;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start() {
        SetState(GameState.Running);
    }

    public void SetState(GameState newState) {
        if (CurrentState == newState) return; 

        CurrentState = newState;
        OnGameStateChanged?.Invoke(newState);

        Debug.Log("Game State changed to: " + newState);
    }

    public void ResetRun() {
        Debug.Log("Run Reset");

        // TODO: Reset player position, ammo, health
        // TODO: Reset RoomManager / current room
    }

    public void GameOver() {
        SetState(GameState.GameOver);
        Debug.Log("Game Over!");

        // TODO: Show GameOver UI, play sound
    }

    // !!!TODO: REMOVE THESE TEST INPUTS
    private void Update() {
        if (Input.GetKeyDown(KeyCode.R))
            ResetRun();
        if (Input.GetKeyDown(KeyCode.G))
            GameOver();
    }
}
