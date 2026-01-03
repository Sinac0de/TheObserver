using UnityEngine;
using UnityEngine.SceneManagement;

public class LabElevatorConsole : Interactable {
    [Header("Target Floor Scene")]
    [SerializeField] private string floor1SceneName = "Floor1_MazeScene";

    public override void Interact(GameObject interactor) {
        Debug.Log("[LabElevatorConsole] Enter Next Trial -> Loading " + floor1SceneName);

        // Optional: reset run stats when starting a new run
        if (GameManager.Instance != null) {
            GameManager.Instance.ResetRun();
        }

        // Load Floor1 scene
        SceneManager.LoadScene(floor1SceneName);
    }
}
