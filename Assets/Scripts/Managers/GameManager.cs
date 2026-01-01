using UnityEngine;
using System;

public class GameManager : MonoBehaviour {
    public static GameManager Instance { get; private set; }
    public GameState CurrentState { get; private set; }

    public event Action<GameState> OnGameStateChanged;

    public enum GameState {
        MainMenu,
        Running,
        Paused,
        GameOver
    }


    private void Awake() {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    private void Start() {
        SetState(GameState.Running);
    }

    public void SetState(GameState newState) {
        CurrentState = newState;
        OnGameStateChanged?.Invoke(newState);
        Debug.Log("Game State changed to: " + newState);
    }

    public void ResetRun() {
        // Reset player position, ammo, health
        // Reset room manager if needed
        Debug.Log("Run Reset");
    }

    public void GameOver() {
        SetState(GameState.GameOver);
        // Show UI, play sound
    }
}
