using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Titanhold.Run
{
    public sealed class RunAbilityChoiceRequest
    {
        private readonly ReadOnlyCollection<string> candidateAbilityIds;

        public RunAbilityChoiceRequest(
            string playerId,
            string choiceId,
            int targetSlotIndex,
            IReadOnlyList<string> candidateAbilityIds,
            int optionCount,
            int rollSeed)
        {
            PlayerId = playerId?.Trim() ?? string.Empty;
            ChoiceId = choiceId?.Trim() ?? string.Empty;
            TargetSlotIndex = targetSlotIndex;
            OptionCount = optionCount;
            RollSeed = rollSeed;

            int count = candidateAbilityIds?.Count ?? 0;
            string[] copy = new string[count];
            for (int i = 0; i < count; i++)
                copy[i] = candidateAbilityIds[i]?.Trim() ?? string.Empty;

            this.candidateAbilityIds = Array.AsReadOnly(copy);
        }

        public string PlayerId { get; }
        public string ChoiceId { get; }
        public int TargetSlotIndex { get; }
        public int OptionCount { get; }
        public int RollSeed { get; }
        public IReadOnlyList<string> CandidateAbilityIds =>
            candidateAbilityIds;
    }

    public sealed class RunAbilityChoiceState
    {
        private readonly ReadOnlyCollection<string> offeredAbilityIds;

        internal RunAbilityChoiceState(
            string playerId,
            string choiceId,
            int targetSlotIndex,
            int rollSeed,
            IReadOnlyList<string> offeredAbilityIds)
        {
            PlayerId = playerId;
            ChoiceId = choiceId;
            TargetSlotIndex = targetSlotIndex;
            RollSeed = rollSeed;

            string[] copy = new string[offeredAbilityIds.Count];
            for (int i = 0; i < copy.Length; i++)
                copy[i] = offeredAbilityIds[i];

            this.offeredAbilityIds = Array.AsReadOnly(copy);
        }

        public string PlayerId { get; }
        public string ChoiceId { get; }
        public int TargetSlotIndex { get; }
        public int RollSeed { get; }
        public IReadOnlyList<string> OfferedAbilityIds => offeredAbilityIds;

        public bool ContainsAbility(string abilityId)
        {
            string normalizedId = abilityId?.Trim() ?? string.Empty;
            if (normalizedId.Length == 0)
                return false;

            for (int i = 0; i < offeredAbilityIds.Count; i++)
            {
                if (string.Equals(
                        offeredAbilityIds[i],
                        normalizedId,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public enum RunAbilityChoiceError
    {
        None,
        InvalidRequest,
        ParticipantNotFound,
        InvalidSlot,
        InvalidCandidatePool,
        InsufficientEligibleAbilities,
        ChoiceAlreadyPending,
        ChoiceAlreadyResolved,
        ChoiceNotFound,
        ChoiceMismatch,
        AbilityNotOffered,
        LoadoutRejected
    }

    public readonly struct RunAbilityChoiceResult
    {
        private RunAbilityChoiceResult(
            bool success,
            RunAbilityChoiceError error,
            RunAbilityChoiceState state,
            string selectedAbilityId,
            RunAbilityLoadoutError loadoutError)
        {
            Success = success;
            Error = error;
            State = state;
            SelectedAbilityId = selectedAbilityId ?? string.Empty;
            LoadoutError = loadoutError;
        }

        public bool Success { get; }
        public RunAbilityChoiceError Error { get; }
        public RunAbilityChoiceState State { get; }
        public string SelectedAbilityId { get; }
        public RunAbilityLoadoutError LoadoutError { get; }

        public static RunAbilityChoiceResult Succeeded(
            RunAbilityChoiceState state,
            string selectedAbilityId = "")
        {
            return new RunAbilityChoiceResult(
                true,
                RunAbilityChoiceError.None,
                state,
                selectedAbilityId,
                RunAbilityLoadoutError.None);
        }

        public static RunAbilityChoiceResult Failed(
            RunAbilityChoiceError error,
            RunAbilityChoiceState state = null,
            RunAbilityLoadoutError loadoutError =
                RunAbilityLoadoutError.None)
        {
            return new RunAbilityChoiceResult(
                false,
                error,
                state,
                string.Empty,
                loadoutError);
        }
    }
}
