using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FloorElevatorController : MonoBehaviour {
    private const string ANIMATOR_PARAM_IS_DOOR_OPEN = "IsDoorOpen";
    [Header("Floor Index (1=Maze,2=Horror,3=Boss)")]
    public int floorIndex = 1;

    [Header("References")]
    public Animator doorAnimator;
    public MazeRoomController mazeRoom; // For Floor1 only; use Horror/Boss for other floors
    [SerializeField] Transform playerSpawnPosition;
    [SerializeField] FlashlightController flaslightController;

    private bool roomStarted;

    private void Start() {
        // Player should already be inside this elevator when the scene starts
        OpenDoor();
        roomStarted = false;
    }

    private void OnTriggerExit(Collider other) {
        if (!other.CompareTag("Player")) return;
        if (roomStarted) return;

        roomStarted = true;
        CloseDoor();

        if (mazeRoom != null) {
            mazeRoom.StartRoom();
        }

        Debug.Log("[Elevator] Player left elevator on floor " + floorIndex);
    }

    public void RespawnPlayerInElevator(Transform player) {
        roomStarted = false;
        OpenDoor();

        var health = player.GetComponent<PlayerHealth>();
        if (health != null) {
            health.ResetHealth();
        }

        //flaslightController.RefillBattery();


        var pc = player.GetComponent<PlayerController>();
        Debug.Log(pc);
        if (pc != null) {
            pc.TeleportTo(playerSpawnPosition.transform.position, playerSpawnPosition.transform.rotation);
        } else {
            // Fallback: simple teleport if no PlayerController
            player.position = playerSpawnPosition.transform.position;
            player.rotation = playerSpawnPosition.transform.rotation;
        }

        Debug.Log("[Elevator] Player respawned in elevator on floor " + floorIndex);
    }


    private void OpenDoor() {
        if (doorAnimator != null) {
            doorAnimator.SetBool(ANIMATOR_PARAM_IS_DOOR_OPEN, true);
        }
    }

    private void CloseDoor() {
        if (doorAnimator != null) {
            doorAnimator.SetBool(ANIMATOR_PARAM_IS_DOOR_OPEN, false);
        }
    }
}
