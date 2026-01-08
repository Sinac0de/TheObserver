using UnityEngine;
using UnityEngine.SceneManagement;
using System;

/// <summary>
/// Central menu system manager following singleton pattern.
/// Handles all menu states, transitions, and game flow control.
/// </summary>
public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance { get; private set; }

    [Header("Menu State")]
    [SerializeField] private MenuState currentState = MenuState.MainMenu;
    [SerializeField] private bool isPaused = false;

    [Header("Scene References")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string gameSceneName = "Floor1_MazeScene";

    [Header("Game State")]
    [SerializeField] private bool isLoadingSave = false;

    // Events
    public event Action<MenuState> OnMenuStateChanged;
    public event Action<bool> OnPauseStateChanged;

    public enum MenuState
    {
        MainMenu,
        Loading,
        InGame,
        Paused,
        Credits
    }

    public MenuState CurrentState => currentState;
    public bool IsPaused => isPaused;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeMenuSystem();
    }

    private void InitializeMenuSystem()
    {
        // Subscribe to scene loading events
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        // Set initial state based on current scene
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == mainMenuSceneName)
        {
            currentState = MenuState.MainMenu;
        }
        else if (currentScene == gameSceneName)
        {
            currentState = MenuState.InGame;
        }
    }

    #region Menu Navigation Methods

    /// <summary>
    /// Start a new game session
    /// </summary>
    public void StartNewGame()
    {
        if (currentState == MenuState.Loading) return;

        currentState = MenuState.Loading;
        OnMenuStateChanged?.Invoke(currentState);

        // Clear any existing save data
        SaveGameManager.Instance?.ClearCurrentSave();

        // Load game scene asynchronously
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadSceneAsync(gameSceneName);
        }
        else
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }

    /// <summary>
    /// Continue from saved game
    /// </summary>
    public void ContinueGame()
    {
        if (currentState == MenuState.Loading) return;

        if (!SaveGameManager.Instance.HasSaveData())
        {
            Debug.LogWarning("No save data found. Starting new game instead.");
            StartNewGame();
            return;
        }

        currentState = MenuState.Loading;
        OnMenuStateChanged?.Invoke(currentState);

        isLoadingSave = true;
        
        // Load game scene asynchronously
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadSceneAsync(gameSceneName);
        }
        else
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }

    /// <summary>
    /// Return to main menu
    /// </summary>
    public void ReturnToMainMenu()
    {
        UnpauseGame();
        
        // Load main menu scene asynchronously
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadSceneAsync(mainMenuSceneName);
        }
        else
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }



    /// <summary>
    /// Quit the application
    /// </summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion

    #region Pause System

    /// <summary>
    /// Toggle pause state
    /// </summary>
    public void TogglePause()
    {
        if (currentState != MenuState.InGame) return;

        if (isPaused)
        {
            UnpauseGame();
        }
        else
        {
            PauseGame();
        }
    }

    /// <summary>
    /// Pause the game
    /// </summary>
    public void PauseGame()
    {
        if (isPaused) return;

        isPaused = true;
        currentState = MenuState.Paused;
        Time.timeScale = 0f;

        OnPauseStateChanged?.Invoke(true);
        OnMenuStateChanged?.Invoke(currentState);
    }

    /// <summary>
    /// Unpause the game
    /// </summary>
    public void UnpauseGame()
    {
        if (!isPaused) return;

        isPaused = false;
        currentState = MenuState.InGame;
        Time.timeScale = 1f;

        OnPauseStateChanged?.Invoke(false);
        OnMenuStateChanged?.Invoke(currentState);
    }

    #endregion

    #region Scene Management

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == mainMenuSceneName)
        {
            currentState = MenuState.MainMenu;
            isPaused = false;
            Time.timeScale = 1f;
        }
        else if (scene.name == gameSceneName)
        {
            currentState = MenuState.InGame;
            isPaused = false;
            Time.timeScale = 1f;

            // Handle save loading if needed
            if (isLoadingSave)
            {
                SaveGameManager.Instance.LoadGame();
                isLoadingSave = false;
            }
        }

        OnMenuStateChanged?.Invoke(currentState);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Check if we can interact with menus right now
    /// </summary>
    public bool CanInteractWithMenus()
    {
        return currentState != MenuState.Loading;
    }

    /// <summary>
    /// Get formatted display name for current state
    /// </summary>
    public string GetCurrentStateDisplayName()
    {
        return currentState.ToString().Replace("_", " ");
    }

    #endregion
}