using UnityEngine;

public class RoomManager : MonoBehaviour {
    public static RoomManager Instance;

    [Header("Current Room")]
    public RoomController currentRoom;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void LoadRoom(RoomController roomPrefab, Transform playerSpawn) {
        if (currentRoom != null)
            Destroy(currentRoom.gameObject);

        currentRoom = Instantiate(roomPrefab);
        currentRoom.EnterRoom();

        SpawnPlayer(playerSpawn);
    }

    private void SpawnPlayer(Transform spawnPoint) {
        GameObject player = Player.Instance.gameObject;
        player.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        player.SetActive(true);
    }
}
