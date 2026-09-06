using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Titanhold.Run
{
    public sealed class RunStartingAbilityChoiceRequest
    {
        private readonly ReadOnlyCollection<string> candidateAbilityIds;

        public RunStartingAbilityChoiceRequest(
            string playerId,
            string choiceId,
            IReadOnlyList<string> candidateAbilityIds,
            int rollSeed)
        {
            PlayerId = playerId?.Trim() ?? string.Empty;
            ChoiceId = choiceId?.Trim() ?? string.Empty;
            RollSeed = rollSeed;

            int count = candidateAbilityIds?.Count ?? 0;
            string[] copy = new string[count];
            for (int i = 0; i < count; i++)
                copy[i] = candidateAbilityIds[i]?.Trim() ?? string.Empty;

            this.candidateAbilityIds = Array.AsReadOnly(copy);
        }

        public string PlayerId { get; }
        public string ChoiceId { get; }
        public int RollSeed { get; }
        public IReadOnlyList<string> CandidateAbilityIds =>
            candidateAbilityIds;
    }

    public enum RunStartingAbilitySelectionError
    {
        None,
        InvalidRequest,
        InvalidStartingPool,
        ParticipantNotFound,
        StartingPoolNotFound,
        ParticipantAlreadyConfirmed,
        ReadinessAlreadySealed,
        ChoiceTargetsWrongSlot,
        ChoiceRejected,
        ReadinessRejected
    }

    public readonly struct RunStartingAbilitySelectionResult
    {
        private RunStartingAbilitySelectionResult(
            bool success,
            RunStartingAbilitySelectionError error,
            RunAbilityChoiceState choice,
            string selectedAbilityId,
            bool rosterSealed,
            RunAbilityChoiceError choiceError,
            RunStartReadinessError readinessError)
        {
            Success = success;
            Error = error;
            Choice = choice;
            SelectedAbilityId = selectedAbilityId ?? string.Empty;
            RosterSealed = rosterSealed;
            ChoiceError = choiceError;
            ReadinessError = readinessError;
        }

        public bool Success { get; }
        public RunStartingAbilitySelectionError Error { get; }
        public RunAbilityChoiceState Choice { get; }
        public string SelectedAbilityId { get; }
        public bool RosterSealed { get; }
        public RunAbilityChoiceError ChoiceError { get; }
        public RunStartReadinessError ReadinessError { get; }

        public static RunStartingAbilitySelectionResult Succeeded(
            RunAbilityChoiceState choice,
            string selectedAbilityId = "",
            bool rosterSealed = false)
        {
            return new RunStartingAbilitySelectionResult(
                true,
                RunStartingAbilitySelectionError.None,
                choice,
                selectedAbilityId,
                rosterSealed,
                RunAbilityChoiceError.None,
                RunStartReadinessError.None);
        }

        public static RunStartingAbilitySelectionResult Failed(
            RunStartingAbilitySelectionError error,
            RunAbilityChoiceState choice = null,
            RunAbilityChoiceError choiceError =
                RunAbilityChoiceError.None,
            RunStartReadinessError readinessError =
                RunStartReadinessError.None)
        {
            return new RunStartingAbilitySelectionResult(
                false,
                error,
                choice,
                string.Empty,
                false,
                choiceError,
                readinessError);
        }
    }
}
