using System;
using Titanhold.Combat;
using Titanhold.Combat.Abilities;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerAbilityExecutor :
    MonoBehaviour,
    IPlayerSkillCommands,
    IPlayerAbilitySlotBinding
{
    [SerializeField] private AreaDamageAbilityDefinition skill1;

    private CharacterStats stats;
    private PlayerResource resource;
    private PlayerAnimator playerAnimator;
    private Health health;
    private AbilityExecutionService execution;
    private IRuntimeAbilitySnapshot currentAbility;
    private ITargetable currentTarget;
    private CombatActorReference actor;
    private AbilitySlotDefinitionResolver abilitySlots;
    private ICombatResourceGateway sourceResourceGateway;

    public bool IsUsingSkill => execution?.CurrentExecution != null;
    public ITargetable CurrentTarget => currentTarget;
    public bool HasAbilitySlotBinding => abilitySlots != null;
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
        sourceResourceGateway =
            GetComponent(typeof(ICombatResourceGateway)) as
                ICombatResourceGateway;
        execution = new AbilityExecutionService(ActorReference,
            resource != null ? new ResourceGateway(resource) : null);
    }

    public PlayerSkillUseEvaluation EvaluateSkillSlot(
        int slotIndex,
        ITargetable selectedTarget)
    {
        if (!TryResolveAbility(
                slotIndex,
                out IRuntimeAbilityDefinition abilityDefinition) ||
            !isActiveAndEnabled || execution == null ||
            IsUsingSkill || (health != null && !health.IsAlive) ||
            playerAnimator == null)
        {
            return PlayerSkillUseEvaluation.Invalid;
        }

        AbilityCommitEvaluation evaluation =
            abilityDefinition.EvaluateUse(
                new AbilityUseContext(transform, selectedTarget));
        if (evaluation.IsReady)
        {
            return new PlayerSkillUseEvaluation(
                PlayerSkillUseStatus.Ready,
                evaluation.Status);
        }

        return evaluation.CanReposition
            ? new PlayerSkillUseEvaluation(
                PlayerSkillUseStatus.RequiresReposition,
                evaluation.Status)
            : new PlayerSkillUseEvaluation(
                PlayerSkillUseStatus.Invalid,
                evaluation.Status);
    }

    public bool TryUseSkillSlot(int slotIndex)
    {
        return TryUseSkillSlot(slotIndex, null);
    }

    public bool TryUseSkillSlot(
        int slotIndex,
        ITargetable selectedTarget)
    {
        if (!TryResolveAbility(slotIndex, out IRuntimeAbilityDefinition abilityDefinition) ||
            !isActiveAndEnabled || execution == null ||
            IsUsingSkill || (health != null && !health.IsAlive) || playerAnimator == null ||
            !abilityDefinition.TryCreateRuntimeSnapshot(
                new AbilityActorSnapshot(
                    CombatDamageCalculator.GetGlobalDamage(stats)),
                out IRuntimeAbilitySnapshot ability) ||
            !ability.CanCommit(new AbilityUseContext(transform, selectedTarget)) ||
            !playerAnimator.CanPlaySkill(ability.AnimatorTrigger))
            return false;

        using (resource != null ? resource.DeferNotifications() : null)
        {
            AbilityExecutionResult result = execution.TryCommit(
                CombatExecutionId.New(), ability.Execution, Time.timeAsDouble);
            if (!result.Success)
                return false;

            currentAbility = ability;
            currentTarget = selectedTarget;
            playerAnimator.PlaySkill(ability.AnimatorTrigger);
        }

        return true;
    }

    public bool TryBindAbilitySlots(
        IAbilitySlotSource slots,
        IAbilityDefinitionResolver definitions)
    {
        if (slots == null || definitions == null || IsUsingSkill)
            return false;

        try
        {
            abilitySlots = new AbilitySlotDefinitionResolver(slots, definitions);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public bool TryClearAbilitySlotBinding()
    {
        if (IsUsingSkill)
            return false;

        abilitySlots = null;
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
            IRuntimeAbilitySnapshot ability = currentAbility;
            AbilityExecutionResult release = execution.TryRelease(active.ExecutionId, now);
            if (release.Success)
            {
                CombatExecutionReport report = ability.Release(
                    new AbilityUseContext(transform, currentTarget),
                    release.Execution,
                    now);
                AbilitySourceResourceGainResolver.TryApply(
                    ability,
                    release.Execution.ExecutionId,
                    report,
                    sourceResourceGateway);
                ExecutionResolved?.Invoke(report);
            }
        }

        // Report subscribers can cancel the cast; retain the original id so a
        // subsequent execution cannot accidentally be finished by this tick.
        if (execution.TryFinish(active.ExecutionId, now).Success)
            ClearCurrentAbility();
    }

    public void CancelCurrentSkill()
    {
        if (IsUsingSkill && execution.TryCancel(
                execution.CurrentExecution.ExecutionId, Time.timeAsDouble).Success)
            ClearCurrentAbility();
    }

    private void OnDisable() => CancelCurrentSkill();

    private bool TryResolveAbility(
        int slotIndex,
        out IRuntimeAbilityDefinition definition)
    {
        definition = null;
        if (abilitySlots == null)
        {
            if (slotIndex != 0 || skill1 == null)
                return false;

            definition = skill1;
            return true;
        }

        return abilitySlots.TryResolve(
                   slotIndex,
                   out IAbilityDefinition resolved) &&
               (definition = resolved as IRuntimeAbilityDefinition) != null;
    }

    private void ClearCurrentAbility()
    {
        currentAbility = null;
        currentTarget = null;
    }

    private sealed class ResourceGateway : IAbilityResourceGateway
    {
        private readonly PlayerResource resource;
        public ResourceGateway(PlayerResource resource) => this.resource = resource;
        public bool TrySpend(float amount) => resource != null && resource.TrySpend(amount);
    }
}
