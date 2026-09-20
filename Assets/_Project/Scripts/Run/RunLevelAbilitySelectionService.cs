using System;
using System.Collections.Generic;

namespace Titanhold.Run
{
    public enum RunLevelAbilitySelectionError
    {
        None,
        ServiceDisposed,
        InvalidPlayerId,
        ParticipantNotConfigured,
        ProgressionParticipantNotFound,
        ChoiceRejected
    }

    public readonly struct RunLevelAbilitySelectionResult
    {
        private RunLevelAbilitySelectionResult(
            bool success,
            bool offered,
            RunLevelAbilitySelectionError error,
            RunAbilityChoiceState choice,
            RunAbilityChoiceError choiceError)
        {
            Success = success;
            Offered = offered;
            Error = error;
            Choice = choice;
            ChoiceError = choiceError;
        }

        public bool Success { get; }
        public bool Offered { get; }
        public RunLevelAbilitySelectionError Error { get; }
        public RunAbilityChoiceState Choice { get; }
        public RunAbilityChoiceError ChoiceError { get; }

        internal static RunLevelAbilitySelectionResult NoChange(
            RunAbilityChoiceState choice = null)
        {
            return new RunLevelAbilitySelectionResult(
                true,
                false,
                RunLevelAbilitySelectionError.None,
                choice,
                RunAbilityChoiceError.None);
        }

        internal static RunLevelAbilitySelectionResult OfferedChoice(
            RunAbilityChoiceState choice)
        {
            return new RunLevelAbilitySelectionResult(
                true,
                true,
                RunLevelAbilitySelectionError.None,
                choice,
                RunAbilityChoiceError.None);
        }

        internal static RunLevelAbilitySelectionResult Failed(
            RunLevelAbilitySelectionError error,
            RunAbilityChoiceError choiceError =
                RunAbilityChoiceError.None,
            RunAbilityChoiceState choice = null)
        {
            return new RunLevelAbilitySelectionResult(
                false,
                false,
                error,
                choice,
                choiceError);
        }
    }

    public sealed class RunLevelAbilitySelectionService : IDisposable
    {
        public const string ChoiceIdPrefix = "choice:run-level-ability";

        private readonly RunProgressionService progression;
        private readonly RunAbilityChoiceService choices;
        private readonly Dictionary<string, RunAbilityUnlockParticipantPlan>
            plans = new(StringComparer.Ordinal);
        private readonly int runSeed;
        private bool disposed;

        public RunLevelAbilitySelectionService(
            RunProgressionService progression,
            RunAbilityChoiceService choices,
            IReadOnlyList<RunAbilityUnlockParticipantPlan> participantPlans,
            int runSeed)
        {
            this.progression = progression ??
                throw new ArgumentNullException(nameof(progression));
            this.choices = choices ??
                throw new ArgumentNullException(nameof(choices));
            if (participantPlans == null || participantPlans.Count == 0)
            {
                throw new ArgumentException(
                    "At least one participant ability plan is required.",
                    nameof(participantPlans));
            }

            for (int i = 0; i < participantPlans.Count; i++)
            {
                RunAbilityUnlockParticipantPlan plan = participantPlans[i];
                if (plan == null || !plan.IsValid)
                {
                    throw new ArgumentException(
                        $"Participant ability plan {i} is invalid.",
                        nameof(participantPlans));
                }

                if (!progression.TryGetParticipant(plan.PlayerId, out _))
                {
                    throw new ArgumentException(
                        $"Progression participant '{plan.PlayerId}' is missing.",
                        nameof(participantPlans));
                }

                if (!plans.TryAdd(plan.PlayerId, plan))
                {
                    throw new ArgumentException(
                        $"Participant '{plan.PlayerId}' has more than one ability plan.",
                        nameof(participantPlans));
                }
            }

            this.runSeed = runSeed;
            progression.StateChanged += HandleProgressionChanged;
            choices.ChoiceResolved += HandleChoiceResolved;
        }

        public event Action<string, RunLevelAbilitySelectionResult>
            OfferFailed;

        public RunLevelAbilitySelectionResult TryOfferNext(string playerId)
        {
            if (disposed)
            {
                return RunLevelAbilitySelectionResult.Failed(
                    RunLevelAbilitySelectionError.ServiceDisposed);
            }

            string normalizedPlayerId = playerId?.Trim() ?? string.Empty;
            if (normalizedPlayerId.Length == 0)
            {
                return RunLevelAbilitySelectionResult.Failed(
                    RunLevelAbilitySelectionError.InvalidPlayerId);
            }

            if (!plans.TryGetValue(
                    normalizedPlayerId,
                    out RunAbilityUnlockParticipantPlan plan))
            {
                return RunLevelAbilitySelectionResult.Failed(
                    RunLevelAbilitySelectionError.ParticipantNotConfigured);
            }

            if (!progression.TryGetParticipant(
                    normalizedPlayerId,
                    out RunParticipantProgressionState state))
            {
                return RunLevelAbilitySelectionResult.Failed(
                    RunLevelAbilitySelectionError
                        .ProgressionParticipantNotFound);
            }

            if (choices.TryGetPendingChoice(
                    normalizedPlayerId,
                    out RunAbilityChoiceState pending))
            {
                return RunLevelAbilitySelectionResult.NoChange(pending);
            }

            for (int i = 0; i < plan.Schedule.Milestones.Count; i++)
            {
                RunAbilityUnlockMilestone milestone =
                    plan.Schedule.Milestones[i];
                if (milestone.UnlockLevel > state.Level)
                    break;

                string choiceId = CreateChoiceId(
                    plan.Schedule.ScheduleId,
                    milestone);
                if (choices.HasResolvedChoice(
                        normalizedPlayerId,
                        choiceId))
                {
                    continue;
                }

                RunAbilityChoiceResult offered = choices.TryOfferChoice(
                    new RunAbilityChoiceRequest(
                        normalizedPlayerId,
                        choiceId,
                        ResolveTargetSlot(
                            normalizedPlayerId,
                            milestone.TargetSlotIndex),
                        milestone.CandidateAbilityIds,
                        milestone.OptionCount,
                        CreateChoiceSeed(
                            runSeed,
                            plan.ParticipantIndex,
                            milestone)));
                if (offered.Success)
                {
                    return RunLevelAbilitySelectionResult.OfferedChoice(
                        offered.State);
                }

                return RunLevelAbilitySelectionResult.Failed(
                    RunLevelAbilitySelectionError.ChoiceRejected,
                    offered.Error,
                    offered.State);
            }

            return RunLevelAbilitySelectionResult.NoChange();
        }

        private int ResolveTargetSlot(
            string playerId,
            int authoredFallbackSlotIndex)
        {
            return choices.TryFindFirstAvailableSlot(
                playerId,
                minimumSlotIndex: 1,
                out int firstAvailableSlotIndex)
                    ? firstAvailableSlotIndex
                    : authoredFallbackSlotIndex;
        }

        public static string CreateChoiceId(
            string scheduleId,
            RunAbilityUnlockMilestone milestone)
        {
            if (milestone == null)
                return string.Empty;

            string normalizedScheduleId = scheduleId?.Trim() ?? string.Empty;
            return normalizedScheduleId.Length == 0
                ? string.Empty
                : $"{ChoiceIdPrefix}:{normalizedScheduleId}:" +
                  $"level:{milestone.UnlockLevel}:" +
                  $"slot:{milestone.TargetSlotIndex}";
        }

        public static bool IsRunLevelChoiceId(string choiceId)
        {
            string normalizedChoiceId = choiceId?.Trim() ?? string.Empty;
            return normalizedChoiceId.StartsWith(
                $"{ChoiceIdPrefix}:",
                StringComparison.Ordinal);
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            progression.StateChanged -= HandleProgressionChanged;
            choices.ChoiceResolved -= HandleChoiceResolved;
        }

        private void HandleProgressionChanged(
            RunParticipantProgressionState state)
        {
            if (state == null || !plans.ContainsKey(state.PlayerId))
                return;

            ReportFailure(state.PlayerId, TryOfferNext(state.PlayerId));
        }

        private void HandleChoiceResolved(
            RunAbilityChoiceState choice,
            string selectedAbilityId)
        {
            if (choice == null || !plans.ContainsKey(choice.PlayerId))
                return;

            ReportFailure(choice.PlayerId, TryOfferNext(choice.PlayerId));
        }

        private void ReportFailure(
            string playerId,
            RunLevelAbilitySelectionResult result)
        {
            if (!result.Success)
                OfferFailed?.Invoke(playerId, result);
        }

        private static int CreateChoiceSeed(
            int baseSeed,
            int participantIndex,
            RunAbilityUnlockMilestone milestone)
        {
            unchecked
            {
                int seed = baseSeed;
                seed = (seed * 397) ^ participantIndex;
                seed = (seed * 397) ^ milestone.UnlockLevel;
                seed = (seed * 397) ^ milestone.TargetSlotIndex;
                return seed;
            }
        }
    }
}
