using UnityEngine;

public class RoomDebug : MonoBehaviour {
    public Vector3 debugSize = new Vector3(10, 4, 10);

    private void OnDrawGizmos() {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position + Vector3.up * (debugSize.y / 2), debugSize);
    }
}
