using UnityEngine;
using System;

public class RoomManager : MonoBehaviour {
    public static RoomManager Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] private GameObject labHubPrefab;
    [SerializeField] private GameObject[] puzzleRoomPrefabs;
    [SerializeField] private GameObject[] horrorRoomPrefabs;
    [SerializeField] private GameObject[] bossRoomPrefabs;

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

        // After each room, go back to Lab (for this jam loop)
        if (success && GameManager.Instance.RoomsCompleted >= 3) {
            // Player completed all 3 rooms - WIN!
            Debug.Log("GAME COMPLETED! All rooms cleared.");
            // TODO: Show victory screen
            GameManager.Instance.ResetRun(); // Reset for endless mode
            LoadLabHub();
        } else {
            LoadLabHub();
        }
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
