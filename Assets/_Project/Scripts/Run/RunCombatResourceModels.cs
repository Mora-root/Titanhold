using Titanhold.Combat;

namespace Titanhold.Run
{
    public enum RunCombatResourceError
    {
        None,
        InvalidParticipant,
        DuplicatePlayer,
        DuplicateCharacter,
        ParticipantLimitExceeded,
        ParticipantNotFound,
        InvalidResource,
        DuplicateResource,
        ResourceNotFound,
        InvalidExecutionId,
        InvalidAmount,
        InsufficientResource
    }

    public readonly struct RunCombatResourceResult
    {
        private RunCombatResourceResult(
            bool success,
            RunCombatResourceError error,
            string playerId,
            CombatResourceSnapshot resource,
            bool changed)
        {
            Success = success;
            Error = error;
            PlayerId = playerId ?? string.Empty;
            Resource = resource;
            Changed = changed;
        }

        public bool Success { get; }
        public RunCombatResourceError Error { get; }
        public string PlayerId { get; }
        public CombatResourceSnapshot Resource { get; }
        public bool Changed { get; }

        public static RunCombatResourceResult Succeeded(
            string playerId,
            CombatResourceSnapshot resource = default,
            bool changed = false)
        {
            return new RunCombatResourceResult(
                true,
                RunCombatResourceError.None,
                playerId,
                resource,
                changed);
        }

        public static RunCombatResourceResult Failed(
            RunCombatResourceError error,
            string playerId = null,
            CombatResourceSnapshot resource = default)
        {
            return new RunCombatResourceResult(
                false,
                error,
                playerId,
                resource,
                false);
        }
    }
}
