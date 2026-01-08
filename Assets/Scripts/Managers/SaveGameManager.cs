using UnityEngine;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

/// <summary>
/// Handles all save/load functionality for the game.
/// Manages player progress, settings, and game state persistence.
/// </summary>
[System.Serializable]
public class SaveGameData
{
    // Player Progress
    public int roomsCompleted;
    public int deaths;
    public int currentRunScore;
    public float playTimeSeconds;
    
    // Player State
    public Vector3 playerPosition;
    public int playerHealth;
    public float flashlightBattery;
    
    // Game Settings
    public float masterVolume;
    public float musicVolume;
    public float sfxVolume;
    
    // Timestamp
    public DateTime saveTimestamp;
    
    // Constructor
    public SaveGameData()
    {
        saveTimestamp = DateTime.Now;
        masterVolume = 1.0f;
        musicVolume = 1.0f;
        sfxVolume = 1.0f;
    }
}

/// <summary>
/// Singleton manager for save game operations
/// </summary>
public class SaveGameManager : MonoBehaviour
{
    public static SaveGameManager Instance { get; private set; }

    [Header("Save Configuration")]
    [SerializeField] private string saveFileName = "game_save.dat";
    [SerializeField] private bool _autoSaveEnabled = true;
    [SerializeField] private float autoSaveInterval = 300f; // 5 minutes
    
    public bool autoSaveEnabled
    {
        get => _autoSaveEnabled;
        set => _autoSaveEnabled = value;
    }

    private SaveGameData currentSaveData;
    private string saveFilePath;
    private float lastAutoSaveTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeSaveSystem();
    }

    private void InitializeSaveSystem()
    {
        saveFilePath = Path.Combine(Application.persistentDataPath, saveFileName);
        LoadSaveData();
    }

    private void Update()
    {
        // Handle auto-save
        if (_autoSaveEnabled && 
            MenuManager.Instance != null && 
            MenuManager.Instance.CurrentState == MenuManager.MenuState.InGame &&
            Time.time - lastAutoSaveTime >= autoSaveInterval)
        {
            AutoSave();
        }
    }

    #region Save Operations

    /// <summary>
    /// Create a new save game
    /// </summary>
    public void CreateNewSave()
    {
        currentSaveData = new SaveGameData();
        SaveToFile();
    }

    /// <summary>
    /// Save current game state
    /// </summary>
    public void SaveGame()
    {
        if (MenuManager.Instance.CurrentState != MenuManager.MenuState.InGame)
        {
            Debug.LogWarning("Cannot save - not in game state");
            return;
        }

        CaptureGameState();
        SaveToFile();
        lastAutoSaveTime = Time.time;
    }

    /// <summary>
    /// Auto-save functionality
    /// </summary>
    public void AutoSave()
    {
        if (HasSaveData())
        {
            SaveGame();
            Debug.Log("Auto-saved game progress");
        }
    }

    /// <summary>
    /// Load saved game data
    /// </summary>
    public void LoadGame()
    {
        if (!HasSaveData())
        {
            Debug.LogWarning("No save data to load");
            return;
        }

        ApplyGameState();
    }

    /// <summary>
    /// Clear current save data
    /// </summary>
    public void ClearCurrentSave()
    {
        currentSaveData = null;
        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
        }
    }

    #endregion

    #region Data Management

    /// <summary>
    /// Check if save data exists
    /// </summary>
    public bool HasSaveData()
    {
        return currentSaveData != null || File.Exists(saveFilePath);
    }

    /// <summary>
    /// Get last save timestamp
    /// </summary>
    public DateTime GetLastSaveTime()
    {
        if (currentSaveData != null)
            return currentSaveData.saveTimestamp;
        
        if (File.Exists(saveFilePath))
        {
            var fileInfo = new FileInfo(saveFilePath);
            return fileInfo.LastWriteTime;
        }

        return DateTime.MinValue;
    }

    /// <summary>
    /// Get formatted save time string
    /// </summary>
    public string GetFormattedSaveTime()
    {
        DateTime saveTime = GetLastSaveTime();
        if (saveTime == DateTime.MinValue)
            return "No save data";

        return saveTime.ToString("MMM dd, yyyy HH:mm");
    }

    #endregion

    #region Private Methods

    private void LoadSaveData()
    {
        if (!File.Exists(saveFilePath))
        {
            currentSaveData = null;
            return;
        }

        try
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream stream = new FileStream(saveFilePath, FileMode.Open))
            {
                currentSaveData = (SaveGameData)formatter.Deserialize(stream);
            }
            Debug.Log($"Save data loaded from: {saveFilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load save data: {e.Message}");
            currentSaveData = null;
        }
    }

    private void SaveToFile()
    {
        try
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream stream = new FileStream(saveFilePath, FileMode.Create))
            {
                formatter.Serialize(stream, currentSaveData);
            }
            Debug.Log($"Game saved to: {saveFilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save game: {e.Message}");
        }
    }

    private void CaptureGameState()
    {
        if (currentSaveData == null)
            currentSaveData = new SaveGameData();

        // Get player state
        var player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            currentSaveData.playerPosition = player.transform.position;
            var playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
                currentSaveData.playerHealth = playerHealth.CurrentHealth;
        }

        // Get flashlight state
        var flashlight = FindObjectOfType<FlashlightController>();
        if (flashlight != null)
            currentSaveData.flashlightBattery = flashlight.BatteryPercentage;

        // Get game progress
        if (GameManager.Instance != null)
        {
            currentSaveData.roomsCompleted = GameManager.Instance.RoomsCompleted;
            currentSaveData.deaths = GameManager.Instance.Deaths;
            currentSaveData.currentRunScore = GameManager.Instance.CurrentRunScore;
        }

        // Get timer data
        var timer = FindObjectOfType<TimerManager>();
        if (timer != null)
            currentSaveData.playTimeSeconds = timer.ElapsedTime;

        // Update timestamp
        currentSaveData.saveTimestamp = DateTime.Now;
    }

    private void ApplyGameState()
    {
        if (currentSaveData == null) return;

        // Apply player position
        var player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.transform.position = currentSaveData.playerPosition;
            
            var playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
                playerHealth.SetHealth(currentSaveData.playerHealth);
        }

        // Apply flashlight state
        var flashlight = FindObjectOfType<FlashlightController>();
        if (flashlight != null)
            flashlight.SetBatteryLevel(currentSaveData.flashlightBattery);

        // Apply game progress
        if (GameManager.Instance != null)
        {
            // Note: We'd need to modify GameManager to allow setting these values
            // For now, we'll just log what would be restored
            Debug.Log($"Would restore: Rooms={currentSaveData.roomsCompleted}, Score={currentSaveData.currentRunScore}");
        }

        Debug.Log("Game state applied from save data");
    }

    #endregion

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}