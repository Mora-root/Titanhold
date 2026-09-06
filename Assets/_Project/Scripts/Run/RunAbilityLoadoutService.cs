using System;
using System.Collections.Generic;

namespace Titanhold.Run
{
    public sealed class RunAbilityLoadoutService
    {
        public const int DefaultAbilitySlotCount = 5;
        public const int DefaultMaximumParticipantCount = 8;

        private readonly int abilitySlotCount;
        private readonly int maximumParticipantCount;
        private readonly Dictionary<string, RunParticipantAbilityState>
            participants = new(StringComparer.Ordinal);
        private readonly HashSet<string> characterIds =
            new(StringComparer.Ordinal);

        public RunAbilityLoadoutService(
            int abilitySlotCount = DefaultAbilitySlotCount,
            int maximumParticipantCount = DefaultMaximumParticipantCount)
        {
            if (abilitySlotCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(abilitySlotCount));
            if (maximumParticipantCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumParticipantCount));

            this.abilitySlotCount = abilitySlotCount;
            this.maximumParticipantCount = maximumParticipantCount;
        }

        public int ParticipantCount => participants.Count;
        public int AbilitySlotCount => abilitySlotCount;
        public int MaximumParticipantCount => maximumParticipantCount;

        public event Action<RunParticipantAbilityState> StateChanged;

        public RunAbilityLoadoutResult TryRegisterParticipant(
            RunParticipantIdentity identity)
        {
            if (!identity.IsValid)
            {
                return RunAbilityLoadoutResult.Failed(
                    RunAbilityLoadoutError.InvalidParticipant);
            }

            if (participants.ContainsKey(identity.PlayerId))
            {
                return RunAbilityLoadoutResult.Failed(
                    RunAbilityLoadoutError.DuplicatePlayer);
            }

            if (characterIds.Contains(identity.CharacterId))
            {
                return RunAbilityLoadoutResult.Failed(
                    RunAbilityLoadoutError.DuplicateCharacter);
            }

            if (participants.Count >= maximumParticipantCount)
            {
                return RunAbilityLoadoutResult.Failed(
                    RunAbilityLoadoutError.ParticipantLimitExceeded);
            }

            RunParticipantAbilityState state = new(identity, abilitySlotCount);
            participants.Add(identity.PlayerId, state);
            characterIds.Add(identity.CharacterId);
            StateChanged?.Invoke(state);
            return RunAbilityLoadoutResult.Succeeded(state);
        }

        public bool TryGetParticipant(
            string playerId,
            out RunParticipantAbilityState state)
        {
            state = null;
            string normalizedId = playerId?.Trim() ?? string.Empty;
            return normalizedId.Length > 0 &&
                   participants.TryGetValue(normalizedId, out state);
        }

        public RunAbilityLoadoutResult TryGrantAbility(
            string playerId,
            string abilityId)
        {
            if (!TryPrepareParticipantAndAbility(
                    playerId,
                    abilityId,
                    out RunParticipantAbilityState state,
                    out string normalizedAbilityId,
                    out RunAbilityLoadoutResult failure))
            {
                return failure;
            }

            if (state.HasAbility(normalizedAbilityId))
            {
                return RunAbilityLoadoutResult.Failed(
                    RunAbilityLoadoutError.AbilityAlreadyGranted,
                    state,
                    normalizedAbilityId);
            }

            state.GrantAbility(normalizedAbilityId);
            StateChanged?.Invoke(state);
            return RunAbilityLoadoutResult.Succeeded(
                state,
                normalizedAbilityId);
        }

        public RunAbilityLoadoutResult TryGrantAndAssignAbility(
            string playerId,
            string abilityId,
            int slotIndex)
        {
            if (!TryPrepareParticipantAndAbility(
                    playerId,
                    abilityId,
                    out RunParticipantAbilityState state,
                    out string normalizedAbilityId,
                    out RunAbilityLoadoutResult failure))
            {
                return failure;
            }

            if (!IsValidSlot(slotIndex))
            {
                return RunAbilityLoadoutResult.Failed(
                    RunAbilityLoadoutError.InvalidSlot,
                    state,
                    normalizedAbilityId,
                    slotIndex);
            }

            if (state.HasAbility(normalizedAbilityId))
            {
                return RunAbilityLoadoutResult.Failed(
                    RunAbilityLoadoutError.AbilityAlreadyGranted,
                    state,
                    normalizedAbilityId,
                    slotIndex);
            }

            state.GrantAbility(normalizedAbilityId);
            string replacedAbilityId =
                state.AssignAbility(normalizedAbilityId, slotIndex);
            StateChanged?.Invoke(state);
            return RunAbilityLoadoutResult.Succeeded(
                state,
                normalizedAbilityId,
                slotIndex,
                replacedAbilityId: replacedAbilityId);
        }

        public RunAbilityLoadoutResult TryAssignAbility(
            string playerId,
            string abilityId,
            int slotIndex)
        {
            if (!TryPrepareParticipantAndAbility(
                    playerId,
                    abilityId,
                    out RunParticipantAbilityState state,
                    out string normalizedAbilityId,
                    out RunAbilityLoadoutResult failure))
            {
                return failure;
            }

            if (!IsValidSlot(slotIndex))
            {
                return RunAbilityLoadoutResult.Failed(
                    RunAbilityLoadoutError.InvalidSlot,
                    state,
                    normalizedAbilityId,
                    slotIndex);
            }

            if (!state.HasAbility(normalizedAbilityId))
            {
                return RunAbilityLoadoutResult.Failed(
                    RunAbilityLoadoutError.AbilityNotGranted,
                    state,
                    normalizedAbilityId,
                    slotIndex);
            }

            state.TryFindAssignedSlot(
                normalizedAbilityId,
                out int previousSlotIndex);
            if (previousSlotIndex == slotIndex)
            {
                return RunAbilityLoadoutResult.Succeeded(
                    state,
                    normalizedAbilityId,
                    slotIndex,
                    previousSlotIndex,
                    changed: false);
            }

            string replacedAbilityId =
                state.AssignAbility(normalizedAbilityId, slotIndex);
            StateChanged?.Invoke(state);
            return RunAbilityLoadoutResult.Succeeded(
                state,
                normalizedAbilityId,
                slotIndex,
                previousSlotIndex,
                replacedAbilityId);
        }

        public RunAbilityLoadoutResult TryClearAbilitySlot(
            string playerId,
            int slotIndex)
        {
            if (!TryGetParticipant(playerId, out RunParticipantAbilityState state))
            {
                return RunAbilityLoadoutResult.Failed(
                    RunAbilityLoadoutError.ParticipantNotFound,
                    slotIndex: slotIndex);
            }

            if (!IsValidSlot(slotIndex))
            {
                return RunAbilityLoadoutResult.Failed(
                    RunAbilityLoadoutError.InvalidSlot,
                    state,
                    slotIndex: slotIndex);
            }

            state.TryGetAbilitySlot(slotIndex, out string abilityId);
            if (abilityId.Length == 0)
            {
                return RunAbilityLoadoutResult.Succeeded(
                    state,
                    slotIndex: slotIndex,
                    changed: false);
            }

            state.ClearAbilitySlot(slotIndex);
            StateChanged?.Invoke(state);
            return RunAbilityLoadoutResult.Succeeded(
                state,
                abilityId,
                slotIndex,
                replacedAbilityId: abilityId);
        }

        private bool TryPrepareParticipantAndAbility(
            string playerId,
            string abilityId,
            out RunParticipantAbilityState state,
            out string normalizedAbilityId,
            out RunAbilityLoadoutResult failure)
        {
            normalizedAbilityId = abilityId?.Trim() ?? string.Empty;
            if (!TryGetParticipant(playerId, out state))
            {
                failure = RunAbilityLoadoutResult.Failed(
                    RunAbilityLoadoutError.ParticipantNotFound,
                    abilityId: normalizedAbilityId);
                return false;
            }

            if (normalizedAbilityId.Length == 0)
            {
                failure = RunAbilityLoadoutResult.Failed(
                    RunAbilityLoadoutError.InvalidAbilityId,
                    state);
                return false;
            }

            failure = default;
            return true;
        }

        private bool IsValidSlot(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < abilitySlotCount;
        }
    }
}
