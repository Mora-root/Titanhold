namespace Titanhold.Run
{
    public enum AssaultEnemyScalingError
    {
        None,
        MissingHealth,
        MissingCombat,
        InvalidSnapshot,
        HealthRejected,
        CombatRejected
    }

    public readonly struct AssaultEnemyScalingResult
    {
        private AssaultEnemyScalingResult(
            bool success,
            AssaultEnemyScalingError error,
            AssaultScalingSnapshot snapshot)
        {
            Success = success;
            Error = error;
            Snapshot = snapshot;
        }

        public bool Success { get; }
        public AssaultEnemyScalingError Error { get; }
        public AssaultScalingSnapshot Snapshot { get; }

        public static AssaultEnemyScalingResult Succeeded(
            AssaultScalingSnapshot snapshot)
        {
            return new AssaultEnemyScalingResult(
                true,
                AssaultEnemyScalingError.None,
                snapshot);
        }

        public static AssaultEnemyScalingResult Failed(
            AssaultEnemyScalingError error,
            AssaultScalingSnapshot snapshot = default)
        {
            return new AssaultEnemyScalingResult(false, error, snapshot);
        }
    }
}
