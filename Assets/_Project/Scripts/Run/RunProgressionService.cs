using System;
using System.Collections.Generic;

namespace Titanhold.Run
{
    public sealed class RunProgressionService
    {
        public const int DefaultMaximumParticipantCount = 8;

        private readonly RunExperienceCurve experienceCurve;
        private readonly int maximumParticipantCount;
        private readonly Dictionary<string, RunParticipantProgressionState>
            participants = new(StringComparer.Ordinal);
        private readonly HashSet<string> characterIds =
            new(StringComparer.Ordinal);

        public RunProgressionService(
            RunExperienceCurve experienceCurve,
            int maximumParticipantCount =
                DefaultMaximumParticipantCount)
        {
            this.experienceCurve = experienceCurve ??
                throw new ArgumentNullException(nameof(experienceCurve));
            if (maximumParticipantCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumParticipantCount));
            }

            this.maximumParticipantCount = maximumParticipantCount;
        }

        public int ParticipantCount => participants.Count;
        public int MaximumParticipantCount => maximumParticipantCount;

        public event Action<RunParticipantProgressionState> StateChanged;

        public RunProgressionResult TryRegisterParticipant(
            RunParticipantIdentity identity)
        {
            if (!identity.IsValid)
            {
                return RunProgressionResult.Failed(
                    RunProgressionError.InvalidParticipant);
            }

            if (participants.ContainsKey(identity.PlayerId))
            {
                return RunProgressionResult.Failed(
                    RunProgressionError.DuplicatePlayer);
            }

            if (characterIds.Contains(identity.CharacterId))
            {
                return RunProgressionResult.Failed(
                    RunProgressionError.DuplicateCharacter);
            }

            if (participants.Count >= maximumParticipantCount)
            {
                return RunProgressionResult.Failed(
                    RunProgressionError.ParticipantLimitExceeded);
            }

            RunParticipantProgressionState state = new(identity);
            participants.Add(identity.PlayerId, state);
            characterIds.Add(identity.CharacterId);
            StateChanged?.Invoke(state);
            return RunProgressionResult.Succeeded(state);
        }

        public bool TryGetParticipant(
            string playerId,
            out RunParticipantProgressionState state)
        {
            state = null;
            string normalizedId = playerId?.Trim() ?? string.Empty;
            return normalizedId.Length > 0 &&
                   participants.TryGetValue(normalizedId, out state);
        }

        public bool TryGetExperienceRequirement(
            string playerId,
            out int experienceRequired)
        {
            experienceRequired = 0;
            return TryGetParticipant(
                       playerId,
                       out RunParticipantProgressionState state) &&
                   experienceCurve.TryGetRequirement(
                       state.Level,
                       out experienceRequired);
        }

        public RunProgressionResult TryGrantExperience(
            string playerId,
            int amount)
        {
            if (!TryGetParticipant(playerId, out RunParticipantProgressionState state))
            {
                return RunProgressionResult.Failed(
                    RunProgressionError.ParticipantNotFound);
            }

            if (amount <= 0)
            {
                return RunProgressionResult.Failed(
                    RunProgressionError.InvalidAmount,
                    state);
            }

            int previousLevel = state.Level;
            if (state.Level >= experienceCurve.MaximumLevel)
            {
                return RunProgressionResult.Succeeded(state);
            }

            long experience = (long)state.Experience + amount;
            int level = state.Level;
            while (experienceCurve.TryGetRequirement(
                       level,
                       out int required) &&
                   experience >= required)
            {
                experience -= required;
                level++;
            }

            if (level >= experienceCurve.MaximumLevel)
                experience = 0;

            state.SetExperience(level, (int)experience);
            StateChanged?.Invoke(state);
            return RunProgressionResult.Succeeded(
                state,
                level - previousLevel,
                amount);
        }

        public RunProgressionResult TryAddGold(
            string playerId,
            int amount)
        {
            if (!TryGetParticipant(playerId, out RunParticipantProgressionState state))
            {
                return RunProgressionResult.Failed(
                    RunProgressionError.ParticipantNotFound);
            }

            if (amount <= 0)
            {
                return RunProgressionResult.Failed(
                    RunProgressionError.InvalidAmount,
                    state);
            }

            long updated = (long)state.Gold + amount;
            if (updated > int.MaxValue)
            {
                return RunProgressionResult.Failed(
                    RunProgressionError.BalanceOverflow,
                    state);
            }

            state.SetGold((int)updated);
            StateChanged?.Invoke(state);
            return RunProgressionResult.Succeeded(
                state,
                goldDelta: amount);
        }

        public RunProgressionResult TrySpendGold(
            string playerId,
            int amount)
        {
            if (!TryGetParticipant(playerId, out RunParticipantProgressionState state))
            {
                return RunProgressionResult.Failed(
                    RunProgressionError.ParticipantNotFound);
            }

            if (amount <= 0)
            {
                return RunProgressionResult.Failed(
                    RunProgressionError.InvalidAmount,
                    state);
            }

            if (state.Gold < amount)
            {
                return RunProgressionResult.Failed(
                    RunProgressionError.InsufficientGold,
                    state);
            }

            state.SetGold(state.Gold - amount);
            StateChanged?.Invoke(state);
            return RunProgressionResult.Succeeded(
                state,
                goldDelta: -amount);
        }
    }
}
