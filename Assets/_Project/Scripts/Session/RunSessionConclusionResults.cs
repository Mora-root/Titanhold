namespace Titanhold.Session
{
    public enum RunSessionConclusionError
    {
        None,
        MissingRuntime,
        InvalidSessionPhase,
        MissingActiveRun,
        RunNotTerminal,
        InvalidParticipantBinding,
        RewardCalculationFailed,
        CharacterRewardFailed,
        CrystalRewardFailed,
        RewardSettlementFailed,
        CharacterCaptureFailed,
        SnapshotStoreFailed,
        SessionConclusionFailed
    }

    public readonly struct RunSessionConclusionResult
    {
        private RunSessionConclusionResult(
            bool success,
            RunSessionConclusionError error,
            string detail,
            string runSessionId,
            RunResultSummary summary)
        {
            Success = success;
            Error = error;
            Detail = detail ?? string.Empty;
            RunSessionId = runSessionId ?? string.Empty;
            Summary = summary;
        }

        public bool Success { get; }
        public RunSessionConclusionError Error { get; }
        public string Detail { get; }
        public string RunSessionId { get; }
        public RunResultSummary Summary { get; }

        public static RunSessionConclusionResult Succeeded(
            string runSessionId,
            RunResultSummary summary)
        {
            return new RunSessionConclusionResult(
                true,
                RunSessionConclusionError.None,
                string.Empty,
                runSessionId,
                summary);
        }

        public static RunSessionConclusionResult Failed(
            RunSessionConclusionError error,
            string detail = null,
            string runSessionId = null)
        {
            return new RunSessionConclusionResult(
                false,
                error,
                detail,
                runSessionId,
                null);
        }
    }
}
