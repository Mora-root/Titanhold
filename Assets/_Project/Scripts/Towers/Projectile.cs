using UnityEngine;

/// <summary>
/// Simple projectile that moves forward and damages the first IDamageable it hits.
/// </summary>
public class Projectile : MonoBehaviour
{

    private ProjectileConfig projectileConfig;
    private Rigidbody rb;
    private Transform target;
    private float timer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody is missing on " + gameObject.name);
            enabled = false;
            return;
        }
    }

    private void FixedUpdate()
    {
        if (projectileConfig == null)
        {
             return;
        }
        if (target != null && !target.gameObject.activeInHierarchy)
        {
            target = null;
        }
        if (target != null)
        {
            Vector3 directionToTarget = (target.position - rb.position).normalized;
            Quaternion targetRot = Quaternion.LookRotation(directionToTarget);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, projectileConfig.HomingStrength * Time.fixedDeltaTime)
            );
        }
        rb.MovePosition(rb.position + transform.forward * (projectileConfig.Speed * Time.fixedDeltaTime));
        timer -= Time.fixedDeltaTime;
        if (timer <= 0)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(projectileConfig.Damage);
            Destroy(gameObject);
        }
    }

    public void SetTarget(Transform target, ProjectileConfig config)
    {
        this.target = target;
        projectileConfig = config;
        timer = projectileConfig.Lifetime;
    }
}
