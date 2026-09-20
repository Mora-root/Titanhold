using System;
using System.Collections.Generic;

namespace Titanhold.Run
{
    public enum RunLevelUpgradeSelectionError
    {
        None,
        ServiceDisposed,
        InvalidPlayerId,
        ParticipantNotConfigured,
        ProgressionParticipantNotFound,
        ChoiceRejected
    }

    public readonly struct RunLevelUpgradeSelectionResult
    {
        private RunLevelUpgradeSelectionResult(
            bool success,
            bool offered,
            RunLevelUpgradeSelectionError error,
            RunUpgradeChoiceState choice,
            RunUpgradeChoiceError choiceError)
        {
            Success = success;
            Offered = offered;
            Error = error;
            Choice = choice;
            ChoiceError = choiceError;
        }

        public bool Success { get; }
        public bool Offered { get; }
        public RunLevelUpgradeSelectionError Error { get; }
        public RunUpgradeChoiceState Choice { get; }
        public RunUpgradeChoiceError ChoiceError { get; }

        internal static RunLevelUpgradeSelectionResult NoChange(
            RunUpgradeChoiceState choice = null)
        {
            return new RunLevelUpgradeSelectionResult(
                true,
                false,
                RunLevelUpgradeSelectionError.None,
                choice,
                RunUpgradeChoiceError.None);
        }

        internal static RunLevelUpgradeSelectionResult OfferedChoice(
            RunUpgradeChoiceState choice)
        {
            return new RunLevelUpgradeSelectionResult(
                true,
                true,
                RunLevelUpgradeSelectionError.None,
                choice,
                RunUpgradeChoiceError.None);
        }

        internal static RunLevelUpgradeSelectionResult Failed(
            RunLevelUpgradeSelectionError error,
            RunUpgradeChoiceError choiceError =
                RunUpgradeChoiceError.None,
            RunUpgradeChoiceState choice = null)
        {
            return new RunLevelUpgradeSelectionResult(
                false,
                false,
                error,
                choice,
                choiceError);
        }
    }

    public sealed class RunLevelUpgradeSelectionService : IDisposable
    {
        public const string ChoiceIdPrefix = "choice:run-level-upgrade";

        private readonly RunProgressionService progression;
        private readonly RunUpgradeChoiceService choices;
        private readonly Dictionary<string, RunUpgradeUnlockParticipantPlan>
            plans = new(StringComparer.Ordinal);
        private readonly int runSeed;
        private readonly bool observeChanges;
        private bool disposed;

        public RunLevelUpgradeSelectionService(
            RunProgressionService progression,
            RunUpgradeChoiceService choices,
            IReadOnlyList<RunUpgradeUnlockParticipantPlan> participantPlans,
            int runSeed,
            bool observeChanges = true)
        {
            this.progression = progression ??
                throw new ArgumentNullException(nameof(progression));
            this.choices = choices ??
                throw new ArgumentNullException(nameof(choices));
            if (participantPlans == null || participantPlans.Count == 0)
            {
                throw new ArgumentException(
                    "At least one participant upgrade plan is required.",
                    nameof(participantPlans));
            }

            for (int i = 0; i < participantPlans.Count; i++)
            {
                RunUpgradeUnlockParticipantPlan plan = participantPlans[i];
                if (plan == null || !plan.IsValid)
                {
                    throw new ArgumentException(
                        $"Participant upgrade plan {i} is invalid.",
                        nameof(participantPlans));
                }

                if (!progression.TryGetParticipant(plan.PlayerId, out _) ||
                    !choices.TryGetParticipant(plan.PlayerId, out _))
                {
                    throw new ArgumentException(
                        $"Participant '{plan.PlayerId}' is not registered in progression and upgrades.",
                        nameof(participantPlans));
                }

                if (!plans.TryAdd(plan.PlayerId, plan))
                {
                    throw new ArgumentException(
                        $"Participant '{plan.PlayerId}' has more than one upgrade plan.",
                        nameof(participantPlans));
                }
            }

            this.runSeed = runSeed;
            this.observeChanges = observeChanges;
            if (observeChanges)
            {
                progression.StateChanged += HandleProgressionChanged;
                choices.ChoiceResolved += HandleChoiceResolved;
            }
        }

        public event Action<string, RunLevelUpgradeSelectionResult>
            OfferFailed;

        public RunLevelUpgradeSelectionResult TryOfferNext(string playerId)
        {
            if (disposed)
            {
                return RunLevelUpgradeSelectionResult.Failed(
                    RunLevelUpgradeSelectionError.ServiceDisposed);
            }

            string normalizedPlayerId = playerId?.Trim() ?? string.Empty;
            if (normalizedPlayerId.Length == 0)
            {
                return RunLevelUpgradeSelectionResult.Failed(
                    RunLevelUpgradeSelectionError.InvalidPlayerId);
            }

            if (!plans.TryGetValue(
                    normalizedPlayerId,
                    out RunUpgradeUnlockParticipantPlan plan))
            {
                return RunLevelUpgradeSelectionResult.Failed(
                    RunLevelUpgradeSelectionError.ParticipantNotConfigured);
            }

            if (!progression.TryGetParticipant(normalizedPlayerId, out _))
            {
                return RunLevelUpgradeSelectionResult.Failed(
                    RunLevelUpgradeSelectionError
                        .ProgressionParticipantNotFound);
            }

            if (choices.TryGetPendingChoice(
                    normalizedPlayerId,
                    out RunUpgradeChoiceState pending))
            {
                return RunLevelUpgradeSelectionResult.NoChange(pending);
            }

            if (!TryGetNextEligibleMilestone(
                    normalizedPlayerId,
                    out RunUpgradeUnlockMilestone milestone))
            {
                return RunLevelUpgradeSelectionResult.NoChange();
            }

            RunUpgradeChoiceResult offered = choices.TryOfferChoice(
                new RunUpgradeChoiceRequest(
                    normalizedPlayerId,
                    CreateChoiceId(plan.Schedule.ScheduleId, milestone),
                    milestone.CandidateUpgradeIds,
                    milestone.OptionCount,
                    CreateChoiceSeed(
                        runSeed,
                        plan.ParticipantIndex,
                        milestone)));
            return offered.Success
                ? RunLevelUpgradeSelectionResult.OfferedChoice(
                    offered.Choice)
                : RunLevelUpgradeSelectionResult.Failed(
                    RunLevelUpgradeSelectionError.ChoiceRejected,
                    offered.Error,
                    offered.Choice);
        }

        public bool TryGetNextEligibleMilestone(
            string playerId,
            out RunUpgradeUnlockMilestone milestone)
        {
            milestone = null;
            if (disposed)
                return false;

            string normalizedPlayerId = playerId?.Trim() ?? string.Empty;
            if (!plans.TryGetValue(
                    normalizedPlayerId,
                    out RunUpgradeUnlockParticipantPlan plan) ||
                !progression.TryGetParticipant(
                    normalizedPlayerId,
                    out RunParticipantProgressionState state))
            {
                return false;
            }

            for (int i = 0; i < plan.Schedule.Milestones.Count; i++)
            {
                RunUpgradeUnlockMilestone candidate =
                    plan.Schedule.Milestones[i];
                if (candidate.UnlockLevel > state.Level)
                    break;

                string choiceId = CreateChoiceId(
                    plan.Schedule.ScheduleId,
                    candidate);
                if (choices.HasResolvedChoice(
                        normalizedPlayerId,
                        choiceId))
                {
                    continue;
                }

                milestone = candidate;
                return true;
            }

            return false;
        }

        public static string CreateChoiceId(
            string scheduleId,
            RunUpgradeUnlockMilestone milestone)
        {
            if (milestone == null)
                return string.Empty;

            string normalizedScheduleId = scheduleId?.Trim() ?? string.Empty;
            return normalizedScheduleId.Length == 0
                ? string.Empty
                : $"{ChoiceIdPrefix}:{normalizedScheduleId}:" +
                  $"level:{milestone.UnlockLevel}";
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
            if (observeChanges)
            {
                progression.StateChanged -= HandleProgressionChanged;
                choices.ChoiceResolved -= HandleChoiceResolved;
            }
        }

        private void HandleProgressionChanged(
            RunParticipantProgressionState state)
        {
            if (state == null || !plans.ContainsKey(state.PlayerId))
                return;

            ReportFailure(state.PlayerId, TryOfferNext(state.PlayerId));
        }

        private void HandleChoiceResolved(
            RunUpgradeChoiceState choice,
            string selectedUpgradeId,
            RunParticipantUpgradeState participant)
        {
            if (choice == null || !plans.ContainsKey(choice.PlayerId))
                return;

            ReportFailure(choice.PlayerId, TryOfferNext(choice.PlayerId));
        }

        private void ReportFailure(
            string playerId,
            RunLevelUpgradeSelectionResult result)
        {
            if (!result.Success)
                OfferFailed?.Invoke(playerId, result);
        }

        private static int CreateChoiceSeed(
            int baseSeed,
            int participantIndex,
            RunUpgradeUnlockMilestone milestone)
        {
            unchecked
            {
                int seed = baseSeed;
                seed = (seed * 397) ^ participantIndex;
                seed = (seed * 397) ^ milestone.UnlockLevel;
                return seed;
            }
        }
    }
}
