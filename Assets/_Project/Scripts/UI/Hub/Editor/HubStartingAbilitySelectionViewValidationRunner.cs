using System;
using Titanhold.Combat.Abilities;
using Titanhold.Run;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Titanhold.UI.Hub.Editor
{
    public static class HubStartingAbilitySelectionViewValidationRunner
    {
        [MenuItem("Tools/Titanhold/Validate Starting Ability View")]
        public static void Validate()
        {
            GameObject fixture = null;
            try
            {
                fixture = new GameObject("StartingAbilityViewValidation");
                GameObject selectionRoot = new("SelectionRoot");
                selectionRoot.transform.SetParent(fixture.transform, false);
                GameObject viewObject = new("View");
                viewObject.transform.SetParent(fixture.transform, false);
                HubStartingAbilitySelectionView view =
                    viewObject.AddComponent<HubStartingAbilitySelectionView>();
                TMP_Text status = CreateText(selectionRoot.transform, "Status");
                Button[] buttons = new Button[3];
                TMP_Text[] names = new TMP_Text[3];
                TMP_Text[] descriptions = new TMP_Text[3];
                Image[] icons = new Image[3];
                for (int i = 0; i < buttons.Length; i++)
                {
                    GameObject option = new($"Option{i}");
                    option.transform.SetParent(selectionRoot.transform, false);
                    icons[i] = option.AddComponent<Image>();
                    buttons[i] = option.AddComponent<Button>();
                    names[i] = CreateText(option.transform, "Name");
                    descriptions[i] = CreateText(
                        option.transform,
                        "Description");
                }

                view.enabled = false;
                view.ConfigureForEditor(
                    selectionRoot,
                    status,
                    buttons,
                    names,
                    descriptions,
                    icons);
                view.enabled = true;
                Assert(view.HasRequiredReferences,
                    "Complete option references were rejected.");

                HubStartingAbilitySelectionModel model = CreateModel();
                selectionRoot.SetActive(false);
                Assert(view.TryShow(model) && selectionRoot.activeSelf,
                    "A valid starting selection was not shown.");
                for (int i = 0; i < model.Options.Count; i++)
                {
                    Assert(names[i].text == model.Options[i].DisplayName &&
                           descriptions[i].text ==
                               model.Options[i].Description &&
                           icons[i].sprite == model.Options[i].Icon &&
                           icons[i].enabled ==
                               (model.Options[i].Icon != null) &&
                           buttons[i].interactable,
                        $"Option {i} was not rendered correctly.");
                }

                int selectedIndex = -1;
                view.OptionSelected += index => selectedIndex = index;
                buttons[2].onClick.Invoke();
                Assert(selectedIndex == 2,
                    "The view did not emit the selected option index.");

                view.SetInteractable(false);
                Assert(!buttons[0].interactable &&
                       !buttons[1].interactable &&
                       !buttons[2].interactable,
                    "The view did not lock all option buttons.");
                view.SetStatus("SELECTION REJECTED");
                Assert(status.text == "SELECTION REJECTED",
                    "The view did not show its status message.");
                view.Hide();
                Assert(!selectionRoot.activeSelf,
                    "The selection root remained visible after Hide.");

                Debug.Log("Starting Ability View validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Starting Ability View validation failed: {exception}");
            }
            finally
            {
                if (fixture != null)
                    UnityEngine.Object.DestroyImmediate(fixture);
            }
        }

        private static TMP_Text CreateText(Transform parent, string name)
        {
            GameObject target = new(name, typeof(RectTransform));
            target.transform.SetParent(parent, false);
            return target.AddComponent<TextMeshProUGUI>();
        }

        private static HubStartingAbilitySelectionModel CreateModel()
        {
            AbilityDefinitionRegistry definitions = CreateDefinitions();
            RunAbilityLoadoutService loadout = new();
            Assert(loadout.TryRegisterParticipant(
                       new RunParticipantIdentity(
                           "player:local",
                           "character:warrior")).Success,
                "Could not prepare the view participant.");
            RunAbilityChoiceService choices = new(loadout);
            RunAbilityChoiceResult offered = choices.TryOfferChoice(
                new RunAbilityChoiceRequest(
                    "player:local",
                    RunStartingAbilitySelectionService.StartingChoiceId,
                    RunStartReadinessService.StartingAbilitySlotIndex,
                    new[]
                    {
                        "ability:strike",
                        "ability:slash",
                        "ability:guard"
                    },
                    3,
                    71));
            Assert(offered.Success,
                "Could not prepare the view choice.");
            HubStartingAbilitySelectionPresenter presenter = new(definitions);
            Assert(presenter.TryBuild(
                       offered.State,
                       out HubStartingAbilitySelectionModel model,
                       out _),
                "Could not prepare the view model.");
            return model;
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
                $"Could not prepare view definitions: {error}");
            return definitions;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private sealed class TestAbility :
            IAbilityDefinition,
            IAbilityPresentationDefinition
        {
            public TestAbility(string abilityId)
            {
                AbilityId = abilityId;
            }

            public string AbilityId { get; }
            public string DisplayName => $"Name: {AbilityId}";
            public string Description => $"Description: {AbilityId}";
            public Sprite Icon => null;
        }
    }
}
