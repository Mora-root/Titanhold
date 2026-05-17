using UnityEngine;

public class EnemyCombat : MonoBehaviour
{
    [SerializeField] private EnemyConfig config;

    private EnemyAnimator animator;

    public float EnemyAttackCooldown => config.EnemyAttackCooldown;
    public float EnemyAttackRange => config.EnemyAttackRange;

    private float lastAttackTime;
    private ITargetable currentTarget;

    private void Awake()
    {
        animator = GetComponentInChildren<EnemyAnimator>();
    }

    public bool CanAttack()
    {
        return Time.time >= lastAttackTime + EnemyAttackCooldown;
    }

    public void Attack(ITargetable target)
    {
        lastAttackTime = Time.time;
        currentTarget = target;
        animator.PlayAttack();
    }
    public void DoDamage()
    {
        if (currentTarget == null) return;

        float distance = Vector3.Distance(
            transform.position,
            currentTarget.AimPoint.position
        );

        if (distance > EnemyAttackRange)
            return;

        if (currentTarget is IDamageable damageable)
        {
            damageable.TakeDamage(config.DamageToPlayer);
        }
    }
}
