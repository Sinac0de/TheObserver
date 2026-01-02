using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PlayerInteractor : MonoBehaviour {
    [SerializeField] private float maxInteractDistance = 3f;
    [SerializeField] private LayerMask interactMask = ~0;

    private Camera _camera;
    private Interactable _currentInteractable;

    private void Awake() {
        _camera = GetComponent<Camera>();
    }

    private void OnEnable() {
        if (GameInputManager.Instance != null)
            GameInputManager.Instance.OnInteract += HandleInteract;
    }

    private void OnDisable() {
        if (GameInputManager.Instance != null)
            GameInputManager.Instance.OnInteract -= HandleInteract;
    }

    private void Update() {
        CheckForInteractable();
    }

    private void CheckForInteractable() {
        _currentInteractable = null;

        Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, maxInteractDistance, interactMask, QueryTriggerInteraction.Collide)) {

            Interactable interactable = hit.collider.GetComponentInParent<Interactable>();

            if (interactable != null) {
                float distance = Vector3.Distance(_camera.transform.position, hit.point);

                if (distance <= interactable.interactionRange) {
                    // Interactable
                    _currentInteractable = interactable;
                    Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.green);
                    return;
                }
            }

            // Hit but Not Interactable
            Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.yellow);
        }
        else {
            // Hit Nothing
            Debug.DrawRay(ray.origin, ray.direction * maxInteractDistance, Color.red);
        }
    }

    private void HandleInteract() {
        if (_currentInteractable == null) return;

        _currentInteractable.Interact(_camera.gameObject);
    }
}
