using UnityEngine;

/// <summary>
/// Basic tower that finds the nearest enemy in range, rotates towards it, and fires projectiles.
/// </summary>
public class Tower : MonoBehaviour, ITargetProvider
{
    [SerializeField] private TowerConfig _towerConfig;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private float _fireAngle = 5f;

    private Transform _transform;
    private float _fireCooldown;
    private Transform _currentTarget;
    public bool HasTarget => _currentTarget != null;

    private void Awake()
    {
        _transform = transform;
        if (_towerConfig == null)
        {
            Debug.LogError("TowerConfig is not assigned on " + gameObject.name);
            enabled = false;
        }
    }

    private void Update()
    {
        FindTarget();
        if (HasTarget)
        {
            RotateTowardsTarget();
            TryFire();
        }
    }

    private void FindTarget()
    {
        Collider[] hits = Physics.OverlapSphere(_transform.position, _towerConfig.Range);
        float closestDistance = float.MaxValue;
        Transform closestAimPoint = null;

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<ITargetable>(out var targetable) && targetable.IsTargetable)
            {
                Transform aimPoint = targetable.AimPoint;
                if (aimPoint == null)
                {
                    continue;
                }

                float distance = Vector3.Distance(_transform.position, aimPoint.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
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
        _fireCooldown -= Time.deltaTime;
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
        return angle < _fireAngle;
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
