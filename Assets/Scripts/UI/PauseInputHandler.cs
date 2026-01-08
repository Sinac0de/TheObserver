using UnityEngine;

/// <summary>
/// Handles pause functionality through GameInputManager
/// </summary>
public class PauseInputHandler : MonoBehaviour
{
    private void OnEnable()
    {
        // Subscribe to GameInputManager events
        if (GameInputManager.Instance != null)
        {
            GameInputManager.Instance.OnInteract += HandlePauseInput;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from GameInputManager events
        if (GameInputManager.Instance != null)
        {
            GameInputManager.Instance.OnInteract -= HandlePauseInput;
        }
    }


    private void HandlePauseInput()
    {
        if (MenuManager.Instance == null) return;

        // Only allow pausing when in game
        if (MenuManager.Instance.CurrentState == MenuManager.MenuState.InGame)
        {
            MenuManager.Instance.TogglePause();
        }
        // If already paused, allow unpausing with the same key
        else if (MenuManager.Instance.CurrentState == MenuManager.MenuState.Paused)
        {
            MenuManager.Instance.UnpauseGame();
        }
    }
}