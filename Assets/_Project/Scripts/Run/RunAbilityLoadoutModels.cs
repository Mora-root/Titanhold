using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Titanhold.Run
{
    public sealed class RunParticipantAbilityState
    {
        private readonly List<string> grantedAbilityIds = new();
        private readonly HashSet<string> grantedAbilities =
            new(StringComparer.Ordinal);
        private readonly string[] abilitySlotIds;
        private readonly ReadOnlyCollection<string> grantedAbilityView;
        private readonly ReadOnlyCollection<string> abilitySlotView;

        internal RunParticipantAbilityState(
            RunParticipantIdentity identity,
            int abilitySlotCount)
        {
            if (!identity.IsValid)
                throw new ArgumentException("A valid participant is required.", nameof(identity));
            if (abilitySlotCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(abilitySlotCount));

            PlayerId = identity.PlayerId;
            CharacterId = identity.CharacterId;
            abilitySlotIds = new string[abilitySlotCount];
            grantedAbilityView = grantedAbilityIds.AsReadOnly();
            abilitySlotView = Array.AsReadOnly(abilitySlotIds);
        }

        public string PlayerId { get; }
        public string CharacterId { get; }
        public int AbilitySlotCount => abilitySlotIds.Length;
        public int GrantedAbilityCount => grantedAbilityIds.Count;
        public IReadOnlyList<string> GrantedAbilityIds => grantedAbilityView;
        public IReadOnlyList<string> AbilitySlotIds => abilitySlotView;

        public bool HasAbility(string abilityId)
        {
            string normalizedId = abilityId?.Trim() ?? string.Empty;
            return normalizedId.Length > 0 &&
                   grantedAbilities.Contains(normalizedId);
        }

        public bool TryGetAbilitySlot(int slotIndex, out string abilityId)
        {
            abilityId = string.Empty;
            if (slotIndex < 0 || slotIndex >= abilitySlotIds.Length)
                return false;

            abilityId = abilitySlotIds[slotIndex] ?? string.Empty;
            return true;
        }

        public bool TryFindAssignedSlot(string abilityId, out int slotIndex)
        {
            slotIndex = -1;
            string normalizedId = abilityId?.Trim() ?? string.Empty;
            if (normalizedId.Length == 0)
                return false;

            for (int i = 0; i < abilitySlotIds.Length; i++)
            {
                if (!string.Equals(
                        abilitySlotIds[i],
                        normalizedId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                slotIndex = i;
                return true;
            }

            return false;
        }

        internal void GrantAbility(string abilityId)
        {
            grantedAbilities.Add(abilityId);
            grantedAbilityIds.Add(abilityId);
        }

        internal string AssignAbility(string abilityId, int slotIndex)
        {
            string replacedAbilityId = abilitySlotIds[slotIndex] ?? string.Empty;
            if (TryFindAssignedSlot(abilityId, out int previousSlotIndex))
                abilitySlotIds[previousSlotIndex] = string.Empty;

            abilitySlotIds[slotIndex] = abilityId;
            return replacedAbilityId;
        }

        internal string ClearAbilitySlot(int slotIndex)
        {
            string removedAbilityId = abilitySlotIds[slotIndex] ?? string.Empty;
            abilitySlotIds[slotIndex] = string.Empty;
            return removedAbilityId;
        }
    }

    public enum RunAbilityLoadoutError
    {
        None,
        InvalidParticipant,
        DuplicatePlayer,
        DuplicateCharacter,
        ParticipantLimitExceeded,
        ParticipantNotFound,
        InvalidAbilityId,
        AbilityAlreadyGranted,
        AbilityNotGranted,
        InvalidSlot
    }

    public readonly struct RunAbilityLoadoutResult
    {
        private RunAbilityLoadoutResult(
            bool success,
            RunAbilityLoadoutError error,
            RunParticipantAbilityState state,
            string abilityId,
            int slotIndex,
            int previousSlotIndex,
            string replacedAbilityId,
            bool changed)
        {
            Success = success;
            Error = error;
            State = state;
            AbilityId = abilityId ?? string.Empty;
            SlotIndex = slotIndex;
            PreviousSlotIndex = previousSlotIndex;
            ReplacedAbilityId = replacedAbilityId ?? string.Empty;
            Changed = changed;
        }

        public bool Success { get; }
        public RunAbilityLoadoutError Error { get; }
        public RunParticipantAbilityState State { get; }
        public string AbilityId { get; }
        public int SlotIndex { get; }
        public int PreviousSlotIndex { get; }
        public string ReplacedAbilityId { get; }
        public bool Changed { get; }

        public static RunAbilityLoadoutResult Succeeded(
            RunParticipantAbilityState state,
            string abilityId = "",
            int slotIndex = -1,
            int previousSlotIndex = -1,
            string replacedAbilityId = "",
            bool changed = true)
        {
            return new RunAbilityLoadoutResult(
                true,
                RunAbilityLoadoutError.None,
                state,
                abilityId,
                slotIndex,
                previousSlotIndex,
                replacedAbilityId,
                changed);
        }

        public static RunAbilityLoadoutResult Failed(
            RunAbilityLoadoutError error,
            RunParticipantAbilityState state = null,
            string abilityId = "",
            int slotIndex = -1)
        {
            return new RunAbilityLoadoutResult(
                false,
                error,
                state,
                abilityId,
                slotIndex,
                -1,
                string.Empty,
                false);
        }
    }
}
