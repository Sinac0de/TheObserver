using UnityEngine;

public abstract class Interactable : MonoBehaviour {
    [Header("Interactable")]
    [TextArea] public string prompt = "Interact";
    public float interactionRange = 3f;
    public LayerMask interactionLayerMask = ~0;

    /// <summary>
    /// Called by PlayerInteractor when player triggers Interact input while looking at this object.
    /// </summary>
    public abstract void Interact(GameObject interactor);
}
