using System.Collections;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private float damage = 10f;

    private CharacterStats stats;
    private float lastAttackTime;
    private float multiplierDamageRadius = 1.5f;
    private ITargetable currentTarget;

    private PlayerAnimator animator;

    private bool isAttacking;
    public bool IsAttacking => isAttacking;
    public float AttackRange
    {
        get
        {
            if (stats != null)
            {
                float statValue = stats.GetValue(StatType.AttackRange);
                if (statValue > 0f)
                    return statValue;
            }

            return attackRange;
        }
    }
    public float Damage
    {
        get
        {
            if (stats != null)
            {
                float statValue = stats.GetValue(StatType.Damage);
                if (statValue > 0f)
                    return statValue;
            }

            return damage;
        }
    }

    private void Awake()
    {
        stats = GetComponent<CharacterStats>();
        animator = GetComponentInChildren<PlayerAnimator>();
    }

    // Check kd
    public bool CanAttack()
    {
        return Time.time >= lastAttackTime + attackCooldown;
    }

    // Try attack(called from State)
    public void TryAttack(ITargetable target)
    {
        if (isAttacking) return;
        if (!CanAttack()) return;
        if (target == null || !target.IsTargetable) return;

        currentTarget = target;
        lastAttackTime = Time.time;

        isAttacking = true;
        animator.PlayAttack();
    }

    // Called fron animation(Event)
    public void ApplyDamage()
    {
        if (currentTarget == null) return;
        if (!currentTarget.IsTargetable) return;

        float distance = Vector3.Distance(
            transform.position,
            currentTarget.AimPoint.position
        );

        // Damage radius
        if (distance > AttackRange * multiplierDamageRadius)
            return;

        var damageable = currentTarget.AimPoint.GetComponentInParent<IDamageable>();
        damageable?.TakeDamage(Damage);

    }

    // Called fron animation (Event)
    public void OnAttackFinished()
    {
        isAttacking = false;
    }
    public void CancelAttack()
    {
        isAttacking = false;
        currentTarget = null;
    }
}
