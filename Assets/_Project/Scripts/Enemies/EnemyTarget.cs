using UnityEngine;

/// <summary>
/// Сomponent that makes the enemy accessible to targetSelection and hover systems
/// </summary>
public class EnemyTarget : MonoBehaviour, ITargetable, ISelectable, IHoverable
{
    [SerializeField] private Transform aimPoint;

    private Health health;
    private TargetVisual visual;

    public Transform AimPoint => aimPoint != null ? aimPoint : transform;

    public bool IsSelectable => health != null && health.IsAlive;
    public bool IsTargetable => health != null && health.IsAlive;

    private void Awake()
    {
        health = GetComponent<Health>();
        visual = GetComponent<TargetVisual>();
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
        visual?.SetHover(true);
    }

    public void OnHoverExit()
    {
        visual?.SetHover(false);
    }
}
