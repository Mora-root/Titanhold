using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CampCore))]
[RequireComponent(typeof(Health))]
public sealed class CampCoreTarget : MonoBehaviour, ITargetable
{
    [SerializeField] private Transform aimPoint;
    [SerializeField] private CampCore campCore;
    [SerializeField] private Health health;

    public Transform AimPoint => aimPoint != null ? aimPoint : transform;

    public bool IsTargetable
    {
        get
        {
            if (campCore != null)
                return !campCore.IsDestroyed;

            return health != null && health.IsAlive;
        }
    }

    private void Awake()
    {
        campCore ??= GetComponent<CampCore>();
        health ??= GetComponent<Health>();
    }
}
