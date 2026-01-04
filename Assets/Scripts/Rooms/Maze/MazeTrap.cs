using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MazeTrap : MonoBehaviour {
    private void OnTriggerEnter(Collider other) {
        if (!other.CompareTag("Player")) return;

        var health = other.GetComponent<PlayerHealth>();
        if (health != null) {
            health.ApplyTrapDamageAndImmune();
        }

        var mazeRoom = FindObjectOfType<MazeRoomController>();
        if (mazeRoom != null) {
            mazeRoom.RegisterMistake();
        }
    }
}
