using Titanhold.Combat;
using UnityEngine;

public class EnemyCombat : MonoBehaviour
{
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private float damage = 10f;

    private float lastAttackTime;
    private float multiplierDamageRadius = 3f;
    private ITargetable currentTarget;
    private CombatActorReference combatActor;
    private CombatExecutionId currentExecutionId;

    private EnemyAnimator animator;

    private bool isAttacking;
    public bool IsAttacking => isAttacking;

    public float AttackRange => attackRange;

    private void Awake()
    {
        animator = GetComponentInChildren<EnemyAnimator>();
        combatActor = new CombatActorReference($"enemy:{gameObject.GetEntityId()}", CombatActorKind.Enemy);
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
        currentExecutionId = CombatExecutionId.New();
        lastAttackTime = Time.time;

        isAttacking = true;

        animator.PlayAttack();
    }

    // Called fron animation (Event)
    public void ApplyDamage()
    {
        if (currentTarget == null) return;
        float distance = Vector3.Distance(
            transform.position,
            currentTarget.AimPoint.position
        );

        // Damage radius
        if (distance > attackRange * multiplierDamageRadius)
            return;
        var damageable = currentTarget.AimPoint.GetComponentInParent<IDamageable>();
        DamageRequest request = new DamageRequest(
            currentExecutionId.IsValid ? currentExecutionId : CombatExecutionId.New(),
            combatActor,
            damage,
            DamageCause.BasicAttack);
        damageable.ApplyDamageRequest(request);

    }

    // Called fron animation (Event)
    public void OnAttackFinished()
    {
        isAttacking = false;
        currentExecutionId = default;
    }
}
