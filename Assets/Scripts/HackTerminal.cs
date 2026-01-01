using UnityEngine;
using System;

public class HackTerminal : MonoBehaviour {
    public static event Action OnHackRequested;

    public void Interact() {
        Debug.Log("Hack Terminal Interacted");
        OnHackRequested?.Invoke();
    }
}
