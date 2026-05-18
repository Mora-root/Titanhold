using System.Collections;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private float damage = 10f;

    private float lastAttackTime;
    private ITargetable currentTarget;

    private PlayerAnimator animator;

    private bool isAttacking;
    public bool IsAttacking => isAttacking;

    public float AttackRange => attackRange;

    private void Awake()
    {
        animator = GetComponentInChildren<PlayerAnimator>();
    }

    // 🔥 проверка кд
    public bool CanAttack()
    {
        return Time.time >= lastAttackTime + attackCooldown;
    }

    // 🔥 попытка атаки (вызывается из State)
    public void TryAttack(ITargetable target)
    {
        if (!CanAttack()) return;
        if (target == null || !target.IsTargetable) return;
        Debug.Log("Can attack target");
        currentTarget = target;
        lastAttackTime = Time.time;

        isAttacking = true;

        animator.PlayAttack();
    }

    // 🔥 вызывается ИЗ АНИМАЦИИ (Event)
    public void ApplyDamage()
    {
        if (currentTarget == null) return;
        Debug.Log("Have target");
        float distance = Vector3.Distance(
            transform.position,
            currentTarget.AimPoint.position
        );

        // 🔥 защита от "ударил в воздух"
        if (distance > attackRange + 0.3f)
            return;
        Debug.Log("Target in attack range");
        var damageable = currentTarget.AimPoint.GetComponentInParent<IDamageable>();
        damageable?.TakeDamage(damage);

    }

    // 🔥 вызывается ИЗ АНИМАЦИИ (Event)
    public void OnAttackFinished()
    {
        isAttacking = false;
    }
}
