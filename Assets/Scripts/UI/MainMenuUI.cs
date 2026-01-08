using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles main menu UI functionality
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("Menu Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject creditsPanel;

    [Header("Main Menu Buttons")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button quitButton;

    [Header("Continue Button")]
    [SerializeField] private TextMeshProUGUI continueButtonText;
    [SerializeField] private TextMeshProUGUI continueSubtitleText;



    [Header("Credits Panel")]
    [SerializeField] private Button creditsBackButton;

    private void Start()
    {
        InitializeMenu();
    }

    private void InitializeMenu()
    {
        // Set up button listeners
        if (newGameButton != null)
            newGameButton.onClick.AddListener(OnNewGameClicked);

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
            UpdateContinueButton();
        }



        if (creditsButton != null)
            creditsButton.onClick.AddListener(OnCreditsClicked);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);



        if (creditsBackButton != null)
            creditsBackButton.onClick.AddListener(OnCreditsBackClicked);

        // Show main menu panel initially
        ShowMainMenu();
    }

    private void UpdateContinueButton()
    {
        if (SaveGameManager.Instance != null && SaveGameManager.Instance.HasSaveData())
        {
            if (continueButtonText != null)
                continueButtonText.text = "CONTINUE";

            if (continueSubtitleText != null)
                continueSubtitleText.text = $"Last saved: {SaveGameManager.Instance.GetFormattedSaveTime()}";
        }
        else
        {
            if (continueButtonText != null)
                continueButtonText.text = "NEW GAME";

            if (continueSubtitleText != null)
                continueSubtitleText.text = "No save data";
        }
    }

    #region Button Callbacks

    private void OnNewGameClicked()
    {
        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.StartNewGame();
        }
    }

    private void OnContinueClicked()
    {
        if (SaveGameManager.Instance != null)
        {
            if (SaveGameManager.Instance.HasSaveData())
            {
                // Continue from save
                if (MenuManager.Instance != null)
                {
                    MenuManager.Instance.ContinueGame();
                }
            }
            else
            {
                // No save data, start new game
                if (MenuManager.Instance != null)
                {
                    MenuManager.Instance.StartNewGame();
                }
            }
        }
    }



    private void OnCreditsClicked()
    {
        ShowCredits();
    }

    private void OnQuitClicked()
    {
        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.QuitGame();
        }
    }



    private void OnCreditsBackClicked()
    {
        ShowMainMenu();
    }

    #endregion

    #region Panel Management

    private void ShowMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }



    private void ShowCredits()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(true);
    }

    #endregion

    private void OnDestroy()
    {
        // Clean up listeners
        if (newGameButton != null)
            newGameButton.onClick.RemoveListener(OnNewGameClicked);

        if (continueButton != null)
            continueButton.onClick.RemoveListener(OnContinueClicked);



        if (creditsButton != null)
            creditsButton.onClick.RemoveListener(OnCreditsClicked);

        if (quitButton != null)
            quitButton.onClick.RemoveListener(OnQuitClicked);



        if (creditsBackButton != null)
            creditsBackButton.onClick.RemoveListener(OnCreditsBackClicked);
    }
}