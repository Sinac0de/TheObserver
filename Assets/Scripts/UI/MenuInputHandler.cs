using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Handles input for menu navigation using GameInputManager events
/// </summary>
public class MenuInputHandler : MonoBehaviour
{
    [Header("UI Navigation")]
    [SerializeField] private EventSystem eventSystem;
    [SerializeField] private GameObject firstSelectedButton;

    [Header("Input Settings")]
    [SerializeField] private float inputDelay = 0.2f; // Delay to prevent rapid input

    private float lastInputTime = 0f;
    private bool isMenuActive = true;

    private void OnEnable()
    {
        // Subscribe to GameInputManager events
        if (GameInputManager.Instance != null)
        {
            GameInputManager.Instance.OnInteract += HandleSelectInput;
            GameInputManager.Instance.OnJump += HandleBackInput;
            GameInputManager.Instance.OnNavigateUp += HandleNavigateUp;
            GameInputManager.Instance.OnNavigateDown += HandleNavigateDown;
            GameInputManager.Instance.OnNavigateLeft += HandleNavigateLeft;
            GameInputManager.Instance.OnNavigateRight += HandleNavigateRight;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from GameInputManager events
        if (GameInputManager.Instance != null)
        {
            GameInputManager.Instance.OnInteract -= HandleSelectInput;
            GameInputManager.Instance.OnJump -= HandleBackInput;
            GameInputManager.Instance.OnNavigateUp -= HandleNavigateUp;
            GameInputManager.Instance.OnNavigateDown -= HandleNavigateDown;
            GameInputManager.Instance.OnNavigateLeft -= HandleNavigateLeft;
            GameInputManager.Instance.OnNavigateRight -= HandleNavigateRight;
        }
    }

    private void Start()
    {
        InitializeEventSystem();
    }

    private void InitializeEventSystem()
    {
        if (eventSystem == null)
            eventSystem = FindObjectOfType<EventSystem>();

        if (eventSystem != null && firstSelectedButton != null)
        {
            eventSystem.SetSelectedGameObject(firstSelectedButton);
        }
    }

    private void HandleSelectInput()
    {
        if (!isMenuActive) return;

        // Check for input delay
        if (Time.time - lastInputTime < inputDelay)
            return;

        SelectCurrentButton();
        lastInputTime = Time.time;
    }

    private void HandleBackInput()
    {
        if (!isMenuActive) return;

        // Check for input delay
        if (Time.time - lastInputTime < inputDelay)
            return;

        // Handle back functionality based on current menu state
        if (MenuManager.Instance != null)
        {
            switch (MenuManager.Instance.CurrentState)
            {
                case MenuManager.MenuState.Paused:
                    // If paused, just unpause
                    MenuManager.Instance.UnpauseGame();
                    break;
                case MenuManager.MenuState.MainMenu:
                    // If in main menu and pressing back, potentially quit
                    // This can be customized based on your needs
                    break;
                default:
                    break;
            }
        }

        lastInputTime = Time.time;
    }

    private void HandleNavigateUp()
    {
        if (!isMenuActive) return;

        // Check for input delay
        if (Time.time - lastInputTime < inputDelay)
            return;

        NavigateToPreviousButton();
        lastInputTime = Time.time;
    }

    private void HandleNavigateDown()
    {
        if (!isMenuActive) return;

        // Check for input delay
        if (Time.time - lastInputTime < inputDelay)
            return;

        NavigateToNextButton();
        lastInputTime = Time.time;
    }

    private void HandleNavigateLeft()
    {
        // Handle left navigation if needed
        // This can be expanded based on specific menu needs
    }

    private void HandleNavigateRight()
    {
        // Handle right navigation if needed
        // This can be expanded based on specific menu needs
    }

    private void NavigateToNextButton()
    {
        if (eventSystem == null) return;

        var current = eventSystem.currentSelectedGameObject;
        if (current != null)
        {
            var button = current.GetComponent<UnityEngine.UI.Button>();
            if (button != null)
            {
                button.OnSelect(null); // This triggers navigation to next
            }
        }
    }

    private void NavigateToPreviousButton()
    {
        if (eventSystem == null) return;

        var current = eventSystem.currentSelectedGameObject;
        if (current != null)
        {
            var button = current.GetComponent<UnityEngine.UI.Button>();
            if (button != null)
            {
                // Unity's event system handles this automatically when using navigation
                // We'll trigger a navigation event
                var navigation = new UnityEngine.UI.Navigation();
                navigation.mode = UnityEngine.UI.Navigation.Mode.Explicit;
                button.navigation = navigation;
            }
        }
    }

    private void SelectCurrentButton()
    {
        if (eventSystem == null) return;

        var current = eventSystem.currentSelectedGameObject;
        if (current != null)
        {
            var button = current.GetComponent<UnityEngine.UI.Button>();
            if (button != null)
            {
                button.onClick.Invoke();
            }
        }
    }



    public void SetMenuActive(bool active)
    {
        isMenuActive = active;
    }

    public void SetSelectedButton(GameObject button)
    {
        if (eventSystem != null && button != null)
        {
            eventSystem.SetSelectedGameObject(button);
        }
    }
}