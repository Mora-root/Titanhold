using System;
using UnityEngine;

/// <summary>
/// Selects target for action and UI
/// </summary>
public class PlayerTargeting : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask selectableMask;
    [SerializeField] private float maxDistance = 100f;

    private void Awake()
    {
        if (cam == null)
            cam = Camera.main;
    }

    public ISelectable GetSelectableUnderMouse()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance, selectableMask);

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            var selectable = hit.collider.GetComponentInParent<ISelectable>();

            if (selectable != null && selectable.IsSelectable)
                return selectable;
        }

        return null;
    }
}
