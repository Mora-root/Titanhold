using UnityEngine;

/// <summary>
/// Basic tower that finds the nearest enemy in range, rotates towards it, and fires projectiles.
/// </summary>
public class Tower : MonoBehaviour, ITargetProvider
{
    [SerializeField] private TowerConfig _towerConfig;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private int _targetScanBufferSize = 64;

    private Transform _transform;
    private float _fireCooldown;
    private Transform _currentTarget;
    private float _targetRefreshTimer;
    private Collider[] _targetHits;
    public bool HasTarget => _currentTarget != null;

    private void Awake()
    {
        _transform = transform;
        if (_towerConfig == null)
        {
            Debug.LogError("TowerConfig is not assigned on " + gameObject.name);
            enabled = false;
            return;
        }

        _targetHits = new Collider[Mathf.Max(1, _targetScanBufferSize)];
        _targetRefreshTimer = 0f;
    }

    private void Update()
    {
        _fireCooldown -= Time.deltaTime;
        _targetRefreshTimer -= Time.deltaTime;

        if (_targetRefreshTimer <= 0f || !HasTarget)
        {
            FindTarget();
            _targetRefreshTimer = Mathf.Max(0.01f, _towerConfig.TargetRefreshInterval);
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
            _transform.position,
            _towerConfig.Range,
            _targetHits,
            _towerConfig.TargetMask
        );

        float closestDistanceSqr = float.MaxValue;
        Transform closestAimPoint = null;

        for (int i = 0; i < hitCount; i++)
        {
            var hit = _targetHits[i];
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

                float distanceSqr = (aimPoint.position - _transform.position).sqrMagnitude;
                if (distanceSqr < closestDistanceSqr)
                {
                    closestDistanceSqr = distanceSqr;
                    closestAimPoint = aimPoint;
                }
            }
        }
        _currentTarget = closestAimPoint;
    }
    private void RotateTowardsTarget()
    {
        Vector3 direction = (_currentTarget.position - _firePoint.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        _firePoint.rotation = Quaternion.Slerp(_firePoint.rotation, targetRotation, _towerConfig.RotationSpeed * Time.deltaTime);
    }

    private void TryFire()
    {
        if (!IsFacingTarget())
        {
            return;
        }
        if (_fireCooldown <= 0)
        {
            Fire();
            _fireCooldown = 1f / _towerConfig.FireRate;
        }
    }

    private bool IsFacingTarget()
    {
        Vector3 directionToTarget = (_currentTarget.position - _firePoint.position).normalized;
        float angle = Vector3.Angle(_firePoint.forward, directionToTarget);
        return angle < _towerConfig.AimTolerance;
    }

    private void Fire()
    {
        if (_towerConfig.projectilePrefab == null || _towerConfig.projectileConfig == null)
        {
            Debug.LogError("Tower missing projectile prefab or config!");
            return;
        }
        GameObject projectileObj = Instantiate(_towerConfig.projectilePrefab, _firePoint.position, _firePoint.rotation);
        Projectile projectile = projectileObj.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.SetTarget(_currentTarget, _towerConfig.projectileConfig);
        }
        else
        {
            Debug.LogError("Projectile prefab is missing Projectile component!");
        }
    }

    public Transform GetTarget() => _currentTarget;

}
