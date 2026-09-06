using System;
using System.Collections.Generic;

namespace Titanhold.Run
{
    public sealed class RunStartingAbilitySelectionService
    {
        public const int StartingAbilityOptionCount = 3;

        private readonly RunAbilityChoiceService choices;
        private readonly RunStartReadinessService readiness;

        public RunStartingAbilitySelectionService(
            RunAbilityChoiceService choices,
            RunStartReadinessService readiness)
        {
            this.choices = choices ??
                throw new ArgumentNullException(nameof(choices));
            this.readiness = readiness ??
                throw new ArgumentNullException(nameof(readiness));
        }

        public RunStartingAbilitySelectionResult TryOfferStartingChoice(
            RunStartingAbilityChoiceRequest request)
        {
            if (request == null || request.PlayerId.Length == 0 ||
                request.ChoiceId.Length == 0)
            {
                return RunStartingAbilitySelectionResult.Failed(
                    RunStartingAbilitySelectionError.InvalidRequest);
            }

            if (!HasValidStartingPool(request.CandidateAbilityIds))
            {
                return RunStartingAbilitySelectionResult.Failed(
                    RunStartingAbilitySelectionError.InvalidStartingPool);
            }

            if (!TryPrepareParticipant(
                    request.PlayerId,
                    out RunParticipantStartReadinessState participant,
                    out RunStartingAbilitySelectionResult failure))
            {
                return failure;
            }

            RunAbilityChoiceResult offered = choices.TryOfferChoice(
                new RunAbilityChoiceRequest(
                    participant.PlayerId,
                    request.ChoiceId,
                    RunStartReadinessService.StartingAbilitySlotIndex,
                    request.CandidateAbilityIds,
                    StartingAbilityOptionCount,
                    request.RollSeed));
            if (!offered.Success)
            {
                return RunStartingAbilitySelectionResult.Failed(
                    RunStartingAbilitySelectionError.ChoiceRejected,
                    offered.State,
                    offered.Error);
            }

            return RunStartingAbilitySelectionResult.Succeeded(
                offered.State);
        }

        public RunStartingAbilitySelectionResult TrySelectStartingAbility(
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
                return RunStartingAbilitySelectionResult.Failed(
                    RunStartingAbilitySelectionError.InvalidRequest);
            }

            if (!TryPrepareParticipant(
                    normalizedPlayerId,
                    out _,
                    out RunStartingAbilitySelectionResult failure))
            {
                return failure;
            }

            if (choices.TryGetPendingChoice(
                    normalizedPlayerId,
                    out RunAbilityChoiceState pending) &&
                pending.TargetSlotIndex !=
                    RunStartReadinessService.StartingAbilitySlotIndex)
            {
                return RunStartingAbilitySelectionResult.Failed(
                    RunStartingAbilitySelectionError.ChoiceTargetsWrongSlot,
                    pending);
            }

            RunAbilityChoiceResult selected = choices.TrySelectAbility(
                normalizedPlayerId,
                normalizedChoiceId,
                normalizedAbilityId);
            if (!selected.Success)
            {
                return RunStartingAbilitySelectionResult.Failed(
                    RunStartingAbilitySelectionError.ChoiceRejected,
                    selected.State,
                    selected.Error);
            }

            RunStartReadinessResult confirmed =
                readiness.TryConfirmStartingAbility(
                    normalizedPlayerId,
                    normalizedAbilityId);
            if (!confirmed.Success)
            {
                return RunStartingAbilitySelectionResult.Failed(
                    RunStartingAbilitySelectionError.ReadinessRejected,
                    selected.State,
                    readinessError: confirmed.Error);
            }

            bool rosterSealed = readiness.IsSealed;
            if (readiness.AllParticipantsConfirmed && !rosterSealed)
            {
                RunStartReadinessResult sealedResult = readiness.TrySeal();
                if (!sealedResult.Success)
                {
                    return RunStartingAbilitySelectionResult.Failed(
                        RunStartingAbilitySelectionError.ReadinessRejected,
                        selected.State,
                        readinessError: sealedResult.Error);
                }

                rosterSealed = true;
            }

            return RunStartingAbilitySelectionResult.Succeeded(
                selected.State,
                selected.SelectedAbilityId,
                rosterSealed);
        }

        private bool TryPrepareParticipant(
            string playerId,
            out RunParticipantStartReadinessState participant,
            out RunStartingAbilitySelectionResult failure)
        {
            if (!readiness.TryGetParticipant(playerId, out participant))
            {
                failure = RunStartingAbilitySelectionResult.Failed(
                    RunStartingAbilitySelectionError.ParticipantNotFound);
                return false;
            }

            if (readiness.IsSealed)
            {
                failure = RunStartingAbilitySelectionResult.Failed(
                    RunStartingAbilitySelectionError.ReadinessAlreadySealed);
                return false;
            }

            if (participant.IsConfirmed)
            {
                failure = RunStartingAbilitySelectionResult.Failed(
                    RunStartingAbilitySelectionError.ParticipantAlreadyConfirmed);
                return false;
            }

            failure = default;
            return true;
        }

        private static bool HasValidStartingPool(
            IReadOnlyList<string> candidateAbilityIds)
        {
            if (candidateAbilityIds == null ||
                candidateAbilityIds.Count != StartingAbilityOptionCount)
            {
                return false;
            }

            HashSet<string> uniqueIds = new(StringComparer.Ordinal);
            for (int i = 0; i < candidateAbilityIds.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(candidateAbilityIds[i]) ||
                    !uniqueIds.Add(candidateAbilityIds[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
