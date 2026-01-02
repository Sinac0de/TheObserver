using UnityEngine;

public class DebugAIOverlay : MonoBehaviour {
    [SerializeField]
    private AIModel aiModel;

    [SerializeField]
    private KeyCode toggleKey = KeyCode.F1;

    [SerializeField]
    private bool visible = true;

    [SerializeField]
    private Color backgroundColor = new Color(0f, 0f, 0f, 0.6f);

    [SerializeField]
    private Color textColor = Color.green;

    private GUIStyle _labelStyle;
    private Texture2D _backgroundTexture;

    private void Awake() {
        if (aiModel == null && GameManager.Instance != null) {
            aiModel = GameManager.Instance.AIModel;
        }

        _labelStyle = new GUIStyle {
            fontSize = 14,
            normal =
            {
                textColor = textColor
            }
        };

        _backgroundTexture = new Texture2D(1, 1);
        _backgroundTexture.SetPixel(0, 0, backgroundColor);
        _backgroundTexture.Apply();
    }

    private void Update() {
        if (Input.GetKeyDown(toggleKey)) {
            visible = !visible;
        }
    }

    private void OnGUI() {
        if (!visible || aiModel == null) return;

        const float width = 320f;
        const float height = 130f;
        Rect rect = new Rect(10f, 10f, width, height);

        GUI.DrawTexture(rect, _backgroundTexture);

        GUILayout.BeginArea(rect);
        GUILayout.Label("AI OBSERVER DEBUG", _labelStyle);
        GUILayout.Label($"Current Complexity: {aiModel.CurrentComplexity:0.00}", _labelStyle);
        GUILayout.Label($"Rooms Completed: {aiModel.TotalRoomsCompleted}", _labelStyle);
        GUILayout.Label($"Total Failures: {aiModel.TotalFailures}", _labelStyle);
        GUILayout.Label($"Last Solve Time: {aiModel.LastRoomSolveTime:0.0}s", _labelStyle);
        GUILayout.Label($"Last Mistakes: {aiModel.LastRoomMistakes}", _labelStyle);
        GUILayout.Label($"Last Detections: {aiModel.LastRoomDetections}", _labelStyle);
        GUILayout.EndArea();
    }
}
