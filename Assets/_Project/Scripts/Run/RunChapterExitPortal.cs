namespace Titanhold.Run
{
    public readonly struct RunChapterExitPortalRequest
    {
        public RunChapterExitPortalRequest(
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
}
