using UnityEngine;

public class ExitDoor : MonoBehaviour {
    private void OnTriggerEnter(Collider other) {
        if (!other.CompareTag("Player")) return;

        RoomController room = GetComponentInParent<RoomController>();
        if (room != null) {
            room.EndRoom(true); // success = player reached the exit
        }
    }
}
