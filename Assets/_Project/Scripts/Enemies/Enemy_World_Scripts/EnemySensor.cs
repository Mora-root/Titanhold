using UnityEngine;

public class EnemySensor : MonoBehaviour
{
    private const int MaxDetectedColliders = 32;

    [SerializeField] private float aggroRange = 10f;
    [SerializeField] private LayerMask mask;

    private readonly Collider[] hits = new Collider[MaxDetectedColliders];

    public float DetectionRange => aggroRange;

    internal void SetDetectionRange(float detectionRange)
    {
        aggroRange = detectionRange;
    }

    public ITargetable GetTarget()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            aggroRange,
            hits,
            mask);

        ITargetable best = null;
        float bestDistanceSqr = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = hits[i];
            hits[i] = null;
            var target = hit.GetComponentInParent<ITargetable>();

            if (target == null || !target.IsTargetable)
                continue;

            // We don't choose ourselves
            if (target.AimPoint.root == transform)
                continue;

            float distanceSqr =
                (target.AimPoint.position - transform.position).sqrMagnitude;

            if (distanceSqr < bestDistanceSqr)
            {
                best = target;
                bestDistanceSqr = distanceSqr;
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
