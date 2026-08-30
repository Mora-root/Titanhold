namespace Titanhold.Run
{
    public enum AssaultEncounterError
    {
        None,
        InvalidEncounterId,
        InvalidExpectedRound,
        InvalidPlannedEnemyCount,
        InvalidEnemy,
        InvalidPhase,
        EncounterNotActive,
        StaleEncounter,
        DuplicateEnemy,
        SpawnLimitReached,
        EnemyNotAlive,
        RunFlowRejected
    }

    public readonly struct AssaultEncounterResult
    {
        private AssaultEncounterResult(
            bool success,
            AssaultEncounterError error,
            bool encounterCompleted,
            RunFlowTransitionResult runFlowTransition)
        {
            Success = success;
            Error = error;
            EncounterCompleted = encounterCompleted;
            RunFlowTransition = runFlowTransition;
        }

        public bool Success { get; }
        public AssaultEncounterError Error { get; }
        public bool EncounterCompleted { get; }
        public RunFlowTransitionResult RunFlowTransition { get; }

        public static AssaultEncounterResult Succeeded(
            bool encounterCompleted = false,
            RunFlowTransitionResult runFlowTransition = default)
        {
            return new AssaultEncounterResult(
                true,
                AssaultEncounterError.None,
                encounterCompleted,
                runFlowTransition);
        }

        public static AssaultEncounterResult Failed(AssaultEncounterError error)
        {
            return new AssaultEncounterResult(false, error, false, default);
        }
    }
}
