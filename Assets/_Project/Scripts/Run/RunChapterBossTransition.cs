namespace Titanhold.Run
{
    public enum RunChapterBossTransitionCause
    {
        ManualPortal,
        CollapseExpired
    }

    public enum RunChapterBossTransitionApplicationError
    {
        None,
        MissingEventId,
        MissingParticipantId,
        InvalidExpectedChapter,
        StaleChapter,
        ParticipantNotRegistered,
        PortalUnavailable,
        DuplicateEvent,
        ChapterFlowRejected,
        TransitionAlreadyPublished
    }

    public interface IRunChapterTransitionParticipantRoster
    {
        bool Contains(string participantId);
    }

    public readonly struct RunChapterBossPortalEntryCommand
    {
        public RunChapterBossPortalEntryCommand(
            string eventId,
            string participantId,
            int expectedChapterNumber,
            double simulationTime)
        {
            EventId = eventId?.Trim() ?? string.Empty;
            ParticipantId = participantId?.Trim() ?? string.Empty;
            ExpectedChapterNumber = expectedChapterNumber;
            SimulationTime = simulationTime;
        }

        public string EventId { get; }
        public string ParticipantId { get; }
        public int ExpectedChapterNumber { get; }
        public double SimulationTime { get; }
    }

    public readonly struct RunChapterBossTransitionRequest
    {
        internal RunChapterBossTransitionRequest(
            string runId,
            string eventId,
            int chapterNumber,
            string initiatingParticipantId,
            RunChapterBossTransitionCause cause,
            double simulationTime,
            RunChapterBossScalingSnapshot bossScaling)
        {
            RunId = runId ?? string.Empty;
            EventId = eventId ?? string.Empty;
            ChapterNumber = chapterNumber;
            InitiatingParticipantId = initiatingParticipantId ?? string.Empty;
            Cause = cause;
            SimulationTime = simulationTime;
            BossScaling = bossScaling;
            IsValid = true;
        }

        public bool IsValid { get; }
        public string RunId { get; }
        public string EventId { get; }
        public int ChapterNumber { get; }
        public string InitiatingParticipantId { get; }
        public RunChapterBossTransitionCause Cause { get; }
        public bool WasForced =>
            Cause == RunChapterBossTransitionCause.CollapseExpired;
        public double SimulationTime { get; }
        public RunChapterBossScalingSnapshot BossScaling { get; }
    }

    public readonly struct RunChapterBossTransitionApplicationResult
    {
        private RunChapterBossTransitionApplicationResult(
            bool success,
            RunChapterBossTransitionApplicationError error,
            RunChapterFlowResult flowResult,
            RunChapterBossTransitionRequest transitionRequest)
        {
            Success = success;
            Error = error;
            FlowResult = flowResult;
            TransitionRequest = transitionRequest;
        }

        public bool Success { get; }
        public RunChapterBossTransitionApplicationError Error { get; }
        public RunChapterFlowResult FlowResult { get; }
        public RunChapterBossTransitionRequest TransitionRequest { get; }
        public bool HasTransitionRequest => TransitionRequest.IsValid;

        internal static RunChapterBossTransitionApplicationResult Succeeded(
            RunChapterFlowResult flowResult,
            RunChapterBossTransitionRequest transitionRequest = default)
        {
            return new RunChapterBossTransitionApplicationResult(
                true,
                RunChapterBossTransitionApplicationError.None,
                flowResult,
                transitionRequest);
        }

        internal static RunChapterBossTransitionApplicationResult Failed(
            RunChapterBossTransitionApplicationError error,
            RunChapterFlowResult flowResult = default)
        {
            return new RunChapterBossTransitionApplicationResult(
                false,
                error,
                flowResult,
                default);
        }
    }
}
