namespace Titanhold.Run
{
    public enum AssaultEnemyRegistryError
    {
        None,
        MissingRuntime,
        InvalidNotifier,
        NotifierAlreadyRegistered,
        NotifierNotRegistered,
        ApplicationRejected
    }

    public readonly struct AssaultEnemyRegistryResult
    {
        private AssaultEnemyRegistryResult(
            bool success,
            AssaultEnemyRegistryError error,
            AssaultEncounterResult encounterResult)
        {
            Success = success;
            Error = error;
            EncounterResult = encounterResult;
        }

        public bool Success { get; }
        public AssaultEnemyRegistryError Error { get; }
        public AssaultEncounterResult EncounterResult { get; }

        public static AssaultEnemyRegistryResult Succeeded(
            AssaultEncounterResult encounterResult)
        {
            return new AssaultEnemyRegistryResult(
                true,
                AssaultEnemyRegistryError.None,
                encounterResult);
        }

        public static AssaultEnemyRegistryResult Failed(
            AssaultEnemyRegistryError error,
            AssaultEncounterResult encounterResult = default)
        {
            return new AssaultEnemyRegistryResult(false, error, encounterResult);
        }
    }
}
