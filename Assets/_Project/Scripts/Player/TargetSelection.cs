using System;
using UnityEngine;

/// <summary>
/// Receives target for UI
/// </summary>
public class TargetSelection : MonoBehaviour
{
    public ISelectable CurrentSelection { get; private set; }

    public event Action<ISelectable> OnSelected;
    public event Action OnCleared;

    public void Select(ISelectable selectable)
    {
        if (selectable == null || !selectable.IsSelectable)
            return;

        if (CurrentSelection == selectable)
            return;

        Clear();

        CurrentSelection = selectable;
        CurrentSelection.OnSelected();
        OnSelected?.Invoke(CurrentSelection);
    }

    public void Clear()
    {
        if (CurrentSelection == null)
            return;

        CurrentSelection.OnDeselected();
        CurrentSelection = null;

        OnCleared?.Invoke();
    }
}
