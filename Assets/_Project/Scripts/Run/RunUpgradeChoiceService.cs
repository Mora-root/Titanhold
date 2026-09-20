using System;
using System.Collections.Generic;

namespace Titanhold.Run
{
    public sealed class RunUpgradeChoiceService
    {
        public const int DefaultMaximumParticipantCount = 8;

        private readonly IRunUpgradeDefinitionResolver definitions;
        private readonly int maximumParticipantCount;
        private readonly Dictionary<string, RunParticipantUpgradeState>
            participants = new(StringComparer.Ordinal);
        private readonly HashSet<string> characterIds =
            new(StringComparer.Ordinal);
        private readonly Dictionary<string, RunUpgradeChoiceState>
            pendingChoices = new(StringComparer.Ordinal);
        private readonly Dictionary<string, HashSet<string>> resolvedChoiceIds =
            new(StringComparer.Ordinal);

        public RunUpgradeChoiceService(
            IRunUpgradeDefinitionResolver definitions,
            int maximumParticipantCount =
                DefaultMaximumParticipantCount)
        {
            this.definitions = definitions ??
                throw new ArgumentNullException(nameof(definitions));
            if (maximumParticipantCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumParticipantCount));
            }

            this.maximumParticipantCount = maximumParticipantCount;
        }

        public int ParticipantCount => participants.Count;
        public int PendingChoiceCount => pendingChoices.Count;

        public event Action<RunUpgradeChoiceState> ChoiceOffered;
        public event Action<
            RunUpgradeChoiceState,
            string,
            RunParticipantUpgradeState> ChoiceResolved;

        public RunUpgradeChoiceResult TryRegisterParticipant(
            RunParticipantIdentity identity)
        {
            if (!identity.IsValid)
            {
                return RunUpgradeChoiceResult.Failed(
                    RunUpgradeChoiceError.InvalidParticipant);
            }

            if (participants.ContainsKey(identity.PlayerId))
            {
                return RunUpgradeChoiceResult.Failed(
                    RunUpgradeChoiceError.DuplicatePlayer);
            }

            if (characterIds.Contains(identity.CharacterId))
            {
                return RunUpgradeChoiceResult.Failed(
                    RunUpgradeChoiceError.DuplicateCharacter);
            }

            if (participants.Count >= maximumParticipantCount)
            {
                return RunUpgradeChoiceResult.Failed(
                    RunUpgradeChoiceError.ParticipantLimitExceeded);
            }

            RunParticipantUpgradeState state = new(identity);
            participants.Add(identity.PlayerId, state);
            characterIds.Add(identity.CharacterId);
            return RunUpgradeChoiceResult.Succeeded(state);
        }

        public bool TryGetParticipant(
            string playerId,
            out RunParticipantUpgradeState participant)
        {
            participant = null;
            string normalizedId = playerId?.Trim() ?? string.Empty;
            return normalizedId.Length > 0 &&
                   participants.TryGetValue(normalizedId, out participant);
        }

        public bool TryGetPendingChoice(
            string playerId,
            out RunUpgradeChoiceState choice)
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

        public RunUpgradeChoiceResult TryOfferChoice(
            RunUpgradeChoiceRequest request)
        {
            if (request == null || request.PlayerId.Length == 0 ||
                request.ChoiceId.Length == 0 || request.OptionCount <= 0)
            {
                return RunUpgradeChoiceResult.Failed(
                    RunUpgradeChoiceError.InvalidRequest);
            }

            if (!participants.TryGetValue(
                    request.PlayerId,
                    out RunParticipantUpgradeState participant))
            {
                return RunUpgradeChoiceResult.Failed(
                    RunUpgradeChoiceError.ParticipantNotFound);
            }

            if (pendingChoices.TryGetValue(
                    request.PlayerId,
                    out RunUpgradeChoiceState pending))
            {
                return RunUpgradeChoiceResult.Failed(
                    RunUpgradeChoiceError.ChoiceAlreadyPending,
                    participant,
                    pending);
            }

            if (HasResolvedChoice(request.PlayerId, request.ChoiceId))
            {
                return RunUpgradeChoiceResult.Failed(
                    RunUpgradeChoiceError.ChoiceAlreadyResolved,
                    participant);
            }

            if (!TryValidateCandidates(
                    request.CandidateUpgradeIds,
                    out List<string> candidates,
                    out RunUpgradeChoiceError candidateError))
            {
                return RunUpgradeChoiceResult.Failed(
                    candidateError,
                    participant);
            }

            if (candidates.Count < request.OptionCount)
            {
                return RunUpgradeChoiceResult.Failed(
                    RunUpgradeChoiceError.InsufficientCandidates,
                    participant);
            }

            RollOptions(candidates, request.OptionCount, request.RollSeed);
            string[] offered = new string[request.OptionCount];
            candidates.CopyTo(0, offered, 0, offered.Length);
            RunUpgradeChoiceState choice = new(
                request.PlayerId,
                request.ChoiceId,
                request.RollSeed,
                offered);
            pendingChoices.Add(request.PlayerId, choice);
            ChoiceOffered?.Invoke(choice);
            return RunUpgradeChoiceResult.Succeeded(
                participant,
                choice);
        }

        public RunUpgradeChoiceResult TrySelectUpgrade(
            string playerId,
            string choiceId,
            string upgradeId)
        {
            string normalizedPlayerId = playerId?.Trim() ?? string.Empty;
            string normalizedChoiceId = choiceId?.Trim() ?? string.Empty;
            string normalizedUpgradeId = upgradeId?.Trim() ?? string.Empty;
            if (normalizedPlayerId.Length == 0 ||
                normalizedChoiceId.Length == 0 ||
                normalizedUpgradeId.Length == 0)
            {
                return RunUpgradeChoiceResult.Failed(
                    RunUpgradeChoiceError.InvalidRequest);
            }

            if (!participants.TryGetValue(
                    normalizedPlayerId,
                    out RunParticipantUpgradeState participant))
            {
                return RunUpgradeChoiceResult.Failed(
                    RunUpgradeChoiceError.ParticipantNotFound);
            }

            if (!pendingChoices.TryGetValue(
                    normalizedPlayerId,
                    out RunUpgradeChoiceState choice))
            {
                return RunUpgradeChoiceResult.Failed(
                    HasResolvedChoice(normalizedPlayerId, normalizedChoiceId)
                        ? RunUpgradeChoiceError.ChoiceAlreadyResolved
                        : RunUpgradeChoiceError.ChoiceNotFound,
                    participant);
            }

            if (!string.Equals(
                    choice.ChoiceId,
                    normalizedChoiceId,
                    StringComparison.Ordinal))
            {
                return RunUpgradeChoiceResult.Failed(
                    RunUpgradeChoiceError.ChoiceMismatch,
                    participant,
                    choice);
            }

            if (!choice.ContainsUpgrade(normalizedUpgradeId))
            {
                return RunUpgradeChoiceResult.Failed(
                    RunUpgradeChoiceError.UpgradeNotOffered,
                    participant,
                    choice);
            }

            if (!participant.TryAddStack(normalizedUpgradeId))
            {
                return RunUpgradeChoiceResult.Failed(
                    RunUpgradeChoiceError.StackOverflow,
                    participant,
                    choice);
            }

            pendingChoices.Remove(normalizedPlayerId);
            GetOrCreateResolvedChoices(normalizedPlayerId).Add(
                normalizedChoiceId);
            ChoiceResolved?.Invoke(
                choice,
                normalizedUpgradeId,
                participant);
            return RunUpgradeChoiceResult.Succeeded(
                participant,
                choice,
                normalizedUpgradeId);
        }

        private bool TryValidateCandidates(
            IReadOnlyList<string> source,
            out List<string> candidates,
            out RunUpgradeChoiceError error)
        {
            candidates = new List<string>();
            error = RunUpgradeChoiceError.None;
            if (source == null || source.Count == 0)
            {
                error = RunUpgradeChoiceError.InvalidCandidatePool;
                return false;
            }

            HashSet<string> seen = new(StringComparer.Ordinal);
            for (int i = 0; i < source.Count; i++)
            {
                string upgradeId = source[i];
                if (string.IsNullOrWhiteSpace(upgradeId) ||
                    !string.Equals(
                        upgradeId,
                        upgradeId.Trim(),
                        StringComparison.Ordinal) ||
                    !seen.Add(upgradeId))
                {
                    candidates.Clear();
                    error = RunUpgradeChoiceError.InvalidCandidatePool;
                    return false;
                }

                if (!definitions.TryResolve(upgradeId, out _))
                {
                    candidates.Clear();
                    error = RunUpgradeChoiceError.UpgradeNotFound;
                    return false;
                }

                candidates.Add(upgradeId);
            }

            return true;
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

        private static void RollOptions(
            IList<string> candidates,
            int optionCount,
            int seed)
        {
            DeterministicRandom random = new(seed);
            for (int i = 0; i < optionCount; i++)
            {
                int selectedIndex = i +
                    random.Next(candidates.Count - i);
                (candidates[i], candidates[selectedIndex]) =
                    (candidates[selectedIndex], candidates[i]);
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
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(maximumExclusive));
                }

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
