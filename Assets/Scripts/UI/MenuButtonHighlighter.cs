using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Handles visual feedback for text-only menu buttons
/// Changes text color and adds hover/select effects
/// Implements Unity EventSystem interfaces for select, deselect, pointer enter/exit
/// </summary>
public class MenuButtonHighlighter : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler {
    [Header("Button References")]
    [SerializeField] private Button targetButton;
    [SerializeField] private TextMeshProUGUI buttonText;

    [Header("Color Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightedColor = Color.yellow;
    [SerializeField] private Color selectedColor = Color.red;
    [SerializeField] private Color disabledColor = Color.gray;

    [Header("Animation Settings")]
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseIntensity = 1.2f;

    private bool isSelected = false;
    private bool isHovered = false;
    private RectTransform rectTransform;
    private Vector3 originalScale;

    private void Start() {
        InitializeButton();
    }

    private void InitializeButton() {
        if (targetButton == null)
            targetButton = GetComponent<Button>();

        if (buttonText == null)
            buttonText = GetComponentInChildren<TextMeshProUGUI>();

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (rectTransform != null)
            originalScale = rectTransform.localScale;

        // Initial state
        UpdateVisualState();
    }

    public void OnSelect(BaseEventData eventData) {
        isSelected = true;
        isHovered = false;
        UpdateVisualState();
    }

    public void OnDeselect(BaseEventData eventData) {
        isSelected = false;
        UpdateVisualState();
    }

    public void OnPointerEnter(PointerEventData eventData) {
        if (!isSelected) {
            isHovered = true;
            UpdateVisualState();
        }
    }

    public void OnPointerExit(PointerEventData eventData) {
        isHovered = false;
        UpdateVisualState();
    }

    private void UpdateVisualState() {
        if (buttonText == null) return;

        if (!targetButton.interactable) {
            buttonText.color = disabledColor;
            if (rectTransform != null)
                rectTransform.localScale = originalScale;
        } else if (isSelected) {
            buttonText.color = selectedColor;
            // Add pulsing effect when selected
            if (rectTransform != null) {
                float pulse = 1 + Mathf.Sin(Time.time * pulseSpeed) * (pulseIntensity - 1);
                rectTransform.localScale = originalScale * pulse;
            }
        } else if (isHovered) {
            buttonText.color = highlightedColor;
            if (rectTransform != null)
                rectTransform.localScale = originalScale * 1.1f; // Slight scale up
        } else {
            buttonText.color = normalColor;
            if (rectTransform != null)
                rectTransform.localScale = originalScale;
        }
    }

    private void Update() {
        // Continuously update for pulsing effect
        if (isSelected) {
            UpdateVisualState();
        }
    }

    public void SetNormalColor(Color color) {
        normalColor = color;
        UpdateVisualState();
    }

    public void SetHighlightedColor(Color color) {
        highlightedColor = color;
        UpdateVisualState();
    }

    public void SetSelectedColor(Color color) {
        selectedColor = color;
        UpdateVisualState();
    }
}
