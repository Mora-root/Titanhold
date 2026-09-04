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
            string runSessionId)
        {
            Success = success;
            Error = error;
            Detail = detail ?? string.Empty;
            RunSessionId = runSessionId ?? string.Empty;
        }

        public bool Success { get; }
        public RunSessionConclusionError Error { get; }
        public string Detail { get; }
        public string RunSessionId { get; }

        public static RunSessionConclusionResult Succeeded(string runSessionId)
        {
            return new RunSessionConclusionResult(
                true,
                RunSessionConclusionError.None,
                string.Empty,
                runSessionId);
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
                runSessionId);
        }
    }
}
