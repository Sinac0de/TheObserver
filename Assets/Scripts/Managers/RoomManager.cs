using UnityEngine;
using System;

public class RoomManager : MonoBehaviour {
    public static RoomManager Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] private GameObject labHubPrefab;
    [SerializeField] private GameObject[] puzzleRoomPrefabs;
    [SerializeField] private GameObject[] horrorRoomPrefabs;
    [SerializeField] private GameObject[] bossRoomPrefabs;
    [SerializeField] private GameObject elevatorRoomPrefab;

    [Header("Runtime Parents")]
    [SerializeField] private Transform roomsRoot;

    // Events (TODO: connect to the playerController)
    public event Action<IRoom> OnRoomLoaded;
    public event Action<IRoom, bool> OnRoomFinished; // room, success
    public event Action OnTransitionStarted;

    private GameObject currentLabInstance;
    private GameObject currentRoomGO;
    private IRoom currentRoom;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (roomsRoot == null) {
            GameObject root = new GameObject("RoomsRoot");
            roomsRoot = root.transform;
            DontDestroyOnLoad(root);
        }
    }

    private void Start() {
        // Ensure Lab Hub is loaded at start
        LoadLabHub();
    }
    
    public void LoadElevator() {
        OnTransitionStarted?.Invoke();

        // Destroy current room
        if (currentRoomGO != null) {
            currentRoom?.OnPlayerExit();
            Destroy(currentRoomGO);
            currentRoomGO = null;
            currentRoom = null;
        }

        // Hide Lab Hub
        if (currentLabInstance != null)
            currentLabInstance.SetActive(false);

        // Load the elevator room prefab
        GameObject elevatorPrefab = elevatorRoomPrefab;
        
        if (elevatorPrefab == null && puzzleRoomPrefabs.Length > 0)
        {
            // Fallback: use first puzzle room as elevator room
            elevatorPrefab = puzzleRoomPrefabs[0];
            Debug.LogWarning("No dedicated elevator room prefab assigned. Using first puzzle room as elevator.");
        }

        if (elevatorPrefab != null)
        {
            LoadRoomInternal(elevatorPrefab);
        }
        else
        {
            Debug.LogWarning("RoomManager: No elevator room prefab available.");
            // Fallback: return to lab hub
            LoadLabHub();
        }
    }

    public void LoadLabHub() {
        OnTransitionStarted?.Invoke();

        // Destroy current room
        if (currentRoomGO != null) {
            currentRoom?.OnPlayerExit();
            Destroy(currentRoomGO);
            currentRoomGO = null;
            currentRoom = null;
        }

        // Create or reuse lab
        if (currentLabInstance == null && labHubPrefab != null) {
            currentLabInstance = Instantiate(labHubPrefab, Vector3.zero, Quaternion.identity, roomsRoot);
        }

        if (currentLabInstance != null)
            currentLabInstance.SetActive(true);

        TeleportPlayerToSpawn(currentLabInstance, "PlayerSpawn");
    }

    public void LoadNextRoom() {
        OnTransitionStarted?.Invoke();

        // Check if we're coming from a completed room and need to return to elevator/lab
        // If current room exists, it means we're completing a room and returning to hub
        if (currentRoom != null)
        {
            // After completing a room, return to the elevator/lab hub
            LoadLabHub();
            return;
        }
        
        // Otherwise, this is the initial loading of the next room in sequence
        int completed = GameManager.Instance.RoomsCompleted;

        GameObject prefabToUse = null;
        if (completed == 0 && puzzleRoomPrefabs.Length > 0)
            prefabToUse = puzzleRoomPrefabs[0];
        else if (completed == 1 && horrorRoomPrefabs.Length > 0)
            prefabToUse = horrorRoomPrefabs[0];
        else if (bossRoomPrefabs.Length > 0)
            prefabToUse = bossRoomPrefabs[0]; // Boss room
        else
        {
            Debug.LogWarning("RoomManager: No boss room prefab assigned. Ending game.");
            return;
        }

        LoadRoomInternal(prefabToUse);
    }
    
    public void LoadSpecificRoom(RoomType roomType) {
        OnTransitionStarted?.Invoke();
        
        GameObject prefabToUse = null;
        
        switch (roomType)
        {
            case RoomType.Maze:
                if (puzzleRoomPrefabs.Length > 0) // Using puzzle room array for maze
                    prefabToUse = puzzleRoomPrefabs[0];
                break;
            case RoomType.Horror:
                if (horrorRoomPrefabs.Length > 0)
                    prefabToUse = horrorRoomPrefabs[0];
                break;
            case RoomType.Boss:
                if (bossRoomPrefabs.Length > 0)
                    prefabToUse = bossRoomPrefabs[0];
                break;
        }
        
        if (prefabToUse == null)
        {
            Debug.LogError($"RoomManager: No prefab found for room type: {roomType}");
            return;
        }
        
        LoadRoomInternal(prefabToUse);
    }
    
    public enum RoomType
    {
        Maze,
        Horror,
        Boss
    }

    private void LoadRoomInternal(GameObject roomPrefab) {
        if (roomPrefab == null) {
            Debug.LogError("RoomManager: No room prefab assigned for this stage.");
            return;
        }

        // Destroy previous room
        if (currentRoomGO != null) {
            currentRoom?.OnPlayerExit();
            Destroy(currentRoomGO);
        }

        // Hide Lab Hub
        if (currentLabInstance != null)
            currentLabInstance.SetActive(false);

        // Instantiate new room
        currentRoomGO = Instantiate(roomPrefab, Vector3.zero, Quaternion.identity, roomsRoot);
        currentRoom = currentRoomGO.GetComponent<IRoom>();

        if (currentRoom == null) {
            Debug.LogError("RoomManager: Room prefab does not implement IRoom.");
            return;
        }

        // Initialize AI difficulty
        currentRoom.Initialize(GameManager.Instance.AIModel);
        TeleportPlayerToSpawn(currentRoomGO, "PlayerSpawn");
        currentRoom.OnPlayerEnter();

        OnRoomLoaded?.Invoke(currentRoom);
    }

    public void CompleteCurrentRoom(bool success) {
        if (currentRoom == null) return;

        float solveTime = currentRoom.GetSolveTime();
        int mistakes = currentRoom.Mistakes;
        int detections = currentRoom.Detections;

        GameManager.Instance.AIModel.RegisterRoomResult(success, solveTime, mistakes, detections);

        if (success) {
            GameManager.Instance.RegisterRoomCompleted();
        } else {
            GameManager.Instance.RegisterDeath();
        }

        OnRoomFinished?.Invoke(currentRoom, success);

        // After each room, go back to Lab Hub
        LoadLabHub();
    }

    private void TeleportPlayerToSpawn(GameObject root, string spawnName) {
        if (root == null) return;

        Transform spawn = FindChildRecursive(root.transform, spawnName);
        if (spawn == null) {
            Debug.LogWarning($"Spawn '{spawnName}' not found under {root.name}.");
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        var controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;

        player.transform.position = spawn.position;
        player.transform.rotation = spawn.rotation;

        if (controller != null) controller.enabled = true;
    }

    private Transform FindChildRecursive(Transform parent, string name) {
        foreach (Transform child in parent) {
            if (child.name == name) return child;
            Transform result = FindChildRecursive(child, name);
            if (result != null) return result;
        }
        return null;
    }
}
