using System;

namespace Titanhold.Run
{
    public readonly struct RunParticipantIdentity
    {
        public RunParticipantIdentity(string playerId, string characterId)
        {
            PlayerId = playerId?.Trim() ?? string.Empty;
            CharacterId = characterId?.Trim() ?? string.Empty;
        }

        public string PlayerId { get; }
        public string CharacterId { get; }
        public bool IsValid =>
            !string.IsNullOrWhiteSpace(PlayerId) &&
            !string.IsNullOrWhiteSpace(CharacterId);
    }

    public sealed class RunParticipantProgressionState
    {
        internal RunParticipantProgressionState(
            RunParticipantIdentity identity)
        {
            PlayerId = identity.PlayerId;
            CharacterId = identity.CharacterId;
            Level = 1;
        }

        public string PlayerId { get; }
        public string CharacterId { get; }
        public int Level { get; private set; }
        public int Experience { get; private set; }
        public int Gold { get; private set; }

        internal void SetExperience(int level, int experience)
        {
            Level = level;
            Experience = experience;
        }

        internal void SetGold(int gold)
        {
            Gold = gold;
        }
    }

    public enum RunProgressionError
    {
        None,
        InvalidParticipant,
        DuplicatePlayer,
        DuplicateCharacter,
        ParticipantLimitExceeded,
        ParticipantNotFound,
        InvalidAmount,
        BalanceOverflow,
        InsufficientGold
    }

    public readonly struct RunProgressionResult
    {
        private RunProgressionResult(
            bool success,
            RunProgressionError error,
            RunParticipantProgressionState state,
            int levelsGained,
            int experienceApplied,
            int goldDelta)
        {
            Success = success;
            Error = error;
            State = state;
            LevelsGained = levelsGained;
            ExperienceApplied = experienceApplied;
            GoldDelta = goldDelta;
        }

        public bool Success { get; }
        public RunProgressionError Error { get; }
        public RunParticipantProgressionState State { get; }
        public int LevelsGained { get; }
        public int ExperienceApplied { get; }
        public int GoldDelta { get; }

        public static RunProgressionResult Succeeded(
            RunParticipantProgressionState state,
            int levelsGained = 0,
            int experienceApplied = 0,
            int goldDelta = 0)
        {
            return new RunProgressionResult(
                true,
                RunProgressionError.None,
                state,
                levelsGained,
                experienceApplied,
                goldDelta);
        }

        public static RunProgressionResult Failed(
            RunProgressionError error,
            RunParticipantProgressionState state = null)
        {
            return new RunProgressionResult(
                false,
                error,
                state,
                0,
                0,
                0);
        }
    }
}
