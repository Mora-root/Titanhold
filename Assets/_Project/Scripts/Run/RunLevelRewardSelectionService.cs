using System;
using System.Collections.Generic;

namespace Titanhold.Run
{
    public enum RunLevelRewardKind
    {
        None,
        Ability,
        Upgrade
    }

    public enum RunLevelRewardSelectionError
    {
        None,
        ServiceDisposed,
        InvalidPlayerId,
        ParticipantNotConfigured,
        ConflictingMilestones,
        AbilityChoiceRejected,
        UpgradeChoiceRejected
    }

    public readonly struct RunLevelRewardSelectionResult
    {
        private RunLevelRewardSelectionResult(
            bool success,
            bool offered,
            RunLevelRewardKind kind,
            RunLevelRewardSelectionError error)
        {
            Success = success;
            Offered = offered;
            Kind = kind;
            Error = error;
        }

        public bool Success { get; }
        public bool Offered { get; }
        public RunLevelRewardKind Kind { get; }
        public RunLevelRewardSelectionError Error { get; }

        internal static RunLevelRewardSelectionResult NoChange(
            RunLevelRewardKind kind = RunLevelRewardKind.None)
        {
            return new RunLevelRewardSelectionResult(
                true,
                false,
                kind,
                RunLevelRewardSelectionError.None);
        }

        internal static RunLevelRewardSelectionResult OfferedChoice(
            RunLevelRewardKind kind)
        {
            return new RunLevelRewardSelectionResult(
                true,
                true,
                kind,
                RunLevelRewardSelectionError.None);
        }

        internal static RunLevelRewardSelectionResult Failed(
            RunLevelRewardSelectionError error)
        {
            return new RunLevelRewardSelectionResult(
                false,
                false,
                RunLevelRewardKind.None,
                error);
        }
    }

    public sealed class RunLevelRewardSelectionService : IDisposable
    {
        private readonly RunProgressionService progression;
        private readonly RunAbilityChoiceService abilityChoices;
        private readonly RunUpgradeChoiceService upgradeChoices;
        private readonly RunLevelAbilitySelectionService abilitySelection;
        private readonly RunLevelUpgradeSelectionService upgradeSelection;
        private readonly HashSet<string> playerIds =
            new(StringComparer.Ordinal);
        private bool disposed;

        public RunLevelRewardSelectionService(
            RunProgressionService progression,
            RunAbilityChoiceService abilityChoices,
            RunUpgradeChoiceService upgradeChoices,
            RunLevelAbilitySelectionService abilitySelection,
            RunLevelUpgradeSelectionService upgradeSelection,
            IReadOnlyList<string> configuredPlayerIds)
        {
            this.progression = progression ??
                throw new ArgumentNullException(nameof(progression));
            this.abilityChoices = abilityChoices ??
                throw new ArgumentNullException(nameof(abilityChoices));
            this.upgradeChoices = upgradeChoices ??
                throw new ArgumentNullException(nameof(upgradeChoices));
            this.abilitySelection = abilitySelection ??
                throw new ArgumentNullException(nameof(abilitySelection));
            this.upgradeSelection = upgradeSelection ??
                throw new ArgumentNullException(nameof(upgradeSelection));
            if (configuredPlayerIds == null ||
                configuredPlayerIds.Count == 0)
            {
                throw new ArgumentException(
                    "At least one participant is required.",
                    nameof(configuredPlayerIds));
            }

            for (int i = 0; i < configuredPlayerIds.Count; i++)
            {
                string playerId = configuredPlayerIds[i]?.Trim() ??
                    string.Empty;
                if (playerId.Length == 0 ||
                    !progression.TryGetParticipant(playerId, out _) ||
                    !upgradeChoices.TryGetParticipant(playerId, out _) ||
                    !playerIds.Add(playerId))
                {
                    throw new ArgumentException(
                        $"Configured participant {i} is invalid or duplicated.",
                        nameof(configuredPlayerIds));
                }
            }

            progression.StateChanged += HandleProgressionChanged;
            abilityChoices.ChoiceResolved += HandleAbilityChoiceResolved;
            upgradeChoices.ChoiceResolved += HandleUpgradeChoiceResolved;
        }

        public event Action<string, RunLevelRewardSelectionResult>
            OfferFailed;

        public RunLevelRewardSelectionResult TryOfferNext(string playerId)
        {
            if (disposed)
            {
                return RunLevelRewardSelectionResult.Failed(
                    RunLevelRewardSelectionError.ServiceDisposed);
            }

            string normalizedPlayerId = playerId?.Trim() ?? string.Empty;
            if (normalizedPlayerId.Length == 0)
            {
                return RunLevelRewardSelectionResult.Failed(
                    RunLevelRewardSelectionError.InvalidPlayerId);
            }

            if (!playerIds.Contains(normalizedPlayerId))
            {
                return RunLevelRewardSelectionResult.Failed(
                    RunLevelRewardSelectionError.ParticipantNotConfigured);
            }

            if (abilityChoices.TryGetPendingChoice(
                    normalizedPlayerId,
                    out RunAbilityChoiceState pendingAbility))
            {
                return RunLevelRewardSelectionResult.NoChange(
                    RunLevelAbilitySelectionService.IsRunLevelChoiceId(
                        pendingAbility.ChoiceId)
                        ? RunLevelRewardKind.Ability
                        : RunLevelRewardKind.None);
            }

            if (upgradeChoices.TryGetPendingChoice(
                    normalizedPlayerId,
                    out _))
            {
                return RunLevelRewardSelectionResult.NoChange(
                    RunLevelRewardKind.Upgrade);
            }

            bool hasAbility =
                abilitySelection.TryGetNextEligibleMilestone(
                    normalizedPlayerId,
                    out RunAbilityUnlockMilestone abilityMilestone);
            bool hasUpgrade =
                upgradeSelection.TryGetNextEligibleMilestone(
                    normalizedPlayerId,
                    out RunUpgradeUnlockMilestone upgradeMilestone);
            if (!hasAbility && !hasUpgrade)
                return RunLevelRewardSelectionResult.NoChange();

            if (hasAbility && hasUpgrade &&
                abilityMilestone.UnlockLevel == upgradeMilestone.UnlockLevel)
            {
                return RunLevelRewardSelectionResult.Failed(
                    RunLevelRewardSelectionError.ConflictingMilestones);
            }

            if (hasAbility &&
                (!hasUpgrade || abilityMilestone.UnlockLevel <
                    upgradeMilestone.UnlockLevel))
            {
                RunLevelAbilitySelectionResult result =
                    abilitySelection.TryOfferNext(normalizedPlayerId);
                return result.Success
                    ? result.Offered
                        ? RunLevelRewardSelectionResult.OfferedChoice(
                            RunLevelRewardKind.Ability)
                        : RunLevelRewardSelectionResult.NoChange(
                            RunLevelRewardKind.Ability)
                    : RunLevelRewardSelectionResult.Failed(
                        RunLevelRewardSelectionError
                            .AbilityChoiceRejected);
            }

            RunLevelUpgradeSelectionResult upgradeResult =
                upgradeSelection.TryOfferNext(normalizedPlayerId);
            return upgradeResult.Success
                ? upgradeResult.Offered
                    ? RunLevelRewardSelectionResult.OfferedChoice(
                        RunLevelRewardKind.Upgrade)
                    : RunLevelRewardSelectionResult.NoChange(
                        RunLevelRewardKind.Upgrade)
                : RunLevelRewardSelectionResult.Failed(
                    RunLevelRewardSelectionError.UpgradeChoiceRejected);
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            progression.StateChanged -= HandleProgressionChanged;
            abilityChoices.ChoiceResolved -= HandleAbilityChoiceResolved;
            upgradeChoices.ChoiceResolved -= HandleUpgradeChoiceResolved;
        }

        private void HandleProgressionChanged(
            RunParticipantProgressionState state)
        {
            if (state == null || !playerIds.Contains(state.PlayerId))
                return;

            ReportFailure(state.PlayerId, TryOfferNext(state.PlayerId));
        }

        private void HandleAbilityChoiceResolved(
            RunAbilityChoiceState choice,
            string selectedAbilityId)
        {
            if (choice == null || !playerIds.Contains(choice.PlayerId))
                return;

            ReportFailure(choice.PlayerId, TryOfferNext(choice.PlayerId));
        }

        private void HandleUpgradeChoiceResolved(
            RunUpgradeChoiceState choice,
            string selectedUpgradeId,
            RunParticipantUpgradeState participant)
        {
            if (choice == null || !playerIds.Contains(choice.PlayerId))
                return;

            ReportFailure(choice.PlayerId, TryOfferNext(choice.PlayerId));
        }

        private void ReportFailure(
            string playerId,
            RunLevelRewardSelectionResult result)
        {
            if (!result.Success)
                OfferFailed?.Invoke(playerId, result);
        }
    }
}
