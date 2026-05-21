using System.Collections;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private float damage = 10f;

    private float lastAttackTime;
    private float multiplierDamageRadius = 1.5f;
    private ITargetable currentTarget;

    private PlayerAnimator animator;

    private bool isAttacking;
    public bool IsAttacking => isAttacking;

    public float AttackRange => attackRange;

    private void Awake()
    {
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
        float distance = Vector3.Distance(
            transform.position,
            currentTarget.AimPoint.position
        );

        // Damage radius
        if (distance > attackRange + multiplierDamageRadius)
            return;
        var damageable = currentTarget.AimPoint.GetComponentInParent<IDamageable>();
        damageable?.TakeDamage(damage);

    }

    // Called fron animation (Event)
    public void OnAttackFinished()
    {
        isAttacking = false;
    }
}
