namespace Titanhold.Run
{
    public enum RunFlowError
    {
        None,
        InvalidPhase,
        EmptyKillBatch,
        InvalidKillContribution,
        NoApplicableContribution,
        TerminalState
    }

    public readonly struct ExplorationKillContribution
    {
        public ExplorationKillContribution(float threatAmount, int instabilityPoints)
        {
            ThreatAmount = threatAmount;
            InstabilityPoints = instabilityPoints;
        }

        public float ThreatAmount { get; }
        public int InstabilityPoints { get; }
    }

    public readonly struct ExplorationKillBatchResult
    {
        private ExplorationKillBatchResult(
            bool success,
            RunFlowError error,
            RunPhase previousPhase,
            RunPhase currentPhase,
            float threatAdded,
            int instabilityPointsAdded,
            bool portalOpened)
        {
            Success = success;
            Error = error;
            PreviousPhase = previousPhase;
            CurrentPhase = currentPhase;
            ThreatAdded = threatAdded;
            InstabilityPointsAdded = instabilityPointsAdded;
            PortalOpened = portalOpened;
        }

        public bool Success { get; }
        public RunFlowError Error { get; }
        public RunPhase PreviousPhase { get; }
        public RunPhase CurrentPhase { get; }
        public float ThreatAdded { get; }
        public int InstabilityPointsAdded { get; }
        public bool PortalOpened { get; }

        public static ExplorationKillBatchResult Succeeded(
            RunPhase previousPhase,
            RunPhase currentPhase,
            float threatAdded,
            int instabilityPointsAdded,
            bool portalOpened)
        {
            return new ExplorationKillBatchResult(
                true,
                RunFlowError.None,
                previousPhase,
                currentPhase,
                threatAdded,
                instabilityPointsAdded,
                portalOpened);
        }

        public static ExplorationKillBatchResult Failed(RunFlowError error, RunPhase phase)
        {
            return new ExplorationKillBatchResult(false, error, phase, phase, 0f, 0, false);
        }
    }

    public readonly struct RunFlowTransitionResult
    {
        private RunFlowTransitionResult(
            bool success,
            RunFlowError error,
            RunPhase previousPhase,
            RunPhase currentPhase)
        {
            Success = success;
            Error = error;
            PreviousPhase = previousPhase;
            CurrentPhase = currentPhase;
        }

        public bool Success { get; }
        public RunFlowError Error { get; }
        public RunPhase PreviousPhase { get; }
        public RunPhase CurrentPhase { get; }

        public static RunFlowTransitionResult Succeeded(RunPhase previousPhase, RunPhase currentPhase)
        {
            return new RunFlowTransitionResult(true, RunFlowError.None, previousPhase, currentPhase);
        }

        public static RunFlowTransitionResult Failed(RunFlowError error, RunPhase phase)
        {
            return new RunFlowTransitionResult(false, error, phase, phase);
        }
    }
}
