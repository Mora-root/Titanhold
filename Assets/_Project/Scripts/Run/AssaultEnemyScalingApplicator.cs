namespace Titanhold.Run
{
    public sealed class AssaultEnemyScalingApplicator
    {
        private readonly EnemyScalingApplicator applicator = new();

        public AssaultEnemyScalingResult TryApply(
            Health health,
            EnemyCombat combat,
            AssaultScalingSnapshot snapshot)
        {
            EnemyScalingResult result = applicator.TryApply(
                health,
                combat,
                snapshot.EnemyScaling,
                restoreFullHealth: true);
            return result.Success
                ? AssaultEnemyScalingResult.Succeeded(snapshot)
                : AssaultEnemyScalingResult.Failed(MapError(result.Error), snapshot);
        }

        private static AssaultEnemyScalingError MapError(EnemyScalingError error)
        {
            return error switch
            {
                EnemyScalingError.MissingHealth => AssaultEnemyScalingError.MissingHealth,
                EnemyScalingError.MissingCombat => AssaultEnemyScalingError.MissingCombat,
                EnemyScalingError.InvalidSnapshot => AssaultEnemyScalingError.InvalidSnapshot,
                EnemyScalingError.HealthRejected => AssaultEnemyScalingError.HealthRejected,
                EnemyScalingError.CombatRejected => AssaultEnemyScalingError.CombatRejected,
                _ => AssaultEnemyScalingError.None
            };
        }
    }
}
