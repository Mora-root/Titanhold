using UnityEngine;

public class EnemySensor : MonoBehaviour
{
    [SerializeField] private float aggroRange;
    [SerializeField] private LayerMask targetMask;

    public ITargetable CurrentTarget { get; private set; }

    public bool HasTarget => CurrentTarget != null;

    public void UpdateSensor()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, aggroRange, targetMask);

        foreach (var hit in hits)
        {
            var target = hit.GetComponentInParent<ITargetable>();

            if (target != null && target.IsTargetable)
            {
                CurrentTarget = target;
                return;
            }
        }
        CurrentTarget = null;
    }
}
