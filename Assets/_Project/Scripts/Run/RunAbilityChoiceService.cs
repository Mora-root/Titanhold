using System;
using System.Collections.Generic;

namespace Titanhold.Run
{
    public sealed class RunAbilityChoiceService
    {
        private readonly RunAbilityLoadoutService loadout;
        private readonly Dictionary<string, RunAbilityChoiceState>
            pendingChoices = new(StringComparer.Ordinal);
        private readonly Dictionary<string, HashSet<string>>
            resolvedChoiceIds = new(StringComparer.Ordinal);

        public RunAbilityChoiceService(RunAbilityLoadoutService loadout)
        {
            this.loadout = loadout ??
                throw new ArgumentNullException(nameof(loadout));
        }

        public int PendingChoiceCount => pendingChoices.Count;

        public event Action<RunAbilityChoiceState> ChoiceOffered;
        public event Action<RunAbilityChoiceState, string> ChoiceResolved;

        public bool TryGetPendingChoice(
            string playerId,
            out RunAbilityChoiceState choice)
        {
            choice = null;
            string normalizedId = playerId?.Trim() ?? string.Empty;
            return normalizedId.Length > 0 &&
                   pendingChoices.TryGetValue(normalizedId, out choice);
        }

        public bool HasResolvedChoice(string playerId, string choiceId)
        {
            string normalizedPlayerId = playerId?.Trim() ?? string.Empty;
            string normalizedChoiceId = choiceId?.Trim() ?? string.Empty;
            return normalizedPlayerId.Length > 0 &&
                   normalizedChoiceId.Length > 0 &&
                   resolvedChoiceIds.TryGetValue(
                       normalizedPlayerId,
                       out HashSet<string> choices) &&
                   choices.Contains(normalizedChoiceId);
        }

        public RunAbilityChoiceResult TryOfferChoice(
            RunAbilityChoiceRequest request)
        {
            if (request == null || request.PlayerId.Length == 0 ||
                request.ChoiceId.Length == 0 || request.OptionCount <= 0)
            {
                return RunAbilityChoiceResult.Failed(
                    RunAbilityChoiceError.InvalidRequest);
            }

            if (!loadout.TryGetParticipant(
                    request.PlayerId,
                    out RunParticipantAbilityState participant))
            {
                return RunAbilityChoiceResult.Failed(
                    RunAbilityChoiceError.ParticipantNotFound);
            }

            if (request.TargetSlotIndex < 0 ||
                request.TargetSlotIndex >= participant.AbilitySlotCount)
            {
                return RunAbilityChoiceResult.Failed(
                    RunAbilityChoiceError.InvalidSlot);
            }

            if (pendingChoices.ContainsKey(request.PlayerId))
            {
                return RunAbilityChoiceResult.Failed(
                    RunAbilityChoiceError.ChoiceAlreadyPending,
                    pendingChoices[request.PlayerId]);
            }

            if (HasResolvedChoice(request.PlayerId, request.ChoiceId))
            {
                return RunAbilityChoiceResult.Failed(
                    RunAbilityChoiceError.ChoiceAlreadyResolved);
            }

            if (!TryCollectEligibleAbilities(
                    participant,
                    request.CandidateAbilityIds,
                    out List<string> eligible))
            {
                return RunAbilityChoiceResult.Failed(
                    RunAbilityChoiceError.InvalidCandidatePool);
            }

            if (eligible.Count < request.OptionCount)
            {
                return RunAbilityChoiceResult.Failed(
                    RunAbilityChoiceError.InsufficientEligibleAbilities);
            }

            RollOptions(eligible, request.OptionCount, request.RollSeed);
            string[] offered = new string[request.OptionCount];
            eligible.CopyTo(0, offered, 0, offered.Length);
            RunAbilityChoiceState choice = new(
                request.PlayerId,
                request.ChoiceId,
                request.TargetSlotIndex,
                request.RollSeed,
                offered);
            pendingChoices.Add(request.PlayerId, choice);
            ChoiceOffered?.Invoke(choice);
            return RunAbilityChoiceResult.Succeeded(choice);
        }

        public RunAbilityChoiceResult TrySelectAbility(
            string playerId,
            string choiceId,
            string abilityId)
        {
            string normalizedPlayerId = playerId?.Trim() ?? string.Empty;
            string normalizedChoiceId = choiceId?.Trim() ?? string.Empty;
            string normalizedAbilityId = abilityId?.Trim() ?? string.Empty;
            if (normalizedPlayerId.Length == 0 ||
                normalizedChoiceId.Length == 0 ||
                normalizedAbilityId.Length == 0)
            {
                return RunAbilityChoiceResult.Failed(
                    RunAbilityChoiceError.InvalidRequest);
            }

            if (!loadout.TryGetParticipant(normalizedPlayerId, out _))
            {
                return RunAbilityChoiceResult.Failed(
                    RunAbilityChoiceError.ParticipantNotFound);
            }

            if (!pendingChoices.TryGetValue(
                    normalizedPlayerId,
                    out RunAbilityChoiceState choice))
            {
                return RunAbilityChoiceResult.Failed(
                    HasResolvedChoice(normalizedPlayerId, normalizedChoiceId)
                        ? RunAbilityChoiceError.ChoiceAlreadyResolved
                        : RunAbilityChoiceError.ChoiceNotFound);
            }

            if (!string.Equals(
                    choice.ChoiceId,
                    normalizedChoiceId,
                    StringComparison.Ordinal))
            {
                return RunAbilityChoiceResult.Failed(
                    RunAbilityChoiceError.ChoiceMismatch,
                    choice);
            }

            if (!choice.ContainsAbility(normalizedAbilityId))
            {
                return RunAbilityChoiceResult.Failed(
                    RunAbilityChoiceError.AbilityNotOffered,
                    choice);
            }

            pendingChoices.Remove(normalizedPlayerId);
            GetOrCreateResolvedChoices(normalizedPlayerId).Add(
                normalizedChoiceId);
            RunAbilityLoadoutResult assignment =
                loadout.TryGrantAndAssignAbility(
                    normalizedPlayerId,
                    normalizedAbilityId,
                    choice.TargetSlotIndex);
            if (!assignment.Success)
            {
                GetOrCreateResolvedChoices(normalizedPlayerId).Remove(
                    normalizedChoiceId);
                pendingChoices.Add(normalizedPlayerId, choice);
                return RunAbilityChoiceResult.Failed(
                    RunAbilityChoiceError.LoadoutRejected,
                    choice,
                    assignment.Error);
            }

            ChoiceResolved?.Invoke(choice, normalizedAbilityId);
            return RunAbilityChoiceResult.Succeeded(
                choice,
                normalizedAbilityId);
        }

        private HashSet<string> GetOrCreateResolvedChoices(string playerId)
        {
            if (!resolvedChoiceIds.TryGetValue(
                    playerId,
                    out HashSet<string> choices))
            {
                choices = new HashSet<string>(StringComparer.Ordinal);
                resolvedChoiceIds.Add(playerId, choices);
            }

            return choices;
        }

        private static bool TryCollectEligibleAbilities(
            RunParticipantAbilityState participant,
            IReadOnlyList<string> candidates,
            out List<string> eligible)
        {
            eligible = new List<string>();
            if (candidates == null || candidates.Count == 0)
                return false;

            HashSet<string> seen = new(StringComparer.Ordinal);
            for (int i = 0; i < candidates.Count; i++)
            {
                string abilityId = candidates[i];
                if (string.IsNullOrWhiteSpace(abilityId) ||
                    !seen.Add(abilityId))
                {
                    eligible.Clear();
                    return false;
                }

                if (!participant.HasAbility(abilityId))
                    eligible.Add(abilityId);
            }

            return true;
        }

        private static void RollOptions(
            IList<string> abilities,
            int optionCount,
            int seed)
        {
            DeterministicRandom random = new(seed);
            for (int i = 0; i < optionCount; i++)
            {
                int selectedIndex = i + random.Next(abilities.Count - i);
                (abilities[i], abilities[selectedIndex]) =
                    (abilities[selectedIndex], abilities[i]);
            }
        }

        private struct DeterministicRandom
        {
            private uint state;

            public DeterministicRandom(int seed)
            {
                state = unchecked((uint)seed);
                if (state == 0)
                    state = 0x6D2B79F5u;
            }

            public int Next(int maximumExclusive)
            {
                if (maximumExclusive <= 0)
                    throw new ArgumentOutOfRangeException(
                        nameof(maximumExclusive));

                uint value = NextUInt();
                return (int)(value % (uint)maximumExclusive);
            }

            private uint NextUInt()
            {
                uint value = state;
                value ^= value << 13;
                value ^= value >> 17;
                value ^= value << 5;
                state = value;
                return value;
            }
        }
    }
}
