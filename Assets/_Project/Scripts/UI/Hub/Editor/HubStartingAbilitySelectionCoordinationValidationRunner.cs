using System;
using Titanhold.Combat.Abilities;
using Titanhold.Run;
using UnityEditor;
using UnityEngine;

namespace Titanhold.UI.Hub.Editor
{
    public static class
        HubStartingAbilitySelectionCoordinationValidationRunner
    {
        [MenuItem("Tools/Titanhold/Validate Starting Ability Coordination")]
        public static void Validate()
        {
            try
            {
                ValidateSelectionFlow();
                ValidatePresentationBoundary();
                Debug.Log("Starting Ability Coordination validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Starting Ability Coordination validation failed: {exception}");
            }
        }

        private static void ValidateSelectionFlow()
        {
            CreateServices(
                out RunAbilityLoadoutService loadout,
                out RunStartReadinessService readiness,
                out RunAbilityChoiceService choices,
                out RunStartingAbilitySelectionService selection);
            using (readiness)
            {
                HubStartingAbilitySelectionCoordinator coordinator = new(
                    choices,
                    selection,
                    new HubStartingAbilitySelectionPresenter(
                        CreateDefinitions()));
                Assert(coordinator.TryPresent("player:local").Error ==
                       HubStartingAbilityCoordinationError.PendingChoiceNotFound,
                    "A missing pending choice was presented.");

                RunStartingAbilitySelectionResult offer =
                    selection.TryOfferStartingChoice(
                        new RunStartingAbilityChoiceRequest(
                            "player:local",
                            RunStartingAbilitySelectionService.StartingChoiceId,
                            new[]
                            {
                                "ability:strike",
                                "ability:slash",
                                "ability:guard"
                            },
                            27));
                Assert(offer.Success,
                    "Could not prepare a starting ability choice.");

                HubStartingAbilityCoordinationResult presented =
                    coordinator.TryPresent(" player:local ");
                Assert(presented.Success &&
                       coordinator.ActiveSelection == presented.Model &&
                       presented.Model.Options.Count == 3,
                    "The pending choice was not retained for UI interaction.");
                Assert(coordinator.TrySelect(" ").Error ==
                       HubStartingAbilityCoordinationError.InvalidAbilityId &&
                       coordinator.TrySelect("ability:unknown").Error ==
                       HubStartingAbilityCoordinationError.AbilityNotOffered &&
                       readiness.ConfirmedParticipantCount == 0 &&
                       choices.PendingChoiceCount == 1,
                    "Invalid UI selection mutated the pending choice.");

                string selectedAbilityId =
                    presented.Model.Options[1].AbilityId;
                HubStartingAbilityCoordinationResult selected =
                    coordinator.TrySelect($" {selectedAbilityId} ");
                Assert(selected.Success &&
                       selected.SelectionResult.Success &&
                       selected.SelectionResult.SelectedAbilityId ==
                           selectedAbilityId &&
                       selected.SelectionResult.RosterSealed &&
                       coordinator.ActiveSelection == null &&
                       choices.PendingChoiceCount == 0 &&
                       readiness.IsSealed &&
                       loadout.TryGetParticipant(
                           "player:local",
                           out RunParticipantAbilityState participant) &&
                       participant.TryGetAbilitySlot(
                           RunStartReadinessService.StartingAbilitySlotIndex,
                           out string assignedAbilityId) &&
                       assignedAbilityId == selectedAbilityId,
                    "The selected UI option was not committed and sealed.");
                Assert(coordinator.TrySelect(selectedAbilityId).Error ==
                       HubStartingAbilityCoordinationError.NoActiveSelection,
                    "A resolved UI choice was submitted twice.");
            }
        }

        private static void ValidatePresentationBoundary()
        {
            CreateServices(
                out _,
                out RunStartReadinessService readiness,
                out RunAbilityChoiceService choices,
                out RunStartingAbilitySelectionService selection);
            using (readiness)
            {
                Assert(selection.TryOfferStartingChoice(
                           new RunStartingAbilityChoiceRequest(
                               "player:local",
                               "choice:not-starting",
                               new[]
                               {
                                   "ability:strike",
                                   "ability:slash",
                                   "ability:guard"
                               },
                               31)).Success,
                    "Could not prepare an invalid presentation choice.");
                HubStartingAbilitySelectionCoordinator coordinator = new(
                    choices,
                    selection,
                    new HubStartingAbilitySelectionPresenter(
                        CreateDefinitions()));
                HubStartingAbilityCoordinationResult rejected =
                    coordinator.TryPresent("player:local");
                Assert(!rejected.Success &&
                       rejected.Error ==
                           HubStartingAbilityCoordinationError
                               .PresentationRejected &&
                       rejected.PresentationError ==
                           HubStartingAbilityPresentationError
                               .NotStartingChoice &&
                       coordinator.ActiveSelection == null &&
                       choices.PendingChoiceCount == 1,
                    "Coordinator bypassed the starting-choice presentation contract.");
            }
        }

        private static void CreateServices(
            out RunAbilityLoadoutService loadout,
            out RunStartReadinessService readiness,
            out RunAbilityChoiceService choices,
            out RunStartingAbilitySelectionService selection)
        {
            loadout = new RunAbilityLoadoutService();
            RunParticipantIdentity participant = new(
                "player:local",
                "character:warrior");
            Assert(loadout.TryRegisterParticipant(participant).Success,
                "Could not prepare the participant loadout.");
            readiness = new RunStartReadinessService(
                loadout,
                new[] { participant });
            choices = new RunAbilityChoiceService(loadout);
            selection = new RunStartingAbilitySelectionService(
                choices,
                readiness);
        }

        private static AbilityDefinitionRegistry CreateDefinitions()
        {
            Assert(AbilityDefinitionRegistry.TryCreate(
                       new IAbilityDefinition[]
                       {
                           new TestAbility("ability:strike"),
                           new TestAbility("ability:slash"),
                           new TestAbility("ability:guard")
                       },
                       out AbilityDefinitionRegistry definitions,
                       out string error),
                $"Could not prepare ability definitions: {error}");
            return definitions;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private sealed class TestAbility : IAbilityDefinition
        {
            public TestAbility(string abilityId)
            {
                AbilityId = abilityId;
            }

            public string AbilityId { get; }
        }
    }
}
