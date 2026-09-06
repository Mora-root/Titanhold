namespace Titanhold.Run
{
    public sealed class RunParticipantStartReadinessState
    {
        internal RunParticipantStartReadinessState(
            RunParticipantIdentity identity)
        {
            PlayerId = identity.PlayerId;
            CharacterId = identity.CharacterId;
        }

        public string PlayerId { get; }
        public string CharacterId { get; }
        public string StartingAbilityId { get; private set; } = string.Empty;
        public bool IsConfirmed => StartingAbilityId.Length > 0;

        internal void Confirm(string abilityId)
        {
            StartingAbilityId = abilityId;
        }

        internal void ClearConfirmation()
        {
            StartingAbilityId = string.Empty;
        }
    }

    public enum RunStartReadinessError
    {
        None,
        ParticipantNotFound,
        InvalidAbilityId,
        AbilityNotAssignedToStartSlot,
        StartingAbilityAlreadyConfirmed,
        NotAllParticipantsConfirmed,
        ReadinessAlreadySealed
    }

    public readonly struct RunStartReadinessResult
    {
        private RunStartReadinessResult(
            bool success,
            RunStartReadinessError error,
            RunParticipantStartReadinessState state,
            bool changed)
        {
            Success = success;
            Error = error;
            State = state;
            Changed = changed;
        }

        public bool Success { get; }
        public RunStartReadinessError Error { get; }
        public RunParticipantStartReadinessState State { get; }
        public bool Changed { get; }

        public static RunStartReadinessResult Succeeded(
            RunParticipantStartReadinessState state = null,
            bool changed = true)
        {
            return new RunStartReadinessResult(
                true,
                RunStartReadinessError.None,
                state,
                changed);
        }

        public static RunStartReadinessResult Failed(
            RunStartReadinessError error,
            RunParticipantStartReadinessState state = null)
        {
            return new RunStartReadinessResult(
                false,
                error,
                state,
                false);
        }
    }
}
