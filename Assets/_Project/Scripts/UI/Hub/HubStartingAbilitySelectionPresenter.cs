using System;
using Titanhold.Combat.Abilities;
using Titanhold.Run;

namespace Titanhold.UI.Hub
{
    public sealed class HubStartingAbilitySelectionPresenter
    {
        private readonly IAbilityDefinitionResolver definitions;

        public HubStartingAbilitySelectionPresenter(
            IAbilityDefinitionResolver definitions)
        {
            this.definitions = definitions ??
                throw new ArgumentNullException(nameof(definitions));
        }

        public bool TryBuild(
            RunAbilityChoiceState choice,
            out HubStartingAbilitySelectionModel model,
            out HubStartingAbilityPresentationError error)
        {
            model = null;
            error = HubStartingAbilityPresentationError.None;
            if (choice == null)
            {
                error = HubStartingAbilityPresentationError.MissingChoice;
                return false;
            }

            if (!string.Equals(
                    choice.ChoiceId,
                    RunStartingAbilitySelectionService.StartingChoiceId,
                    StringComparison.Ordinal))
            {
                error = HubStartingAbilityPresentationError.NotStartingChoice;
                return false;
            }

            if (choice.TargetSlotIndex !=
                RunStartReadinessService.StartingAbilitySlotIndex)
            {
                error = HubStartingAbilityPresentationError.WrongTargetSlot;
                return false;
            }

            if (choice.OfferedAbilityIds.Count !=
                RunStartingAbilitySelectionService.StartingAbilityOptionCount)
            {
                error = HubStartingAbilityPresentationError.InvalidOptionCount;
                return false;
            }

            HubStartingAbilityOption[] options =
                new HubStartingAbilityOption[choice.OfferedAbilityIds.Count];
            for (int i = 0; i < options.Length; i++)
            {
                string abilityId = choice.OfferedAbilityIds[i];
                if (!definitions.TryResolve(
                        abilityId,
                        out IAbilityDefinition definition))
                {
                    error = HubStartingAbilityPresentationError.AbilityNotFound;
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
