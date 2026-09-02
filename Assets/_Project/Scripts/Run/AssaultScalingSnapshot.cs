namespace Titanhold.Run
{
    public readonly struct AssaultScalingSnapshot
    {
        public AssaultScalingSnapshot(
            int instabilityPoints,
            int instabilityLevel,
            EnemyScalingSnapshot enemyScaling)
        {
            InstabilityPoints = instabilityPoints;
            InstabilityLevel = instabilityLevel;
            EnemyScaling = enemyScaling;
        }

        public int InstabilityPoints { get; }
        public int InstabilityLevel { get; }
        public EnemyScalingSnapshot EnemyScaling { get; }
        public int RoundNumber => EnemyScaling.RoundNumber;
        public float HealthMultiplier => EnemyScaling.HealthMultiplier;
        public float DamageMultiplier => EnemyScaling.DamageMultiplier;

        public static AssaultScalingSnapshot None =>
            NoneForRound(1);

        public static AssaultScalingSnapshot NoneForRound(int roundNumber)
        {
            return new AssaultScalingSnapshot(
                0,
                0,
                EnemyScalingSnapshot.Identity(roundNumber));
        }
    }
}
