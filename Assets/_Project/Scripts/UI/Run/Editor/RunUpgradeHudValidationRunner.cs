using System;
using Titanhold.Run;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Titanhold.UI.Run.Editor
{
    public static class RunUpgradeHudValidationRunner
    {
        [MenuItem("Tools/Titanhold/Validate Run Upgrade HUD Presentation")]
        public static void Validate()
        {
            GameObject fixture = null;
            try
            {
                RunUpgradeHudModel model = CreateModel();
                Assert(
                    model.Entries.Count == 2 &&
                    model.Entries[0].UpgradeId == "upgrade:damage" &&
                    model.Entries[0].StackCount == 2 &&
                    model.Entries[1].UpgradeId == "upgrade:health" &&
                    model.Entries[1].StackCount == 1,
                    "HUD model did not aggregate stacks in first-selection order.");

                fixture = new GameObject("RunUpgradeHudValidation");
                GameObject content = new("Content");
                content.transform.SetParent(fixture.transform, false);
                TMP_Text title = CreateText(content.transform, "Title");
                GameObject rowsObject = new(
                    "Rows",
                    typeof(RectTransform));
                rowsObject.transform.SetParent(content.transform, false);
                RectTransform rowsRoot =
                    rowsObject.GetComponent<RectTransform>();
                TMP_Text template = CreateText(
                    rowsRoot,
                    "RowTemplate");
                template.gameObject.SetActive(false);

                RunUpgradeHudView view =
                    fixture.AddComponent<RunUpgradeHudView>();
                view.ConfigureForEditor(
                    content,
                    title,
                    rowsRoot,
                    template);
                Assert(view.HasRequiredReferences && view.Render(model),
                    "A valid HUD model could not be rendered.");
                TMP_Text first = rowsRoot.Find("UpgradeRow_1")
                    ?.GetComponent<TMP_Text>();
                TMP_Text second = rowsRoot.Find("UpgradeRow_2")
                    ?.GetComponent<TMP_Text>();
                Assert(
                    content.activeSelf &&
                    title.text == "RUN UPGRADES" &&
                    first != null && first.gameObject.activeSelf &&
                    first.text == "Damage ×2" &&
                    second != null && second.gameObject.activeSelf &&
                    second.text == "Health",
                    "HUD rows were not rendered as a vertical stack list.");

                view.Clear();
                Assert(!content.activeSelf,
                    "Clearing the HUD did not hide its content.");
                Debug.Log(
                    "Run Upgrade HUD presentation validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Run Upgrade HUD presentation validation failed: {exception}");
            }
            finally
            {
                if (fixture != null)
                    UnityEngine.Object.DestroyImmediate(fixture);
            }
        }

        private static RunUpgradeHudModel CreateModel()
        {
            ValidationUpgrade[] definitions =
            {
                new("upgrade:damage", "Damage"),
                new("upgrade:health", "Health")
            };
            Assert(
                RunUpgradeDefinitionRegistry.TryCreate(
                    definitions,
                    out RunUpgradeDefinitionRegistry registry,
                    out string error),
                $"Could not create upgrade definitions: {error}");

            RunParticipantIdentity identity = new(
                "player:local",
                "character:warrior");
            RunUpgradeChoiceService choices = new(registry);
            Assert(choices.TryRegisterParticipant(identity).Success,
                "Could not register HUD participant.");
            Select(
                choices,
                identity.PlayerId,
                "choice:one",
                definitions,
                "upgrade:damage");
            Select(
                choices,
                identity.PlayerId,
                "choice:two",
                definitions,
                "upgrade:health");
            Select(
                choices,
                identity.PlayerId,
                "choice:three",
                definitions,
                "upgrade:damage");
            Assert(
                choices.TryGetParticipant(
                    identity.PlayerId,
                    out RunParticipantUpgradeState participant),
                "Could not resolve HUD participant state.");

            RunUpgradeHudModelBuilder builder = new(registry);
            Assert(
                builder.TryBuild(
                    participant,
                    out RunUpgradeHudModel model,
                    out RunUpgradeHudPresentationError presentationError),
                $"Could not build HUD model: {presentationError}");
            return model;
        }

        private static void Select(
            RunUpgradeChoiceService choices,
            string playerId,
            string choiceId,
            ValidationUpgrade[] definitions,
            string selection)
        {
            RunUpgradeChoiceResult offered = choices.TryOfferChoice(
                new RunUpgradeChoiceRequest(
                    playerId,
                    choiceId,
                    new[]
                    {
                        definitions[0].UpgradeId,
                        definitions[1].UpgradeId
                    },
                    2,
                    choiceId.GetHashCode()));
            Assert(
                offered.Success &&
                choices.TrySelectUpgrade(
                    playerId,
                    choiceId,
                    selection).Success,
                $"Could not resolve validation choice '{choiceId}'.");
        }

        private static TMP_Text CreateText(Transform parent, string name)
        {
            GameObject target = new(name, typeof(RectTransform));
            target.transform.SetParent(parent, false);
            return target.AddComponent<TextMeshProUGUI>();
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private sealed class ValidationUpgrade :
            IRunUpgradeDefinition,
            IRunUpgradePresentationDefinition
        {
            public ValidationUpgrade(string upgradeId, string displayName)
            {
                UpgradeId = upgradeId;
                DisplayName = displayName;
            }

            public string UpgradeId { get; }
            public string DisplayName { get; }
            public string Description => string.Empty;
            public Sprite Icon => null;

            public bool TryValidate(out string error)
            {
                error = string.Empty;
                return true;
            }
        }
    }
}
