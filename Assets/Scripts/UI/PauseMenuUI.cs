using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Handles pause menu functionality
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    [Header("Pause Menu Panel")]
    [SerializeField] private GameObject pauseMenuPanel;

    [Header("Pause Menu Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;

    private bool isPauseMenuActive = false;

    private void Start()
    {
        InitializePauseMenu();
        HidePauseMenu();
    }

    private void InitializePauseMenu()
    {
        // Set up button listeners
        if (resumeButton != null)
            resumeButton.onClick.AddListener(OnResumeClicked);



        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);



        // Subscribe to menu manager events
        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.OnPauseStateChanged += OnPauseStateChanged;
        }
    }

    private void OnPauseStateChanged(bool isPaused)
    {
        if (isPaused)
        {
            ShowPauseMenu();
        }
        else
        {
            HidePauseMenu();
        }
    }

    #region Button Callbacks

    private void OnResumeClicked()
    {
        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.UnpauseGame();
        }
    }



    private void OnMainMenuClicked()
    {
        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.ReturnToMainMenu();
        }
    }

    private void OnQuitClicked()
    {
        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.QuitGame();
        }
    }



    #endregion

    #region Menu Display

    private void ShowPauseMenu()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
        isPauseMenuActive = true;
    }

    private void HidePauseMenu()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        isPauseMenuActive = false;
    }



    #endregion

    private void OnDestroy()
    {
        // Clean up listeners
        if (resumeButton != null)
            resumeButton.onClick.RemoveListener(OnResumeClicked);



        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);

        if (quitButton != null)
            quitButton.onClick.RemoveListener(OnQuitClicked);



        // Unsubscribe from events
        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.OnPauseStateChanged -= OnPauseStateChanged;
        }
    }
}