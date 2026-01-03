using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MazeExitElevator : MonoBehaviour {
    public MazeRoomController mazeRoom;

    private void OnTriggerEnter(Collider other) {
        if (!other.CompareTag("Player")) return;

        // maybe later require an Interact; for now, entering the trigger is enough
        mazeRoom.Success();
    }
}
