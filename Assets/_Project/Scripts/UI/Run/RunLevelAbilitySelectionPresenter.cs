using System;
using Titanhold.Combat.Abilities;
using Titanhold.Run;
using Titanhold.UI.Hub;

namespace Titanhold.UI.Run
{
    public enum RunLevelAbilityPresentationError
    {
        None,
        MissingChoice,
        NotRunLevelChoice,
        InvalidTargetSlot,
        InvalidOptionCount,
        AbilityNotFound
    }

    public sealed class RunLevelAbilitySelectionPresenter
    {
        private const int MaximumOptionCount = 3;
        private readonly IAbilityDefinitionResolver definitions;

        public RunLevelAbilitySelectionPresenter(
            IAbilityDefinitionResolver definitions)
        {
            this.definitions = definitions ??
                throw new ArgumentNullException(nameof(definitions));
        }

        public bool TryBuild(
            RunAbilityChoiceState choice,
            out HubStartingAbilitySelectionModel model,
            out RunLevelAbilityPresentationError error)
        {
            model = null;
            error = RunLevelAbilityPresentationError.None;
            if (choice == null)
            {
                error = RunLevelAbilityPresentationError.MissingChoice;
                return false;
            }

            if (!RunLevelAbilitySelectionService.IsRunLevelChoiceId(
                    choice.ChoiceId))
            {
                error = RunLevelAbilityPresentationError.NotRunLevelChoice;
                return false;
            }

            if (choice.TargetSlotIndex < 1)
            {
                error = RunLevelAbilityPresentationError.InvalidTargetSlot;
                return false;
            }

            if (choice.OfferedAbilityIds.Count <= 0 ||
                choice.OfferedAbilityIds.Count > MaximumOptionCount)
            {
                error = RunLevelAbilityPresentationError.InvalidOptionCount;
                return false;
            }

            HubStartingAbilityOption[] options =
                new HubStartingAbilityOption[
                    choice.OfferedAbilityIds.Count];
            for (int i = 0; i < options.Length; i++)
            {
                string abilityId = choice.OfferedAbilityIds[i];
                if (!definitions.TryResolve(
                        abilityId,
                        out IAbilityDefinition definition))
                {
                    error = RunLevelAbilityPresentationError.AbilityNotFound;
                    return false;
                }

                IAbilityPresentationDefinition presentation =
                    definition as IAbilityPresentationDefinition;
                string displayName = presentation?.DisplayName?.Trim() ??
                    string.Empty;
                if (displayName.Length == 0)
                    displayName = abilityId;

                options[i] = new HubStartingAbilityOption(
                    abilityId,
                    displayName,
                    presentation?.Description?.Trim() ?? string.Empty,
                    presentation?.Icon);
            }

            model = new HubStartingAbilitySelectionModel(
                choice.PlayerId,
                choice.ChoiceId,
                options);
            return true;
        }
    }
}
