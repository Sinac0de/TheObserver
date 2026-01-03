using UnityEngine;

public class LabHubIntroController : MonoBehaviour {
    private const string ANIMATION_PARAM_IS_DOOR_OPEN = "IsDoorOpen";

    [Header("References")]
    [SerializeField] private Transform playerSpawn;
    [SerializeField] private GameObject labElevator; // parent of elevator and console
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerInteractor playerInteractor;
    [SerializeField] private WeaponController weaponController;

    [Header("Intro Settings")]
    [SerializeField] private float introDuration = 3f;

    private bool introRunning;

    private void Start() {
        // Place player at spawn
        if (playerController != null && playerSpawn != null) {
            playerController.transform.position = playerSpawn.position;
            playerController.transform.rotation = playerSpawn.rotation;
        }

        // Disable interaction / weapon during intro
        SetPlayerControlEnabled(false);

        // Keep elevator doors closed initially (if you have an animator, do it there)
        if (labElevator != null) {
            var animator = labElevator.GetComponentInChildren<Animator>();
            if (animator != null) {
                animator.SetBool(ANIMATION_PARAM_IS_DOOR_OPEN, false);
            }
        }

        introRunning = true;
        Invoke(nameof(EndIntro), introDuration);
    }

    private void EndIntro() {
        introRunning = false;

        // Enable player control
        SetPlayerControlEnabled(true);

        // Open elevator doors
        if (labElevator != null) {
            var animator = labElevator.GetComponentInChildren<Animator>();
            if (animator != null) {
                animator.SetBool(ANIMATION_PARAM_IS_DOOR_OPEN, true);
            }
        }

        Debug.Log("[LabHubIntro] Intro finished, elevator opened.");
    }

    private void SetPlayerControlEnabled(bool enabled) {
        if (playerController != null)
            playerController.enabled = enabled;

        if (playerInteractor != null)
            playerInteractor.EnableInteraction(enabled);

        if (weaponController != null)
            weaponController.enabled = enabled;
    }
}
