using UnityEngine;

public interface IInteractable
{
    Transform InteractionPoint { get; }
    float InteractionRange { get; }
    bool IsInteractable { get; }

    void Interact(GameObject interactor);
}
