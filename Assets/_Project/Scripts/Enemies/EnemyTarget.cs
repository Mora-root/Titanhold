using UnityEngine;

public class EnemyTarget : MonoBehaviour, ITargetable
{
    [SerializeField] private Transform aimPoint;

    private Health health;

    public Transform AimPoint => aimPoint != null ? aimPoint : transform;

    public bool IsTargetable => health != null && health.IsAlive;

    private void Awake()
    {
        health = GetComponent<Health>();
    }
}
