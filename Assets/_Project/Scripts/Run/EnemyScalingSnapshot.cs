namespace Titanhold.Run
{
    public readonly struct EnemyScalingSnapshot
    {
        public EnemyScalingSnapshot(
            int roundNumber,
            float healthMultiplier,
            float damageMultiplier)
        {
            RoundNumber = roundNumber;
            HealthMultiplier = healthMultiplier;
            DamageMultiplier = damageMultiplier;
        }

        public int RoundNumber { get; }
        public float HealthMultiplier { get; }
        public float DamageMultiplier { get; }

        public static EnemyScalingSnapshot Identity(int roundNumber)
        {
            return new EnemyScalingSnapshot(roundNumber, 1f, 1f);
        }
    }
}
