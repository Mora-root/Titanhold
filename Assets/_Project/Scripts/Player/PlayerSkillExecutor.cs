using System;
using System.Collections.Generic;
using Titanhold.Combat;
using UnityEngine;

public class PlayerSkillExecutor : MonoBehaviour
{
    [SerializeField] private SkillData skill1;

    private CharacterStats stats;
    private PlayerResource resource;
    private PlayerAnimator animator;

    private readonly Dictionary<SkillData, float> lastUseTimes = new();

    private SkillData currentSkill;
    private CombatActorReference combatActor;
    private CombatExecutionId currentExecutionId;
    private bool effectReleased;

    public bool IsUsingSkill { get; private set; }

    public event Action<CombatExecutionReport> ExecutionResolved;

    private void Awake()
    {
        stats = GetComponent<CharacterStats>();
        resource = GetComponent<PlayerResource>();
        animator = GetComponentInChildren<PlayerAnimator>();
        combatActor = new CombatActorReference($"player:{gameObject.GetEntityId()}", CombatActorKind.Player);
    }

    public bool TryUseSkill1()
    {
        return TryUseSkill(skill1);
    }

    private bool TryUseSkill(SkillData skill)
    {
        if (skill == null) return false;
        if (IsUsingSkill) return false;
        if (!IsCooldownReady(skill)) return false;

        if (animator == null)
        {
            Debug.LogError("PlayerAnimator not found on player children.");
            return false;
        }

        if (resource != null && !resource.TrySpend(skill.ResourceCost))
            return false;

        currentSkill = skill;
        currentExecutionId = CombatExecutionId.New();
        effectReleased = false;
        IsUsingSkill = true;

        lastUseTimes[skill] = Time.time;

        animator.PlaySkill(skill.AnimatorTrigger);

        return true;
    }

    private bool IsCooldownReady(SkillData skill)
    {
        if (!lastUseTimes.TryGetValue(skill, out float lastUseTime))
            return true;

        return Time.time >= lastUseTime + skill.Cooldown;
    }

    public void ApplyCurrentSkill()
    {
        if (!IsUsingSkill || currentSkill == null || effectReleased)
            return;

        effectReleased = true;
        ExecutionResolved?.Invoke(ApplyAreaDamage(currentSkill));
    }

    private CombatExecutionReport ApplyAreaDamage(SkillData skill)
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            skill.Radius,
            skill.TargetMask
        );

        HashSet<IDamageable> damagedTargets = new();

        float finalDamage = CombatDamageCalculator.GetSkillDamage(stats, skill);
        CombatExecutionId executionId = currentExecutionId.IsValid
            ? currentExecutionId
            : CombatExecutionId.New();
        string abilityId = string.IsNullOrWhiteSpace(skill.name)
            ? "legacy:unknown"
            : $"legacy:{skill.name}";
        currentExecutionId = executionId;
        List<DamageTargetResolution> resolutions = new List<DamageTargetResolution>();

        foreach (var hit in hits)
        {
            var damageable = hit.GetComponentInParent<IDamageable>();

            if (damageable == null)
                continue;

            if (damageable is Component damageableComponent &&
                damageableComponent.transform.root == transform.root)
                continue;

            if (damagedTargets.Contains(damageable))
                continue;

            damagedTargets.Add(damageable);
            DamageRequest request = new DamageRequest(
                executionId,
                combatActor,
                finalDamage,
                DamageCause.Ability,
                abilityId);
            DamageResult result = damageable.ApplyDamageRequest(request);
            resolutions.Add(new DamageTargetResolution(damageable, result));
        }

        return new CombatExecutionReport(executionId, resolutions);
    }

    public void FinishCurrentSkill()
    {
        IsUsingSkill = false;
        currentSkill = null;
        currentExecutionId = default;
        effectReleased = true;
    }

    private void OnDrawGizmosSelected()
    {
        if (skill1 == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, skill1.Radius);
    }
}
