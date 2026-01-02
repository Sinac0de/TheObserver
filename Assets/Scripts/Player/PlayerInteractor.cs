using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PlayerInteractor : MonoBehaviour {
    [SerializeField] private float maxInteractDistance = 3f;
    [SerializeField] private LayerMask interactMask = ~0;

    private Camera _camera;

    private void Awake() {
        _camera = GetComponent<Camera>();
    }

    private void OnEnable() {
        if (GameInputManager.Instance != null) {
            GameInputManager.Instance.OnInteract += HandleInteract;
        }
    }

    private void OnDisable() {
        if (GameInputManager.Instance != null) {
            GameInputManager.Instance.OnInteract -= HandleInteract;
        }
    }

    private void HandleInteract() {
        Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxInteractDistance, interactMask, QueryTriggerInteraction.Collide)) {
            Interactable interactable = hit.collider.GetComponentInParent<Interactable>();
            if (interactable == null) return;

            float distance = Vector3.Distance(_camera.transform.position, hit.point);
            if (distance <= interactable.interactionRange) {
                interactable.Interact(_camera.gameObject);
            }
        }
    }
}
