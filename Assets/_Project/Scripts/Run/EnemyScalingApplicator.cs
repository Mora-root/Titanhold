namespace Titanhold.Run
{
    public sealed class EnemyScalingApplicator
    {
        public EnemyScalingResult TryApply(
            Health health,
            EnemyCombat combat,
            EnemyScalingSnapshot snapshot,
            bool restoreFullHealth)
        {
            if (health == null)
                return EnemyScalingResult.Failed(EnemyScalingError.MissingHealth, snapshot);

            if (combat == null)
                return EnemyScalingResult.Failed(EnemyScalingError.MissingCombat, snapshot);

            if (snapshot.RoundNumber <= 0 ||
                !IsValidMultiplier(snapshot.HealthMultiplier) ||
                !IsValidMultiplier(snapshot.DamageMultiplier))
            {
                return EnemyScalingResult.Failed(EnemyScalingError.InvalidSnapshot, snapshot);
            }

            if (!health.TrySetEncounterMaxHealthMultiplier(snapshot.HealthMultiplier))
                return EnemyScalingResult.Failed(EnemyScalingError.HealthRejected, snapshot);

            if (!combat.TrySetEncounterDamageMultiplier(snapshot.DamageMultiplier))
            {
                health.TrySetEncounterMaxHealthMultiplier(1f);
                return EnemyScalingResult.Failed(EnemyScalingError.CombatRejected, snapshot);
            }

            if (restoreFullHealth)
                health.RestoreFull();

            return EnemyScalingResult.Succeeded(snapshot);
        }

        private static bool IsValidMultiplier(float multiplier)
        {
            return multiplier > 0f &&
                   !float.IsNaN(multiplier) &&
                   !float.IsInfinity(multiplier);
        }
    }
}
