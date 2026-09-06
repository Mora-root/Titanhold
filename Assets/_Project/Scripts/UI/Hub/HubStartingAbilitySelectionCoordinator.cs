using System;
using Titanhold.Run;

namespace Titanhold.UI.Hub
{
    public sealed class HubStartingAbilitySelectionCoordinator
    {
        private readonly RunAbilityChoiceService choices;
        private readonly RunStartingAbilitySelectionService selection;
        private readonly HubStartingAbilitySelectionPresenter presenter;

        public HubStartingAbilitySelectionCoordinator(
            RunAbilityChoiceService choices,
            RunStartingAbilitySelectionService selection,
            HubStartingAbilitySelectionPresenter presenter)
        {
            this.choices = choices ??
                throw new ArgumentNullException(nameof(choices));
            this.selection = selection ??
                throw new ArgumentNullException(nameof(selection));
            this.presenter = presenter ??
                throw new ArgumentNullException(nameof(presenter));
        }

        public HubStartingAbilitySelectionModel ActiveSelection { get; private set; }

        public HubStartingAbilityCoordinationResult TryPresent(string playerId)
        {
            ActiveSelection = null;
            string normalizedPlayerId = playerId?.Trim() ?? string.Empty;
            if (normalizedPlayerId.Length == 0)
            {
                return HubStartingAbilityCoordinationResult.Failed(
                    HubStartingAbilityCoordinationError.InvalidPlayerId);
            }

            if (!choices.TryGetPendingChoice(
                    normalizedPlayerId,
                    out RunAbilityChoiceState pendingChoice))
            {
                return HubStartingAbilityCoordinationResult.Failed(
                    HubStartingAbilityCoordinationError.PendingChoiceNotFound);
            }

            if (!presenter.TryBuild(
                    pendingChoice,
                    out HubStartingAbilitySelectionModel model,
                    out HubStartingAbilityPresentationError presentationError))
            {
                return HubStartingAbilityCoordinationResult.Failed(
                    HubStartingAbilityCoordinationError.PresentationRejected,
                    presentationError: presentationError);
            }

            ActiveSelection = model;
            return HubStartingAbilityCoordinationResult.Succeeded(model);
        }

        public HubStartingAbilityCoordinationResult TrySelect(string abilityId)
        {
            if (ActiveSelection == null)
            {
                return HubStartingAbilityCoordinationResult.Failed(
                    HubStartingAbilityCoordinationError.NoActiveSelection);
            }

            string normalizedAbilityId = abilityId?.Trim() ?? string.Empty;
            if (normalizedAbilityId.Length == 0)
            {
                return HubStartingAbilityCoordinationResult.Failed(
                    HubStartingAbilityCoordinationError.InvalidAbilityId,
                    ActiveSelection);
            }

            if (!ContainsAbility(ActiveSelection, normalizedAbilityId))
            {
                return HubStartingAbilityCoordinationResult.Failed(
                    HubStartingAbilityCoordinationError.AbilityNotOffered,
                    ActiveSelection);
            }

            HubStartingAbilitySelectionModel submittedModel = ActiveSelection;
            RunStartingAbilitySelectionResult result =
                selection.TrySelectStartingAbility(
                    submittedModel.PlayerId,
                    submittedModel.ChoiceId,
                    normalizedAbilityId);
            if (!result.Success)
            {
                return HubStartingAbilityCoordinationResult.Failed(
                    HubStartingAbilityCoordinationError.SelectionRejected,
                    submittedModel,
                    selectionResult: result);
            }

            ActiveSelection = null;
            return HubStartingAbilityCoordinationResult.Succeeded(
                submittedModel,
                result);
        }

        public void Clear()
        {
            ActiveSelection = null;
        }

        private static bool ContainsAbility(
            HubStartingAbilitySelectionModel model,
            string abilityId)
        {
            for (int i = 0; i < model.Options.Count; i++)
            {
                if (string.Equals(
                        model.Options[i].AbilityId,
                        abilityId,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
