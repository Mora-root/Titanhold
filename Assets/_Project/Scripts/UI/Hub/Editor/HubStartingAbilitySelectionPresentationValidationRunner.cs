using System;
using Titanhold.Combat.Abilities;
using Titanhold.Run;
using UnityEditor;
using UnityEngine;

namespace Titanhold.UI.Hub.Editor
{
    public static class
        HubStartingAbilitySelectionPresentationValidationRunner
    {
        [MenuItem("Tools/Titanhold/Validate Starting Ability Presentation")]
        public static void Validate()
        {
            try
            {
                AbilityDefinitionRegistry definitions = CreateDefinitions();
                HubStartingAbilitySelectionPresenter presenter = new(
                    definitions);

                Assert(presenter.TryBuild(
                           CreateChoice(
                               RunStartingAbilitySelectionService
                                   .StartingChoiceId,
                               RunStartReadinessService
                                   .StartingAbilitySlotIndex,
                               3,
                               "ability:strike",
                               "ability:slash",
                               "ability:guard"),
                           out HubStartingAbilitySelectionModel model,
                           out HubStartingAbilityPresentationError error) &&
                       error == HubStartingAbilityPresentationError.None &&
                       model.PlayerId == "player:local" &&
                       model.ChoiceId ==
                           RunStartingAbilitySelectionService.StartingChoiceId &&
                       model.Options.Count == 3,
                    "A valid pending starting choice was not presented.");

                HubStartingAbilityOption strike = FindOption(
                    model,
                    "ability:strike");
                HubStartingAbilityOption slash = FindOption(
                    model,
                    "ability:slash");
                Assert(strike != null &&
                       strike.DisplayName == "Heavy Strike" &&
                       strike.Description == "Generate rage." &&
                       strike.Icon == null,
                    "Ability presentation metadata was not normalized.");
                Assert(slash != null &&
                       slash.DisplayName == "ability:slash" &&
                       slash.Description.Length == 0 &&
                       slash.Icon == null,
                    "An ability without presentation metadata did not use its stable-id fallback.");

                Assert(!presenter.TryBuild(
                           null,
                           out _,
                           out error) &&
                       error ==
                           HubStartingAbilityPresentationError.MissingChoice,
                    "A missing pending choice was accepted.");
                Assert(!presenter.TryBuild(
                           CreateChoice(
                               "choice:later",
                               RunStartReadinessService
                                   .StartingAbilitySlotIndex,
                               3,
                               "ability:strike",
                               "ability:slash",
                               "ability:guard"),
                           out _,
                           out error) &&
                       error ==
                           HubStartingAbilityPresentationError.NotStartingChoice,
                    "A non-starting choice was accepted.");
                Assert(!presenter.TryBuild(
                           CreateChoice(
                               RunStartingAbilitySelectionService
                                   .StartingChoiceId,
                               1,
                               3,
                               "ability:strike",
                               "ability:slash",
                               "ability:guard"),
                           out _,
                           out error) &&
                       error ==
                           HubStartingAbilityPresentationError.WrongTargetSlot,
                    "A choice targeting a later slot was accepted.");
                Assert(!presenter.TryBuild(
                           CreateChoice(
                               RunStartingAbilitySelectionService
                                   .StartingChoiceId,
                               RunStartReadinessService
                                   .StartingAbilitySlotIndex,
                               2,
                               "ability:strike",
                               "ability:slash"),
                           out _,
                           out error) &&
                       error ==
                           HubStartingAbilityPresentationError.InvalidOptionCount,
                    "A starting choice without exactly three options was accepted.");
                Assert(!presenter.TryBuild(
                           CreateChoice(
                               RunStartingAbilitySelectionService
                                   .StartingChoiceId,
                               RunStartReadinessService
                                   .StartingAbilitySlotIndex,
                               3,
                               "ability:unknown-one",
                               "ability:unknown-two",
                               "ability:unknown-three"),
                           out _,
                           out error) &&
                       error ==
                           HubStartingAbilityPresentationError.AbilityNotFound,
                    "An unresolved offered ability was accepted.");

                Debug.Log("Starting Ability Presentation validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Starting Ability Presentation validation failed: {exception}");
            }
        }

        private static AbilityDefinitionRegistry CreateDefinitions()
        {
            Assert(AbilityDefinitionRegistry.TryCreate(
                       new IAbilityDefinition[]
                       {
                           new TestPresentedAbility(
                               "ability:strike",
                               "  Heavy Strike ",
                               "  Generate rage.  "),
                           new TestAbility("ability:slash"),
                           new TestAbility("ability:guard")
                       },
                       out AbilityDefinitionRegistry definitions,
                       out string error),
                $"Could not prepare ability definitions: {error}");
            return definitions;
        }

        private static RunAbilityChoiceState CreateChoice(
            string choiceId,
            int targetSlotIndex,
            int optionCount,
            params string[] abilityIds)
        {
            RunAbilityLoadoutService loadout = new();
            Assert(loadout.TryRegisterParticipant(
                       new RunParticipantIdentity(
                           "player:local",
                           "character:warrior")).Success,
                "Could not prepare the validation participant.");
            RunAbilityChoiceService choices = new(loadout);
            RunAbilityChoiceResult offered = choices.TryOfferChoice(
                new RunAbilityChoiceRequest(
                    "player:local",
                    choiceId,
                    targetSlotIndex,
                    abilityIds,
                    optionCount,
                    41));
            Assert(offered.Success && offered.State != null,
                "Could not prepare a pending validation choice.");
            return offered.State;
        }

        private static HubStartingAbilityOption FindOption(
            HubStartingAbilitySelectionModel model,
            string abilityId)
        {
            for (int i = 0; i < model.Options.Count; i++)
            {
                if (model.Options[i].AbilityId == abilityId)
                    return model.Options[i];
            }

            return null;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private class TestAbility : IAbilityDefinition
        {
            public TestAbility(string abilityId)
            {
                AbilityId = abilityId;
            }

            public string AbilityId { get; }
        }

        private sealed class TestPresentedAbility :
            TestAbility,
            IAbilityPresentationDefinition
        {
            public TestPresentedAbility(
                string abilityId,
                string displayName,
                string description)
                : base(abilityId)
            {
                DisplayName = displayName;
                Description = description;
            }

            public string DisplayName { get; }
            public string Description { get; }
            public Sprite Icon => null;
        }
    }
}
