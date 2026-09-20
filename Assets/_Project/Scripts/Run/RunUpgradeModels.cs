using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Titanhold.Run
{
    public sealed class RunParticipantUpgradeState
    {
        private readonly Dictionary<string, int> stacks =
            new(StringComparer.Ordinal);
        private readonly List<string> selectionHistory = new();
        private readonly ReadOnlyCollection<string> readOnlySelectionHistory;

        internal RunParticipantUpgradeState(RunParticipantIdentity identity)
        {
            PlayerId = identity.PlayerId;
            CharacterId = identity.CharacterId;
            readOnlySelectionHistory = selectionHistory.AsReadOnly();
        }

        public string PlayerId { get; }
        public string CharacterId { get; }
        public IReadOnlyList<string> SelectionHistory =>
            readOnlySelectionHistory;

        public int GetStackCount(string upgradeId)
        {
            string normalizedId = upgradeId?.Trim() ?? string.Empty;
            return normalizedId.Length > 0 &&
                   stacks.TryGetValue(normalizedId, out int count)
                ? count
                : 0;
        }

        internal bool TryAddStack(string upgradeId)
        {
            int current = GetStackCount(upgradeId);
            if (current == int.MaxValue)
                return false;

            stacks[upgradeId] = current + 1;
            selectionHistory.Add(upgradeId);
            return true;
        }
    }

    public sealed class RunUpgradeChoiceRequest
    {
        private readonly ReadOnlyCollection<string> candidateUpgradeIds;

        public RunUpgradeChoiceRequest(
            string playerId,
            string choiceId,
            IReadOnlyList<string> candidateUpgradeIds,
            int optionCount,
            int rollSeed)
        {
            PlayerId = playerId?.Trim() ?? string.Empty;
            ChoiceId = choiceId?.Trim() ?? string.Empty;
            OptionCount = optionCount;
            RollSeed = rollSeed;

            int count = candidateUpgradeIds?.Count ?? 0;
            string[] copy = new string[count];
            for (int i = 0; i < count; i++)
                copy[i] = candidateUpgradeIds[i]?.Trim() ?? string.Empty;

            this.candidateUpgradeIds = Array.AsReadOnly(copy);
        }

        public string PlayerId { get; }
        public string ChoiceId { get; }
        public int OptionCount { get; }
        public int RollSeed { get; }
        public IReadOnlyList<string> CandidateUpgradeIds =>
            candidateUpgradeIds;
    }

    public sealed class RunUpgradeChoiceState
    {
        private readonly ReadOnlyCollection<string> offeredUpgradeIds;

        internal RunUpgradeChoiceState(
            string playerId,
            string choiceId,
            int rollSeed,
            IReadOnlyList<string> offeredUpgradeIds)
        {
            PlayerId = playerId;
            ChoiceId = choiceId;
            RollSeed = rollSeed;

            string[] copy = new string[offeredUpgradeIds.Count];
            for (int i = 0; i < copy.Length; i++)
                copy[i] = offeredUpgradeIds[i];

            this.offeredUpgradeIds = Array.AsReadOnly(copy);
        }

        public string PlayerId { get; }
        public string ChoiceId { get; }
        public int RollSeed { get; }
        public IReadOnlyList<string> OfferedUpgradeIds => offeredUpgradeIds;

        public bool ContainsUpgrade(string upgradeId)
        {
            string normalizedId = upgradeId?.Trim() ?? string.Empty;
            if (normalizedId.Length == 0)
                return false;

            for (int i = 0; i < offeredUpgradeIds.Count; i++)
            {
                if (string.Equals(
                        offeredUpgradeIds[i],
                        normalizedId,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public enum RunUpgradeChoiceError
    {
        None,
        InvalidParticipant,
        DuplicatePlayer,
        DuplicateCharacter,
        ParticipantLimitExceeded,
        ParticipantNotFound,
        InvalidRequest,
        InvalidCandidatePool,
        UpgradeNotFound,
        InsufficientCandidates,
        ChoiceAlreadyPending,
        ChoiceAlreadyResolved,
        ChoiceNotFound,
        ChoiceMismatch,
        UpgradeNotOffered,
        StackOverflow
    }

    public readonly struct RunUpgradeChoiceResult
    {
        private RunUpgradeChoiceResult(
            bool success,
            RunUpgradeChoiceError error,
            RunUpgradeChoiceState choice,
            RunParticipantUpgradeState participant,
            string selectedUpgradeId)
        {
            Success = success;
            Error = error;
            Choice = choice;
            Participant = participant;
            SelectedUpgradeId = selectedUpgradeId ?? string.Empty;
        }

        public bool Success { get; }
        public RunUpgradeChoiceError Error { get; }
        public RunUpgradeChoiceState Choice { get; }
        public RunParticipantUpgradeState Participant { get; }
        public string SelectedUpgradeId { get; }

        public static RunUpgradeChoiceResult Succeeded(
            RunParticipantUpgradeState participant,
            RunUpgradeChoiceState choice = null,
            string selectedUpgradeId = "")
        {
            return new RunUpgradeChoiceResult(
                true,
                RunUpgradeChoiceError.None,
                choice,
                participant,
                selectedUpgradeId);
        }

        public static RunUpgradeChoiceResult Failed(
            RunUpgradeChoiceError error,
            RunParticipantUpgradeState participant = null,
            RunUpgradeChoiceState choice = null)
        {
            return new RunUpgradeChoiceResult(
                false,
                error,
                choice,
                participant,
                string.Empty);
        }
    }
}
