using UnityEngine;

public class InteractObject : Interactable
{
    public override void Interact(GameObject interactor) {
        Debug.Log("[InteractObject] Interacted with " + gameObject.name);
    }
}
