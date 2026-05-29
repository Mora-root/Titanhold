using UnityEngine;

public sealed class CampCommandInteractable : MonoBehaviour, ISelectable, IInteractable, IHoverable
{
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private float interactionRange = 2f;
    [SerializeField] private CampDefenseWaveController waveController;
    [SerializeField] private CampBrokenState campBrokenState;

    private TargetVisual visual;

    public Transform InteractionPoint => interactionPoint != null ? interactionPoint : transform;
    public float InteractionRange => interactionRange;
    public bool IsInteractable => campBrokenState != null && campBrokenState.IsBroken || waveController != null && waveController.IsPending;
    public bool IsSelectable => IsInteractable;

    private void Awake()
    {
        interactionPoint ??= transform;
        waveController ??= GetComponent<CampDefenseWaveController>();
        campBrokenState ??= GetComponent<CampBrokenState>();
        visual = GetComponent<TargetVisual>();
        visual ??= GetComponentInChildren<TargetVisual>();
    }

    public void OnSelected() { }

    public void OnDeselected() { }

    public void OnHoverEnter()
    {
        if (IsInteractable)
            visual?.SetHover(true);
    }

    public void OnHoverExit()
    {
        visual?.SetHover(false);
    }

    public void Interact(GameObject interactor)
    {
        if (campBrokenState != null && campBrokenState.IsBroken)
        {
            campBrokenState.RestoreCamp();
            return;
        }

        if (waveController != null && waveController.IsPending)
        {
            waveController.StartWave();
            return;
        }
    }
}
