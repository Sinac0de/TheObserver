using UnityEngine;

public class PuzzleRoomController : BaseRoomController {
    [Header("Puzzle Elements")]
    [SerializeField] private PressurePlate[] pressurePlates;
    [SerializeField] private GameObject[] meltPools;
    [SerializeField] private Animator exitDoorAnimator;

    private int platesPressed;

    protected override void ApplyAIDifficulty() {
        if (aiModel == null) {
            Debug.LogWarning("PuzzleRoomController: AIModel not assigned in Initialize.");
            return;
        }

        // if complexity is 0, ensure at least one melt pool is active
        float c = aiModel.CurrentComplexity;

        // activate melt pools based on complexity
        int activeCount = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(1, meltPools.Length, c)), 1, meltPools.Length);
        for (int i = 0; i < meltPools.Length; i++) {
            if (meltPools[i] == null) continue;

            bool shouldBeActive = i < activeCount;
            meltPools[i].SetActive(shouldBeActive);

            if (shouldBeActive) {
                var hazard = meltPools[i].GetComponent<HazardDamageZone>();
                if (hazard != null) {
                    hazard.damagePerSecond = Mathf.Lerp(5f, 25f, c);
                }
            }
        }

        foreach (var plate in pressurePlates) {
            if (plate != null) {
                plate.requiredMass = Mathf.Lerp(0.1f, 2f, c);
            }
        }
    }

    private void OnEnable() {
        foreach (var plate in pressurePlates) {
            if (plate == null) continue;
            plate.OnPressed.AddListener(HandlePlatePressed);
        }
    }

    private void OnDisable() {
        foreach (var plate in pressurePlates) {
            if (plate == null) continue;
            plate.OnPressed.RemoveListener(HandlePlatePressed);
        }
    }

    private void HandlePlatePressed() {
        platesPressed++;
        if (platesPressed >= pressurePlates.Length) {
            CompletePuzzle();
        }
    }

    private void CompletePuzzle() {
        if (currentState == RoomState.Completing) return;

        currentState = RoomState.Completing;
        Debug.Log("Puzzle completed! Opening exit door...");

        if (exitDoorAnimator != null) {
            exitDoorAnimator.SetTrigger("Open");
        }

        // Enable exit interaction
        ExitDoor exitDoor = FindObjectOfType<ExitDoor>();
        if (exitDoor != null)
        {
            exitDoor.EnableExit();
        }

        // Delay notifying the RoomManager to allow door animation to play
        Invoke(nameof(NotifyRoomManagerSuccess), 1.5f);
    }

    private void NotifyRoomManagerSuccess() {
        RoomManager.Instance.CompleteCurrentRoom(true);
    }

    public override bool CanExit() {
        return platesPressed >= pressurePlates.Length;
    }
}
