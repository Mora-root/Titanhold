namespace Titanhold.Run
{
    public enum EnemyScalingError
    {
        None,
        MissingHealth,
        MissingCombat,
        InvalidSnapshot,
        HealthRejected,
        CombatRejected
    }

    public readonly struct EnemyScalingResult
    {
        private EnemyScalingResult(
            bool success,
            EnemyScalingError error,
            EnemyScalingSnapshot snapshot)
        {
            Success = success;
            Error = error;
            Snapshot = snapshot;
        }

        public bool Success { get; }
        public EnemyScalingError Error { get; }
        public EnemyScalingSnapshot Snapshot { get; }

        public static EnemyScalingResult Succeeded(EnemyScalingSnapshot snapshot)
        {
            return new EnemyScalingResult(true, EnemyScalingError.None, snapshot);
        }

        public static EnemyScalingResult Failed(
            EnemyScalingError error,
            EnemyScalingSnapshot snapshot = default)
        {
            return new EnemyScalingResult(false, error, snapshot);
        }
    }
}
