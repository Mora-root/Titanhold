using Titanhold.Combat.Abilities;
using Titanhold.Run;
using Titanhold.UI.Common;

namespace Titanhold.UI.Run
{
    public enum RunLevelRewardPresentationError
    {
        None,
        PresenterNotFound,
        MissingChoice,
        UnsupportedChoice,
        InvalidTargetSlot,
        InvalidOptionCount,
        DefinitionNotFound,
        PresentationNotFound
    }

    public sealed class RunLevelRewardSelectionPresenter
    {
        private const int MaximumOptionCount = 3;

        private readonly IAbilityDefinitionResolver abilityDefinitions;
        private readonly IRunUpgradeDefinitionResolver upgradeDefinitions;

        public RunLevelRewardSelectionPresenter(
            IAbilityDefinitionResolver abilityDefinitions,
            IRunUpgradeDefinitionResolver upgradeDefinitions)
        {
            this.abilityDefinitions = abilityDefinitions;
            this.upgradeDefinitions = upgradeDefinitions;
        }

        public bool TryBuild(
            RunAbilityChoiceState choice,
            out ChoiceSelectionModel model,
            out RunLevelRewardPresentationError error)
        {
            model = null;
            error = RunLevelRewardPresentationError.None;
            if (choice == null)
            {
                error = RunLevelRewardPresentationError.MissingChoice;
                return false;
            }

            if (!RunLevelAbilitySelectionService.IsRunLevelChoiceId(
                    choice.ChoiceId))
            {
                error = RunLevelRewardPresentationError.UnsupportedChoice;
                return false;
            }

            if (choice.TargetSlotIndex < 1)
            {
                error = RunLevelRewardPresentationError.InvalidTargetSlot;
                return false;
            }

            if (abilityDefinitions == null ||
                !HasValidOptionCount(choice.OfferedAbilityIds.Count))
            {
                error = abilityDefinitions == null
                    ? RunLevelRewardPresentationError.DefinitionNotFound
                    : RunLevelRewardPresentationError.InvalidOptionCount;
                return false;
            }

            ChoiceSelectionOption[] options =
                new ChoiceSelectionOption[choice.OfferedAbilityIds.Count];
            for (int i = 0; i < options.Length; i++)
            {
                string abilityId = choice.OfferedAbilityIds[i];
                if (!abilityDefinitions.TryResolve(
                        abilityId,
                        out IAbilityDefinition definition))
                {
                    error = RunLevelRewardPresentationError.DefinitionNotFound;
                    return false;
                }

                if (definition is not IAbilityPresentationDefinition presentation)
                {
                    error = RunLevelRewardPresentationError.PresentationNotFound;
                    return false;
                }

                string displayName = presentation.DisplayName?.Trim() ??
                    string.Empty;
                options[i] = new ChoiceSelectionOption(
                    abilityId,
                    displayName.Length > 0 ? displayName : abilityId,
                    presentation.Description,
                    presentation.Icon);
            }

            model = new ChoiceSelectionModel(
                choice.PlayerId,
                choice.ChoiceId,
                options);
            return true;
        }

        public bool TryBuild(
            RunUpgradeChoiceState choice,
            out ChoiceSelectionModel model,
            out RunLevelRewardPresentationError error)
        {
            model = null;
            error = RunLevelRewardPresentationError.None;
            if (choice == null)
            {
                error = RunLevelRewardPresentationError.MissingChoice;
                return false;
            }

            if (!RunLevelUpgradeSelectionService.IsRunLevelChoiceId(
                    choice.ChoiceId))
            {
                error = RunLevelRewardPresentationError.UnsupportedChoice;
                return false;
            }

            if (upgradeDefinitions == null ||
                !HasValidOptionCount(choice.OfferedUpgradeIds.Count))
            {
                error = upgradeDefinitions == null
                    ? RunLevelRewardPresentationError.DefinitionNotFound
                    : RunLevelRewardPresentationError.InvalidOptionCount;
                return false;
            }

            ChoiceSelectionOption[] options =
                new ChoiceSelectionOption[choice.OfferedUpgradeIds.Count];
            for (int i = 0; i < options.Length; i++)
            {
                string upgradeId = choice.OfferedUpgradeIds[i];
                if (!upgradeDefinitions.TryResolve(
                        upgradeId,
                        out IRunUpgradeDefinition definition))
                {
                    error = RunLevelRewardPresentationError.DefinitionNotFound;
                    return false;
                }

                if (definition is not IRunUpgradePresentationDefinition
                    presentation)
                {
                    error = RunLevelRewardPresentationError.PresentationNotFound;
                    return false;
                }

                string displayName = presentation.DisplayName?.Trim() ??
                    string.Empty;
                options[i] = new ChoiceSelectionOption(
                    upgradeId,
                    displayName.Length > 0 ? displayName : upgradeId,
                    presentation.Description,
                    presentation.Icon);
            }

            model = new ChoiceSelectionModel(
                choice.PlayerId,
                choice.ChoiceId,
                options);
            return true;
        }

        private static bool HasValidOptionCount(int count)
        {
            return count > 0 && count <= MaximumOptionCount;
        }
    }
}
