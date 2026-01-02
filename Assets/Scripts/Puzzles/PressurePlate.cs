using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class PressurePlate : MonoBehaviour {
    public float requiredMass = 0.1f;
    public float pressDepth = 0.05f;
    public UnityEvent OnPressed;
    public UnityEvent OnReleased;

    private Vector3 initialLocalPos;
    private int objectsCount;

    private void Awake() {
        initialLocalPos = transform.localPosition;
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other) {
        if (!HasSufficientMass(other)) return;

        objectsCount++;
        if (objectsCount == 1) {
            transform.localPosition = initialLocalPos + Vector3.down * pressDepth;
            OnPressed?.Invoke();
        }
    }

    private void OnTriggerExit(Collider other) {
        if (!HasSufficientMass(other)) return;

        objectsCount = Mathf.Max(0, objectsCount - 1);
        if (objectsCount == 0) {
            transform.localPosition = initialLocalPos;
            OnReleased?.Invoke();
        }
    }

    private bool HasSufficientMass(Collider other) {
        Rigidbody rb = other.attachedRigidbody;
        return rb != null && rb.mass >= requiredMass;
    }
}
