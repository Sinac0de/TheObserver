using UnityEngine;

/// <summary>
/// Initializes all menu systems at game start
/// </summary>
public class MenuSystemInitializer : MonoBehaviour
{
    [Header("Menu System Prefabs")]
    [SerializeField] private GameObject menuManagerPrefab;
    [SerializeField] private GameObject saveGameManagerPrefab;
    [SerializeField] private GameObject sceneTransitionManagerPrefab;

    [Header("Initialization Order")]
    [SerializeField] private bool initializeSaveFirst = true;

    private bool isInitialized = false;

    private void Start()
    {
        InitializeMenuSystems();
    }

    private void InitializeMenuSystems()
    {
        if (isInitialized) return;

        // Initialize managers in proper order
        if (initializeSaveFirst)
            InitializeSaveGameManager();

        // Initialize core managers
        InitializeMenuManager();
        InitializeSceneTransitionManager();

        isInitialized = true;
        
        Debug.Log("Menu systems initialized successfully");
    }

    private void InitializeMenuManager()
    {
        if (MenuManager.Instance == null)
        {
            GameObject managerObj = Instantiate(menuManagerPrefab);
            if (managerObj == null)
            {
                // Create empty GameObject if prefab isn't assigned
                managerObj = new GameObject("MenuManager");
                managerObj.AddComponent<MenuManager>();
            }
        }
    }

    private void InitializeSaveGameManager()
    {
        if (SaveGameManager.Instance == null)
        {
            GameObject managerObj = Instantiate(saveGameManagerPrefab);
            if (managerObj == null)
            {
                // Create empty GameObject if prefab isn't assigned
                managerObj = new GameObject("SaveGameManager");
                managerObj.AddComponent<SaveGameManager>();
            }
        }
    }


    private void InitializeSceneTransitionManager()
    {
        if (SceneTransitionManager.Instance == null)
        {
            GameObject managerObj = Instantiate(sceneTransitionManagerPrefab);
            if (managerObj == null)
            {
                // Create empty GameObject if prefab isn't assigned
                managerObj = new GameObject("SceneTransitionManager");
                managerObj.AddComponent<SceneTransitionManager>();
            }
        }
    }

    /// <summary>
    /// Force reinitialization of menu systems (useful for testing)
    /// </summary>
    public void ReinitializeMenuSystems()
    {
        isInitialized = false;
        InitializeMenuSystems();
    }
}