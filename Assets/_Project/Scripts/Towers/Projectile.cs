using UnityEngine;

/// <summary>
/// Simple projectile that moves forward and damages the first IDamageable it hits.
/// </summary>
public class Projectile : MonoBehaviour
{
    [SerializeField] private ProjectileConfig _projectileConfig;

    private Rigidbody _rigidbody;
    private Transform _target;
    private float _timer;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();

        if (_projectileConfig == null)
        {
            Debug.LogError("ProjectileConfig is not assigned on " + gameObject.name);
            enabled = false;
        }
    }

    private void Update()
    {
        if (_target != null)
        {
            Vector3 directionToTarget = (_target.position - _rigidbody.position).normalized;
            Quaternion targetRot = Quaternion.LookRotation(directionToTarget);
            _rigidbody.MoveRotation(Quaternion.Slerp(transform.rotation, targetRot, _projectileConfig.HomingStrength * Time.deltaTime));
        }
        _rigidbody.MovePosition(_rigidbody.position + transform.forward * (_projectileConfig.Speed * Time.deltaTime));
        _timer += Time.deltaTime;
        if (_timer >= _projectileConfig.Lifetime)
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

    public void SetTarget(Transform target)
    {
        _target = target;
    }
}
