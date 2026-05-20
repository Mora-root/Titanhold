using System;
using UnityEngine;

public class TargetSelection : MonoBehaviour
{
    public ISelectable CurrentTarget { get; private set; }

    public event Action<ISelectable> OnTargetSelected;
    public event Action OnTargetCleared;

    [SerializeField] private LayerMask selectableMask;

    private Camera cam;

    private void Awake()
    {
        cam = Camera.main;
    }

    public void HandleRightClick()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 200f, selectableMask))
        {
            var selectable = hit.collider.GetComponentInParent<ISelectable>();

            if (selectable != null)
            {
                SetTarget(selectable);
                return;
            }
        }

        ClearTarget();
    }

    private void SetTarget(ISelectable target)
    {
        if (CurrentTarget == target) return;

        CurrentTarget?.OnDeselected();

        CurrentTarget = target;
        CurrentTarget.OnSelected();

        OnTargetSelected?.Invoke(target);
    }

    public void ClearTarget()
    {
        if (CurrentTarget == null) return;

        CurrentTarget.OnDeselected();
        CurrentTarget = null;

        OnTargetCleared?.Invoke();
    }
}
