using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FloorElevatorController : MonoBehaviour {
    [Header("Floor Index (1=Maze,2=Horror,3=Boss)")]
    public int floorIndex = 1;

    [Header("References")]
    public Animator doorAnimator;
    public MazeRoomController mazeRoom; // For Floor1 only; use Horror/Boss for other floors

    private bool roomStarted;

    private void Start() {
        // At scene start the player should already be inside this elevator
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

        // Place the player inside the elevator
        player.position = transform.position;
        player.rotation = transform.rotation;

        var pc = player.GetComponent<PlayerController>();
        if (pc != null) {
            pc.ForceStandUp();
        }

        Debug.Log("[Elevator] Player respawned in elevator on floor " + floorIndex);
    }

    private void OpenDoor() {
        if (doorAnimator != null) {
            doorAnimator.SetBool("IsDoorOpen", true);
        }
    }

    private void CloseDoor() {
        if (doorAnimator != null) {
            doorAnimator.SetBool("IsDoorOpen", false);
        }
    }
}
