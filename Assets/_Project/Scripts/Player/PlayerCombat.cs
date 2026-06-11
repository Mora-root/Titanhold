using System.Collections;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float attackCooldown = 1f;
    [SerializeField, Min(0.01f)] private float baseAttackAnimationDuration = 1f;
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private PlayerEquipmentRuntime equipmentRuntime;

    private CharacterStats stats;
    private float lastAttackTime;
    private float multiplierDamageRadius = 1.5f;
    private ITargetable currentTarget;

    private PlayerAnimator animator;

    private bool isAttacking;
    public bool IsAttacking => isAttacking;

    public float AttacksPerSecond
    {
        get
        {
            float baseAttacksPerSecond = GetBaseAttacksPerSecond();
            float attackSpeedMultiplier = GetAttackSpeedMultiplier();
            return Mathf.Max(0.01f, baseAttacksPerSecond * attackSpeedMultiplier);
        }
    }

    public float CurrentAttackCooldown => 1f / AttacksPerSecond;
    public float AttackAnimationPlaybackSpeed => Mathf.Max(0.01f, baseAttackAnimationDuration / CurrentAttackCooldown);

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
            return CombatDamageCalculator.GetGlobalDamage(stats, damage);
        }
    }

    private void Awake()
    {
        stats = GetComponent<CharacterStats>();
        if (equipmentRuntime == null)
            equipmentRuntime = GetComponent<PlayerEquipmentRuntime>();

        animator = GetComponentInChildren<PlayerAnimator>();
    }

    // Check kd
    public bool CanAttack()
    {
        return Time.time >= lastAttackTime + CurrentAttackCooldown;
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
        animator.PlayAttack(AttackAnimationPlaybackSpeed);
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
        animator?.ResetPlaybackSpeed();
    }

    private float GetBaseAttacksPerSecond()
    {
        ItemDefinition weaponDefinition = GetMainHandWeaponDefinition();
        if (weaponDefinition != null)
            return weaponDefinition.WeaponBaseAttacksPerSecond;

        return attackCooldown > 0f ? 1f / attackCooldown : 1f;
    }

    private float GetAttackSpeedMultiplier()
    {
        if (stats == null)
            return 1f;

        float statValue = stats.GetValue(StatType.AttackSpeed);
        return statValue > 0f ? statValue : 1f;
    }

    private ItemDefinition GetMainHandWeaponDefinition()
    {
        CharacterEquipment equipment = equipmentRuntime != null ? equipmentRuntime.Equipment : null;
        ItemInstance mainHand = equipment != null ? equipment.GetEquipped(EquipmentSlotId.MainHand) : null;
        ItemDefinition definition = mainHand != null ? mainHand.Definition : null;

        return definition != null && definition.IsWeapon ? definition : null;
    }
}
