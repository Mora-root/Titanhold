namespace Titanhold.Combat.Abilities
{
    public interface IAbilityResourceGateway
    {
        // Preflight must never mutate the balance or notify observers. Commit
        // repeats the check through TrySpend because availability can change
        // between command acceptance and execution.
        bool CanSpend(float amount);

        // A rejected spend must leave the resource unchanged. Implementations must
        // defer observer notifications until the enclosing ability command returns,
        // so observers see both the committed cast and its resource cost together.
        bool TrySpend(float amount);
    }

    public enum AbilityExecutionPhase
    {
        Idle,
        Committed,
        Released
    }

    public enum AbilityExecutionError
    {
        None,
        InvalidDefinition,
        InvalidExecutionId,
        InvalidTime,
        Busy,
        DuplicateExecution,
        OnCooldown,
        MissingResourceGateway,
        InsufficientResource,
        NoActiveExecution,
        WrongExecution,
        NotReady,
        AlreadyReleased,
        EffectNotReleased,
        ReentrantCommand
    }

    public readonly struct AbilityCooldownSnapshot
    {
        public AbilityCooldownSnapshot(
            string abilityId,
            double duration,
            double remaining)
        {
            AbilityId = abilityId ?? string.Empty;
            Duration = duration;
            Remaining = remaining;
        }

        public string AbilityId { get; }
        public double Duration { get; }
        public double Remaining { get; }
        public bool IsCoolingDown => Remaining > 0d;
        public double NormalizedRemaining => Duration > 0d
            ? System.Math.Min(
                1d,
                System.Math.Max(0d, Remaining / Duration))
            : 0d;
        public bool IsValid =>
            !string.IsNullOrWhiteSpace(AbilityId) &&
            AbilityExecutionDefinition.IsNonNegativeFinite(Duration) &&
            AbilityExecutionDefinition.IsNonNegativeFinite(Remaining) &&
            Remaining <= Duration;
    }

    public sealed class AbilityExecutionSnapshot
    {
        internal AbilityExecutionSnapshot(
            CombatExecutionId executionId,
            CombatActorReference actor,
            AbilityExecutionDefinition definition,
            double committedAt,
            double releaseAt,
            double finishAt)
        {
            ExecutionId = executionId;
            Actor = actor;
            Definition = definition;
            CommittedAt = committedAt;
            ReleaseAt = releaseAt;
            FinishAt = finishAt;
        }

        public CombatExecutionId ExecutionId { get; }
        public CombatActorReference Actor { get; }
        public AbilityExecutionDefinition Definition { get; }
        public double CommittedAt { get; }
        public double ReleaseAt { get; }
        public double FinishAt { get; }
    }

    public readonly struct AbilityExecutionResult
    {
        private AbilityExecutionResult(
            bool success,
            AbilityExecutionError error,
            AbilityExecutionSnapshot execution)
        {
            Success = success;
            Error = error;
            Execution = execution;
        }

        public bool Success { get; }
        public AbilityExecutionError Error { get; }
        public AbilityExecutionSnapshot Execution { get; }

        internal static AbilityExecutionResult Succeeded(AbilityExecutionSnapshot execution)
        {
            return new AbilityExecutionResult(true, AbilityExecutionError.None, execution);
        }

        internal static AbilityExecutionResult Failed(AbilityExecutionError error)
        {
            return new AbilityExecutionResult(false, error, null);
        }
    }
}
