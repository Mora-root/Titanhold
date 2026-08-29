namespace Titanhold.Combat
{
    public enum DamageCause
    {
        Unknown,
        BasicAttack,
        Ability,
        Periodic,
        Environment
    }

    public readonly struct DamageRequest
    {
        public DamageRequest(
            CombatExecutionId executionId,
            CombatActorReference source,
            float rawDamage,
            DamageCause cause,
            string abilityId = null)
        {
            ExecutionId = executionId;
            Source = source;
            RawDamage = rawDamage;
            Cause = cause;
            AbilityId = abilityId ?? string.Empty;
        }

        public CombatExecutionId ExecutionId { get; }
        public CombatActorReference Source { get; }
        public float RawDamage { get; }
        public DamageCause Cause { get; }
        public string AbilityId { get; }

        public static DamageRequest CreateUnattributed(float rawDamage)
        {
            return new DamageRequest(
                CombatExecutionId.New(),
                CombatActorReference.Unknown,
                rawDamage,
                DamageCause.Unknown);
        }
    }
}
