using UnityEngine;

public class EnemyTarget : MonoBehaviour, ITargetable, ISelectable, IHoverable
{
    [SerializeField] private Transform aimPoint;

    private Health health;
    private TargetVisual visual;

    public Transform AimPoint => aimPoint != null ? aimPoint : transform;
    public bool IsTargetable => health != null && health.IsAlive;

    private void Awake()
    {
        health = GetComponent<Health>();
        visual = GetComponent<TargetVisual>();
    }
    // 🔥 SELECT
    public void OnSelected()
    {
        visual?.SetSelected(true);
    }

    public void OnDeselected()
    {
        visual?.SetSelected(false);
    }

    // 🔥 HOVER
    public void OnHoverEnter()
    {
        visual?.SetHover(true);
    }

    public void OnHoverExit()
    {
        visual?.SetHover(false);
    }
}
