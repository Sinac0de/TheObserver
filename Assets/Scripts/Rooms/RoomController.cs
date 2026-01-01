using UnityEngine;
using System;

public class RoomController : MonoBehaviour {
    [Header("Room Settings")]
    public float roomDuration = 60f; //seconds
    private float timer;
    private bool isActive;

    // Events
    public static event Action OnRoomStarted;
    public static event Action<bool, float> OnRoomEnded; // success, timeLeft

    private void Start() {
        timer = roomDuration;
    }

    // when player enters the room
    public void EnterRoom() {
        isActive = true;
        timer = roomDuration;
        Debug.Log($"Room Started: {gameObject.name}");
        OnRoomStarted?.Invoke();
    }

    private void Update() {
        if (!isActive) return;

        timer -= Time.deltaTime;

        if (timer <= 0f) {
            EndRoom(false); // if time runs out, room failed
        }
    }

    public void EndRoom(bool success) {
        if (!isActive) return;

        isActive = false;
        Debug.Log($"Room Ended: {gameObject.name}, Success: {success}, TimeLeft: {timer}");
        OnRoomEnded?.Invoke(success, timer);
    }

    public float GetTimeLeft() {
        return timer;
    }
}
