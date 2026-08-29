using Titanhold.Combat;

namespace Titanhold.Run
{
    public enum ExplorationKillApplicationError
    {
        None,
        EmptyBatch,
        InvalidDeathContext,
        NonPlayerSource,
        InvalidDefeatedActor,
        MixedExecution,
        MixedSource,
        DuplicateDefeatedActor,
        DuplicateExecution,
        RunFlowRejected
    }

    public readonly struct ExplorationKillApplicationResult
    {
        private ExplorationKillApplicationResult(
            bool success,
            ExplorationKillApplicationError error,
            CombatExecutionId executionId,
            int acceptedKillCount,
            ExplorationKillBatchResult runFlowResult)
        {
            Success = success;
            Error = error;
            ExecutionId = executionId;
            AcceptedKillCount = acceptedKillCount;
            RunFlowResult = runFlowResult;
        }

        public bool Success { get; }
        public ExplorationKillApplicationError Error { get; }
        public CombatExecutionId ExecutionId { get; }
        public int AcceptedKillCount { get; }
        public ExplorationKillBatchResult RunFlowResult { get; }

        public static ExplorationKillApplicationResult Succeeded(
            CombatExecutionId executionId,
            int acceptedKillCount,
            ExplorationKillBatchResult runFlowResult)
        {
            return new ExplorationKillApplicationResult(
                true,
                ExplorationKillApplicationError.None,
                executionId,
                acceptedKillCount,
                runFlowResult);
        }

        public static ExplorationKillApplicationResult Failed(
            ExplorationKillApplicationError error,
            CombatExecutionId executionId = default,
            ExplorationKillBatchResult runFlowResult = default)
        {
            return new ExplorationKillApplicationResult(
                false,
                error,
                executionId,
                0,
                runFlowResult);
        }
    }
}
