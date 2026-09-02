namespace Titanhold.Session
{
    public enum GameSessionError
    {
        None,
        InvalidPhase,
        InvalidDifficulty,
        MissingParticipants,
        InvalidParticipant,
        DuplicatePlayer,
        DuplicateCharacter,
        ParticipantLimitExceeded,
        MissingRunSessionId,
        RunSessionMismatch,
        InvalidRunResult
    }

    public readonly struct GameSessionCommandResult
    {
        private GameSessionCommandResult(
            bool success,
            GameSessionError error,
            GameSessionPhase previousPhase,
            GameSessionPhase currentPhase,
            string runSessionId)
        {
            Success = success;
            Error = error;
            PreviousPhase = previousPhase;
            CurrentPhase = currentPhase;
            RunSessionId = runSessionId ?? string.Empty;
        }

        public bool Success { get; }
        public GameSessionError Error { get; }
        public GameSessionPhase PreviousPhase { get; }
        public GameSessionPhase CurrentPhase { get; }
        public string RunSessionId { get; }

        public static GameSessionCommandResult Succeeded(
            GameSessionPhase previousPhase,
            GameSessionPhase currentPhase,
            string runSessionId)
        {
            return new GameSessionCommandResult(
                true,
                GameSessionError.None,
                previousPhase,
                currentPhase,
                runSessionId);
        }

        public static GameSessionCommandResult Failed(
            GameSessionError error,
            GameSessionPhase phase,
            string runSessionId = null)
        {
            return new GameSessionCommandResult(
                false,
                error,
                phase,
                phase,
                runSessionId);
        }
    }
}
