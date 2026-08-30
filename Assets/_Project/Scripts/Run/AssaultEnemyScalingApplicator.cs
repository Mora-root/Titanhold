namespace Titanhold.Run
{
    public sealed class AssaultEnemyScalingApplicator
    {
        public AssaultEnemyScalingResult TryApply(
            Health health,
            EnemyCombat combat,
            AssaultScalingSnapshot snapshot)
        {
            if (health == null)
            {
                return AssaultEnemyScalingResult.Failed(
                    AssaultEnemyScalingError.MissingHealth,
                    snapshot);
            }

            if (combat == null)
            {
                return AssaultEnemyScalingResult.Failed(
                    AssaultEnemyScalingError.MissingCombat,
                    snapshot);
            }

            if (!IsValidMultiplier(snapshot.HealthMultiplier) ||
                !IsValidMultiplier(snapshot.DamageMultiplier))
            {
                return AssaultEnemyScalingResult.Failed(
                    AssaultEnemyScalingError.InvalidSnapshot,
                    snapshot);
            }

            if (!health.TrySetEncounterMaxHealthMultiplier(
                    snapshot.HealthMultiplier))
            {
                return AssaultEnemyScalingResult.Failed(
                    AssaultEnemyScalingError.HealthRejected,
                    snapshot);
            }

            if (!combat.TrySetEncounterDamageMultiplier(
                    snapshot.DamageMultiplier))
            {
                health.TrySetEncounterMaxHealthMultiplier(1f);
                return AssaultEnemyScalingResult.Failed(
                    AssaultEnemyScalingError.CombatRejected,
                    snapshot);
            }

            health.RestoreFull();
            return AssaultEnemyScalingResult.Succeeded(snapshot);
        }

        private static bool IsValidMultiplier(float multiplier)
        {
            return multiplier > 0f &&
                   !float.IsNaN(multiplier) &&
                   !float.IsInfinity(multiplier);
        }
    }
}
