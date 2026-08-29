namespace Titanhold.Combat
{
    public readonly struct DeathContext
    {
        public DeathContext(DamageRequest killingDamage, float appliedDamage)
        {
            KillingDamage = killingDamage;
            AppliedDamage = appliedDamage;
        }

        public DamageRequest KillingDamage { get; }
        public float AppliedDamage { get; }
        public CombatExecutionId ExecutionId => KillingDamage.ExecutionId;
        public CombatActorReference Source => KillingDamage.Source;
        public bool IsPlayerAttributed => Source.IsValid && Source.IsPlayer;
        public bool IsValid => ExecutionId.IsValid;
    }
}
