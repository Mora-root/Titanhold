namespace Titanhold.Enemies
{
    public interface IEnemyBaseStatsGateway
    {
        bool IsReady { get; }
        void SetBaseStat(StatType statType, float value);
        void ConfigureCombat(float baseAttacksPerSecond);
        void ConfigureMovement(float rotationSpeed);
        void ConfigureDetection(float detectionRange);
        void RestoreFullHealth();
    }

    public enum EnemyBaseStatsApplicationError
    {
        None,
        MissingArchetype,
        InvalidStats,
        MissingGateway,
        GatewayNotReady
    }

    public readonly struct EnemyBaseStatsApplicationResult
    {
        private EnemyBaseStatsApplicationResult(
            bool success,
            EnemyBaseStatsApplicationError error,
            string enemyId)
        {
            Success = success;
            Error = error;
            EnemyId = enemyId ?? string.Empty;
        }

        public bool Success { get; }
        public EnemyBaseStatsApplicationError Error { get; }
        public string EnemyId { get; }

        public static EnemyBaseStatsApplicationResult Succeeded(
            string enemyId)
        {
            return new EnemyBaseStatsApplicationResult(
                true,
                EnemyBaseStatsApplicationError.None,
                enemyId);
        }

        public static EnemyBaseStatsApplicationResult Failed(
            EnemyBaseStatsApplicationError error,
            string enemyId = "")
        {
            return new EnemyBaseStatsApplicationResult(
                false,
                error,
                enemyId);
        }
    }

    public sealed class EnemyBaseStatsApplicationService
    {
        public EnemyBaseStatsApplicationResult TryApply(
            EnemyArchetype archetype,
            IEnemyBaseStatsGateway gateway)
        {
            if (archetype == null)
            {
                return EnemyBaseStatsApplicationResult.Failed(
                    EnemyBaseStatsApplicationError.MissingArchetype);
            }

            EnemyBaseStats baseStats = archetype.BaseStats;
            if (!baseStats.TryValidate(out _))
            {
                return EnemyBaseStatsApplicationResult.Failed(
                    EnemyBaseStatsApplicationError.InvalidStats,
                    archetype.EnemyId);
            }

            if (gateway == null)
            {
                return EnemyBaseStatsApplicationResult.Failed(
                    EnemyBaseStatsApplicationError.MissingGateway,
                    archetype.EnemyId);
            }

            if (!gateway.IsReady)
            {
                return EnemyBaseStatsApplicationResult.Failed(
                    EnemyBaseStatsApplicationError.GatewayNotReady,
                    archetype.EnemyId);
            }

            gateway.SetBaseStat(
                StatType.MaxHealth,
                baseStats.MaximumHealth);
            gateway.SetBaseStat(StatType.Armor, baseStats.Armor);
            gateway.SetBaseStat(StatType.Damage, baseStats.BaseDamage);
            gateway.SetBaseStat(StatType.AttackSpeed, 100f);
            gateway.SetBaseStat(
                StatType.AttackRange,
                baseStats.AttackRange);
            gateway.SetBaseStat(
                StatType.MoveSpeed,
                baseStats.MovementSpeed);
            gateway.ConfigureCombat(baseStats.AttacksPerSecond);
            gateway.ConfigureMovement(baseStats.RotationSpeed);
            gateway.ConfigureDetection(baseStats.DetectionRange);
            gateway.RestoreFullHealth();

            return EnemyBaseStatsApplicationResult.Succeeded(
                archetype.EnemyId);
        }
    }
}
