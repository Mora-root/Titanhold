using System;

namespace Titanhold.Session
{
    public readonly struct RunChapterCompletionCommand
    {
        public RunChapterCompletionCommand(
            string eventId,
            string runSessionId,
            string participantId,
            int chapterNumber)
        {
            EventId = eventId?.Trim() ?? string.Empty;
            RunSessionId = runSessionId?.Trim() ?? string.Empty;
            ParticipantId = participantId?.Trim() ?? string.Empty;
            ChapterNumber = chapterNumber;
        }

        public string EventId { get; }
        public string RunSessionId { get; }
        public string ParticipantId { get; }
        public int ChapterNumber { get; }
        public bool IsValid =>
            EventId.Length > 0 &&
            RunSessionId.Length > 0 &&
            ParticipantId.Length > 0 &&
            ChapterNumber > 0;
    }

    public enum RunChapterCompletionError
    {
        None,
        InvalidCommand,
        MissingRuntime,
        MissingChapterFlow,
        MissingReward,
        InvalidSessionPhase,
        MissingActiveRun,
        RunSessionMismatch,
        InvalidChapter,
        InvalidChapterPhase,
        ParticipantNotRegistered,
        InvalidParticipantBinding,
        CharacterCaptureFailed,
        CharacterRewardFailed,
        CrystalRewardFailed,
        SnapshotStoreFailed,
        RewardSettlementFailed,
        ChapterCompletionFailed,
        SessionConclusionFailed
    }

    public readonly struct RunChapterCompletionResult
    {
        private RunChapterCompletionResult(
            bool success,
            bool replayed,
            RunChapterCompletionError error,
            string detail,
            string runSessionId,
            RunChapterCompletionReward reward,
            RunResultSummary summary)
        {
            Success = success;
            Replayed = replayed;
            Error = error;
            Detail = detail ?? string.Empty;
            RunSessionId = runSessionId ?? string.Empty;
            Reward = reward;
            Summary = summary;
        }

        public bool Success { get; }
        public bool Replayed { get; }
        public RunChapterCompletionError Error { get; }
        public string Detail { get; }
        public string RunSessionId { get; }
        public RunChapterCompletionReward Reward { get; }
        public RunResultSummary Summary { get; }

        public static RunChapterCompletionResult Succeeded(
            string runSessionId,
            RunChapterCompletionReward reward,
            RunResultSummary summary,
            bool replayed = false)
        {
            return new RunChapterCompletionResult(
                true,
                replayed,
                RunChapterCompletionError.None,
                string.Empty,
                runSessionId,
                reward,
                summary);
        }

        public static RunChapterCompletionResult Failed(
            RunChapterCompletionError error,
            string detail = null,
            string runSessionId = null)
        {
            return new RunChapterCompletionResult(
                false,
                false,
                error,
                detail,
                runSessionId,
                default,
                null);
        }
    }
}
