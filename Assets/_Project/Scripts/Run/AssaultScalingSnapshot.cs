namespace Titanhold.Run
{
    public readonly struct AssaultScalingSnapshot
    {
        public AssaultScalingSnapshot(
            int instabilityPoints,
            int instabilityLevel,
            float healthMultiplier,
            float damageMultiplier)
        {
            InstabilityPoints = instabilityPoints;
            InstabilityLevel = instabilityLevel;
            HealthMultiplier = healthMultiplier;
            DamageMultiplier = damageMultiplier;
        }

        public int InstabilityPoints { get; }
        public int InstabilityLevel { get; }
        public float HealthMultiplier { get; }
        public float DamageMultiplier { get; }

        public static AssaultScalingSnapshot None => new AssaultScalingSnapshot(0, 0, 1f, 1f);
    }
}
