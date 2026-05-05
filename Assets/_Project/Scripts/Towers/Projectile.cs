using UnityEngine;

/// <summary>
/// Simple projectile that moves forward and damages the first IDamageable it hits.
/// </summary>
public class Projectile : MonoBehaviour
{

    private ProjectileConfig _projectileConfig;
    private Rigidbody _rigidbody;
    private Transform _target;
    private float _timer;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        if (_rigidbody == null)
        {
            Debug.LogError("Rigidbody is missing on " + gameObject.name);
            enabled = false;
            return;
        }
    }

    private void FixedUpdate()
    {
        if (_projectileConfig == null) 
        {
             return;
        }
        if (_target != null)
        {
            Vector3 directionToTarget = (_target.position - _rigidbody.position).normalized;
            Quaternion targetRot = Quaternion.LookRotation(directionToTarget);
            _rigidbody.MoveRotation(Quaternion.Slerp(_rigidbody.rotation, targetRot, _projectileConfig.HomingStrength * Time.fixedDeltaTime)
            );
        }
        _rigidbody.MovePosition(_rigidbody.position + transform.forward * (_projectileConfig.Speed * Time.fixedDeltaTime));
        _timer -= Time.fixedDeltaTime;
        if (_timer <= 0)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(_projectileConfig.Damage);
            Destroy(gameObject);
        }
    }

    public void SetTarget(Transform target, ProjectileConfig config)
    {
        _target = target;
        _projectileConfig = config;
        _timer = _projectileConfig.Lifetime;
    }
}
