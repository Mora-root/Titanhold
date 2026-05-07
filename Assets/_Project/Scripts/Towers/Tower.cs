using UnityEngine;

/// <summary>
/// Basic tower that finds the nearest enemy in range, rotates towards it, and fires projectiles.
/// </summary>
public class Tower : MonoBehaviour, ITargetProvider
{
    [SerializeField] private TowerConfig towerConfig;
    [SerializeField] private Transform firePoint;
    [SerializeField] private int targetScanBufferSize = 64;

    private float fireCooldown;
    private Transform currentTarget;
    private float targetRefreshTimer;
    private Collider[] targetHits;
    public bool HasTarget => currentTarget != null;

    private void Awake()
    {
        if (towerConfig == null)
        {
            Debug.LogError("TowerConfig is not assigned on " + gameObject.name);
            enabled = false;
            return;
        }

        targetHits = new Collider[Mathf.Max(1, targetScanBufferSize)];
        targetRefreshTimer = 0f;
    }

    private void Update()
    {

        if (towerConfig.FireRate == 0)
        {
            return;
        }

        fireCooldown -= Time.deltaTime;
        targetRefreshTimer -= Time.deltaTime;

        if (targetRefreshTimer <= 0f || !HasTarget)
        {
            FindTarget();
            targetRefreshTimer = Mathf.Max(0.01f, towerConfig.TargetRefreshInterval);
        }
        if (HasTarget)
        {
            RotateTowardsTarget();
            TryFire();
        }
    }

    private void FindTarget()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            towerConfig.Range,
            targetHits,
            towerConfig.TargetMask
        );

        float closestDistanceSqr = float.MaxValue;
        Transform closestAimPoint = null;

        for (int i = 0; i < hitCount; i++)
        {
            var hit = targetHits[i];
            if (hit == null)
            {
                continue;
            }

            if (hit.TryGetComponent<ITargetable>(out var targetable) && targetable.IsTargetable)
            {
                Transform aimPoint = targetable.AimPoint;
                if (aimPoint == null)
                {
                    continue;
                }

                float distanceSqr = (aimPoint.position - transform.position).sqrMagnitude;
                if (distanceSqr < closestDistanceSqr)
                {
                    closestDistanceSqr = distanceSqr;
                    closestAimPoint = aimPoint;
                }
            }
        }
        currentTarget = closestAimPoint;
    }
    private void RotateTowardsTarget()
    {
        Vector3 direction = (currentTarget.position - firePoint.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        firePoint.rotation = Quaternion.Slerp(firePoint.rotation, targetRotation, towerConfig.RotationSpeed * Time.deltaTime);
    }

    private void TryFire()
    {
        if (!IsFacingTarget())
        {
            return;
        }
        if (fireCooldown <= 0)
        {
            Fire();
            fireCooldown = 1f / towerConfig.FireRate;
        }
    }

    private bool IsFacingTarget()
    {
        Vector3 directionToTarget = (currentTarget.position - firePoint.position).normalized;
        float angle = Vector3.Angle(firePoint.forward, directionToTarget);
        return angle < towerConfig.AimTolerance;
    }

    private void Fire()
    {
        if (towerConfig.projectilePrefab == null || towerConfig.projectileConfig == null)
        {
            Debug.LogError("Tower missing projectile prefab or config!");
            return;
        }
        GameObject projectileObj = Instantiate(towerConfig.projectilePrefab, firePoint.position, firePoint.rotation);
        Projectile projectile = projectileObj.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.SetTarget(currentTarget, towerConfig.projectileConfig);
        }
        else
        {
            Debug.LogError("Projectile prefab is missing Projectile component!");
        }
    }

    public Transform GetTarget() => currentTarget;

}
