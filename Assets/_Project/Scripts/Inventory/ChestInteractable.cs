using System;
using UnityEngine;

public sealed class ChestInteractable : MonoBehaviour, ISelectable, IInteractable, IHoverable
{
    [SerializeField] private ChestInventory chestInventory;
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private float interactionRange = 2f;
    [SerializeField] private bool isInteractable = true;
    [SerializeField] private GameObject hoverState;

    private TargetVisual visual;

    public event Action HoverEntered;
    public event Action HoverExited;

    public ChestInventory Inventory
    {
        get
        {
            chestInventory ??= GetComponent<ChestInventory>();
            return chestInventory;
        }
    }

    public bool IsSelectable => isInteractable;
    public Transform InteractionPoint => interactionPoint != null ? interactionPoint : transform;
    public float InteractionRange => interactionRange;
    public bool IsInteractable => isInteractable && Inventory != null;

    private void Awake()
    {
        chestInventory ??= GetComponent<ChestInventory>();
        visual = GetComponent<TargetVisual>();
        visual ??= GetComponentInChildren<TargetVisual>();

        if (hoverState != null)
            hoverState.SetActive(false);
    }

    public void OnSelected()
    {
        visual?.SetSelected(true);
    }

    public void OnDeselected()
    {
        visual?.SetSelected(false);
    }

    public void OnHoverEnter()
    {
        if (!IsInteractable)
            return;

        visual?.SetHover(true);

        if (hoverState != null)
            hoverState.SetActive(true);

        HoverEntered?.Invoke();
    }

    public void OnHoverExit()
    {
        visual?.SetHover(false);

        if (hoverState != null)
            hoverState.SetActive(false);

        HoverExited?.Invoke();
    }

    public void Interact(GameObject interactor)
    {
        if (!IsInteractable || interactor == null)
            return;

        PlayerChestInteractionController controller = interactor.GetComponent<PlayerChestInteractionController>();
        controller ??= interactor.GetComponentInParent<PlayerChestInteractionController>();

        if (controller == null)
        {
            Debug.LogWarning($"{nameof(ChestInteractable)} could not find PlayerChestInteractionController on interactor '{interactor.name}'.", this);
            return;
        }

        controller.OpenChest(this);
    }
}
