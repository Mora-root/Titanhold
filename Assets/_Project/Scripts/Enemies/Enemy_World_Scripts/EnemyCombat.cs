using Titanhold.Combat;
using UnityEngine;

public class EnemyCombat : MonoBehaviour
{
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private float damage = 10f;

    private float encounterDamageMultiplier = 1f;
    private CharacterStats definitionStats;
    private float definitionBaseAttacksPerSecond = 1f;
    private bool usesDefinitionStats;

    private float lastAttackTime;
    private float multiplierDamageRadius = 3f;
    private ITargetable currentTarget;
    private CombatActorReference combatActor;
    private CombatExecutionId currentExecutionId;

    private EnemyAnimator animator;

    private bool isAttacking;
    public bool IsAttacking => isAttacking;

    public float AttacksPerSecond
    {
        get
        {
            float baseRate = usesDefinitionStats
                ? definitionBaseAttacksPerSecond
                : GetLegacyBaseAttacksPerSecond();
            return Mathf.Max(
                0.01f,
                baseRate * GetAttackSpeedMultiplier());
        }
    }
    public float CurrentAttackCooldown => usesDefinitionStats
        ? 1f / AttacksPerSecond
        : attackCooldown;
    public float AttackRange
    {
        get
        {
            if (!usesDefinitionStats)
                return attackRange;

            return Mathf.Max(
                0.01f,
                definitionStats.GetValue(StatType.AttackRange));
        }
    }
    public float Damage
    {
        get
        {
            float baseDamage = usesDefinitionStats
                ? Mathf.Max(
                    0f,
                    definitionStats.GetValue(StatType.Damage))
                : damage;
            double scaled =
                (double)baseDamage * encounterDamageMultiplier;
            return scaled >= float.MaxValue
                ? float.MaxValue
                : (float)scaled;
        }
    }
    public float EncounterDamageMultiplier => encounterDamageMultiplier;

    private void Awake()
    {
        animator = GetComponentInChildren<EnemyAnimator>();
        combatActor = new CombatActorReference($"enemy:{gameObject.GetEntityId()}", CombatActorKind.Enemy);
    }

    // Check kd
    public bool CanAttack()
    {
        return Time.time >= lastAttackTime + CurrentAttackCooldown;
    }

    internal void UseDefinitionStats(
        CharacterStats configuredStats,
        float baseAttacksPerSecond)
    {
        definitionStats = configuredStats;
        definitionBaseAttacksPerSecond = baseAttacksPerSecond;
        usesDefinitionStats = true;
    }

    public bool TrySetEncounterDamageMultiplier(float multiplier)
    {
        if (multiplier <= 0f ||
            float.IsNaN(multiplier) ||
            float.IsInfinity(multiplier))
        {
            return false;
        }

        encounterDamageMultiplier = multiplier;
        return true;
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
        if (distance > AttackRange * multiplierDamageRadius)
            return;
        var damageable = currentTarget.AimPoint.GetComponentInParent<IDamageable>();
        DamageRequest request = new DamageRequest(
            currentExecutionId.IsValid ? currentExecutionId : CombatExecutionId.New(),
            combatActor,
            Damage,
            DamageCause.BasicAttack);
        damageable.ApplyDamageRequest(request);

    }

    // Called fron animation (Event)
    public void OnAttackFinished()
    {
        isAttacking = false;
        currentExecutionId = default;
    }

    private float GetLegacyBaseAttacksPerSecond()
    {
        return attackCooldown > 0f ? 1f / attackCooldown : 1f;
    }

    private float GetAttackSpeedMultiplier()
    {
        if (!usesDefinitionStats)
            return 1f;

        float attackSpeed =
            definitionStats.GetValue(StatType.AttackSpeed);
        return Mathf.Max(0.01f, attackSpeed / 100f);
    }
}
