using System;
using Titanhold.Combat;
using Titanhold.Combat.Abilities;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerAbilityExecutor : MonoBehaviour, IPlayerSkillCommands
{
    [SerializeField] private AreaDamageAbilityDefinition skill1;

    private CharacterStats stats;
    private PlayerResource resource;
    private PlayerAnimator playerAnimator;
    private Health health;
    private AbilityExecutionService execution;
    private AreaDamageAbilitySnapshot currentAbility;
    private CombatActorReference actor;

    public bool IsUsingSkill => execution?.CurrentExecution != null;
    public CombatActorReference ActorReference
    {
        get
        {
            if (!actor.IsValid)
                actor = new CombatActorReference($"player:{gameObject.GetEntityId()}", CombatActorKind.Player);
            return actor;
        }
    }

    public event Action<CombatExecutionReport> ExecutionResolved;

    private void Awake()
    {
        stats = GetComponent<CharacterStats>();
        resource = GetComponent<PlayerResource>();
        playerAnimator = GetComponentInChildren<PlayerAnimator>();
        health = GetComponent<Health>();
        execution = new AbilityExecutionService(ActorReference,
            resource != null ? new ResourceGateway(resource) : null);
    }

    public bool TryUseSkillSlot(int slotIndex)
    {
        if (!isActiveAndEnabled || execution == null || slotIndex != 0 || skill1 == null ||
            IsUsingSkill || (health != null && !health.IsAlive) || playerAnimator == null ||
            !skill1.TryCreateSnapshot(CombatDamageCalculator.GetGlobalDamage(stats), out var ability) ||
            !playerAnimator.CanPlaySkill(ability.AnimatorTrigger))
            return false;

        using (resource != null ? resource.DeferNotifications() : null)
        {
            AbilityExecutionResult result = execution.TryCommit(
                CombatExecutionId.New(), ability.Execution, Time.timeAsDouble);
            if (!result.Success)
                return false;

            currentAbility = ability;
            playerAnimator.PlaySkill(ability.AnimatorTrigger);
        }

        return true;
    }

    private void Update()
    {
        if (!IsUsingSkill)
            return;
        if (health != null && !health.IsAlive)
        {
            CancelCurrentSkill();
            return;
        }

        // Scaled simulation time freezes with solo pause. Animation events never
        // authorize damage or completion on this path.
        double now = Time.timeAsDouble;
        AbilityExecutionSnapshot active = execution.CurrentExecution;
        if (execution.Phase == AbilityExecutionPhase.Committed && now >= active.ReleaseAt)
        {
            AreaDamageAbilitySnapshot ability = currentAbility;
            AbilityExecutionResult release = execution.TryRelease(active.ExecutionId, now);
            if (release.Success)
            {
                CombatExecutionReport report = AreaDamageAbilityEffect.Apply(transform, release.Execution, ability);
                ExecutionResolved?.Invoke(report);
            }
        }

        // Report subscribers can cancel the cast; retain the original id so a
        // subsequent execution cannot accidentally be finished by this tick.
        if (execution.TryFinish(active.ExecutionId, now).Success)
            currentAbility = null;
    }

    public void CancelCurrentSkill()
    {
        if (IsUsingSkill && execution.TryCancel(
                execution.CurrentExecution.ExecutionId, Time.timeAsDouble).Success)
            currentAbility = null;
    }

    private void OnDisable() => CancelCurrentSkill();

    private sealed class ResourceGateway : IAbilityResourceGateway
    {
        private readonly PlayerResource resource;
        public ResourceGateway(PlayerResource resource) => this.resource = resource;
        public bool TrySpend(float amount) => resource != null && resource.TrySpend(amount);
    }
}
