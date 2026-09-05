using System;
using System.Collections.Generic;

namespace Titanhold.Combat.Abilities
{
    // One instance per actor for an encounter/run lifetime. The authoritative caller
    // supplies simulation time and globally unique execution ids. No Unity clock,
    // animation event, input, target query or damage application lives here.
    public sealed class AbilityExecutionService
    {
        private readonly IAbilityResourceGateway resources;
        private readonly Dictionary<string, double> cooldownEnds = new(StringComparer.Ordinal);
        private readonly HashSet<CombatExecutionId> committedExecutions = new();
        private double lastAcceptedTime;
        private bool isCommitting;

        public AbilityExecutionService(
            CombatActorReference actor,
            IAbilityResourceGateway resources = null)
        {
            if (!actor.IsValid)
                throw new ArgumentException("An ability runtime requires a valid actor.", nameof(actor));

            Actor = actor;
            this.resources = resources;
        }

        public CombatActorReference Actor { get; }
        public AbilityExecutionPhase Phase { get; private set; }
        public AbilityExecutionSnapshot CurrentExecution { get; private set; }

        public AbilityExecutionResult TryCommit(
            CombatExecutionId executionId,
            AbilityExecutionDefinition definition,
            double now)
        {
            if (isCommitting)
                return Fail(AbilityExecutionError.ReentrantCommand);
            if (definition == null)
                return Fail(AbilityExecutionError.InvalidDefinition);
            if (!executionId.IsValid)
                return Fail(AbilityExecutionError.InvalidExecutionId);
            if (!IsValidTime(now))
                return Fail(AbilityExecutionError.InvalidTime);
            if (committedExecutions.Contains(executionId))
                return Fail(AbilityExecutionError.DuplicateExecution);
            if (CurrentExecution != null)
                return Fail(AbilityExecutionError.Busy);
            if (cooldownEnds.TryGetValue(definition.AbilityId, out double readyAt) && now < readyAt)
                return Fail(AbilityExecutionError.OnCooldown);

            double releaseAt = now + definition.WindUp;
            double finishAt = releaseAt + definition.Recovery;
            double cooldownEnd = now + definition.Cooldown;
            if (!AbilityExecutionDefinition.IsNonNegativeFinite(finishAt) ||
                !AbilityExecutionDefinition.IsNonNegativeFinite(cooldownEnd))
                return Fail(AbilityExecutionError.InvalidTime);
            if (definition.ResourceCost > 0f && resources == null)
                return Fail(AbilityExecutionError.MissingResourceGateway);

            AbilityExecutionSnapshot execution = new(
                executionId, Actor, definition, now, releaseAt, finishAt);

            isCommitting = true;
            try
            {
                if (definition.ResourceCost > 0f && !resources.TrySpend(definition.ResourceCost))
                    return Fail(AbilityExecutionError.InsufficientResource);

                cooldownEnds[definition.AbilityId] = cooldownEnd;
                committedExecutions.Add(executionId);
                CurrentExecution = execution;
                Phase = AbilityExecutionPhase.Committed;
                lastAcceptedTime = now;
                return AbilityExecutionResult.Succeeded(execution);
            }
            finally
            {
                isCommitting = false;
            }
        }

        // Only a successful result authorizes the caller to emit the effect.
        // A delayed/stale callback must retain its original execution id.
        public AbilityExecutionResult TryRelease(CombatExecutionId executionId, double now)
        {
            AbilityExecutionError error = ValidateActiveCommand(executionId, now);
            if (error != AbilityExecutionError.None)
                return Fail(error);
            if (Phase == AbilityExecutionPhase.Released)
                return Fail(AbilityExecutionError.AlreadyReleased);
            if (now < CurrentExecution.ReleaseAt)
                return Fail(AbilityExecutionError.NotReady);

            Phase = AbilityExecutionPhase.Released;
            lastAcceptedTime = now;
            return AbilityExecutionResult.Succeeded(CurrentExecution);
        }

        public AbilityExecutionResult TryFinish(CombatExecutionId executionId, double now)
        {
            AbilityExecutionError error = ValidateActiveCommand(executionId, now);
            if (error != AbilityExecutionError.None)
                return Fail(error);
            if (Phase != AbilityExecutionPhase.Released)
                return Fail(AbilityExecutionError.EffectNotReleased);
            if (now < CurrentExecution.FinishAt)
                return Fail(AbilityExecutionError.NotReady);

            lastAcceptedTime = now;
            return ClearCurrentExecution();
        }

        // Application rules decide whether a voluntary/hard interrupt is eligible.
        // Cancellation never refunds the committed cost or resets its cooldown.
        public AbilityExecutionResult TryCancel(CombatExecutionId executionId, double now)
        {
            AbilityExecutionError error = ValidateActiveCommand(executionId, now);
            if (error != AbilityExecutionError.None)
                return Fail(error);

            lastAcceptedTime = now;
            return ClearCurrentExecution();
        }

        private AbilityExecutionError ValidateActiveCommand(CombatExecutionId executionId, double now)
        {
            if (isCommitting)
                return AbilityExecutionError.ReentrantCommand;
            if (!executionId.IsValid)
                return AbilityExecutionError.InvalidExecutionId;
            if (!IsValidTime(now))
                return AbilityExecutionError.InvalidTime;
            if (CurrentExecution == null)
                return AbilityExecutionError.NoActiveExecution;
            if (CurrentExecution.ExecutionId != executionId)
                return AbilityExecutionError.WrongExecution;

            return AbilityExecutionError.None;
        }

        private AbilityExecutionResult ClearCurrentExecution()
        {
            AbilityExecutionSnapshot execution = CurrentExecution;
            CurrentExecution = null;
            Phase = AbilityExecutionPhase.Idle;
            return AbilityExecutionResult.Succeeded(execution);
        }

        private bool IsValidTime(double now)
        {
            return AbilityExecutionDefinition.IsNonNegativeFinite(now) && now >= lastAcceptedTime;
        }

        private static AbilityExecutionResult Fail(AbilityExecutionError error)
        {
            return AbilityExecutionResult.Failed(error);
        }
    }
}
