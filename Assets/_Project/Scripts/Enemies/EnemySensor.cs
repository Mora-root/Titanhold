using UnityEngine;

public class EnemySensor : MonoBehaviour
{
    [SerializeField] private float aggroRange = 10f;
    [SerializeField] private LayerMask mask;

    public ITargetable GetTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, aggroRange, mask);

        ITargetable best = null;
        float bestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            var target = hit.GetComponentInParent<ITargetable>();

            if (target == null || !target.IsTargetable)
                continue;

            // не выбираем себя
            if (target.AimPoint.root == transform)
                continue;

            float dist = Vector3.Distance(
                transform.position,
                target.AimPoint.position
            );

            if (dist < bestDist)
            {
                best = target;
                bestDist = dist;
            }
        }

        return best;
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, aggroRange);
    }
}
