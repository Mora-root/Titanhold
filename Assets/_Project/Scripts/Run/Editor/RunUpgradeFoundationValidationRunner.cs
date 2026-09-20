using System;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Run.Editor
{
    public static class RunUpgradeFoundationValidationRunner
    {
        [MenuItem("Tools/Titanhold/Validate Run Upgrade Foundation")]
        public static void Validate()
        {
            try
            {
                ValidateCatalog();
                ValidateChoicesAndStacks();
                Debug.Log(
                    "Run Upgrade Foundation validation passed (2 scenarios).");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Run Upgrade Foundation validation failed: {exception}");
            }
        }

        private static void ValidateCatalog()
        {
            ValidationUpgrade first = new("upgrade:damage");
            ValidationUpgrade second = new("upgrade:health");
            Assert(
                RunUpgradeDefinitionRegistry.TryCreate(
                    new IRunUpgradeDefinition[] { first, second },
                    out RunUpgradeDefinitionRegistry registry,
                    out _) &&
                registry.Count == 2 &&
                registry.TryResolve(first.UpgradeId, out _) &&
                registry.TryResolve(second.UpgradeId, out _),
                "Valid upgrade definitions did not build a registry.");

            Assert(
                !RunUpgradeDefinitionRegistry.TryCreate(
                    new IRunUpgradeDefinition[] { first, first },
                    out _,
                    out _),
                "Duplicate upgrade ids exposed a partial registry.");
        }

        private static void ValidateChoicesAndStacks()
        {
            ValidationUpgrade first = new("upgrade:damage");
            ValidationUpgrade second = new("upgrade:health");
            ValidationUpgrade third = new("upgrade:armor");
            ValidationUpgrade fourth = new("upgrade:speed");
            Assert(
                RunUpgradeDefinitionRegistry.TryCreate(
                    new IRunUpgradeDefinition[]
                    {
                        first,
                        second,
                        third,
                        fourth
                    },
                    out RunUpgradeDefinitionRegistry registry,
                    out _),
                "Choice registry could not be created.");

            RunUpgradeChoiceService service = new(registry);
            RunParticipantIdentity identity = new(
                "player:local",
                "character:warrior");
            Assert(
                service.TryRegisterParticipant(identity).Success,
                "Upgrade participant could not be registered.");

            string[] candidates =
            {
                first.UpgradeId,
                second.UpgradeId,
                third.UpgradeId,
                fourth.UpgradeId
            };
            RunUpgradeChoiceResult offer = service.TryOfferChoice(
                new RunUpgradeChoiceRequest(
                    identity.PlayerId,
                    "choice:upgrade:2",
                    candidates,
                    3,
                    12345));
            Assert(
                offer.Success &&
                offer.Choice.OfferedUpgradeIds.Count == 3 &&
                AreUnique(offer.Choice.OfferedUpgradeIds),
                "Upgrade offer was not a unique deterministic subset.");

            string selected = offer.Choice.OfferedUpgradeIds[0];
            Assert(
                service.TrySelectUpgrade(
                    identity.PlayerId,
                    offer.Choice.ChoiceId,
                    selected).Success &&
                offer.Participant.GetStackCount(selected) == 1,
                "Selecting an upgrade did not add its first stack.");

            RunUpgradeChoiceResult repeatedOffer = service.TryOfferChoice(
                new RunUpgradeChoiceRequest(
                    identity.PlayerId,
                    "choice:upgrade:4",
                    candidates,
                    3,
                    12345));
            Assert(
                repeatedOffer.Success &&
                repeatedOffer.Choice.ContainsUpgrade(selected) &&
                service.TrySelectUpgrade(
                    identity.PlayerId,
                    repeatedOffer.Choice.ChoiceId,
                    selected).Success &&
                repeatedOffer.Participant.GetStackCount(selected) == 2 &&
                repeatedOffer.Participant.SelectionHistory.Count == 2,
                "A repeated upgrade selection did not stack.");

            Assert(
                service.TryOfferChoice(
                    new RunUpgradeChoiceRequest(
                        identity.PlayerId,
                        "choice:upgrade:invalid",
                        new[]
                        {
                            first.UpgradeId,
                            first.UpgradeId,
                            second.UpgradeId
                        },
                        3,
                        1)).Error ==
                    RunUpgradeChoiceError.InvalidCandidatePool,
                "A duplicate candidate pool was accepted.");
        }

        private static bool AreUnique(
            System.Collections.Generic.IReadOnlyList<string> values)
        {
            for (int i = 0; i < values.Count; i++)
            {
                for (int j = i + 1; j < values.Count; j++)
                {
                    if (values[i] == values[j])
                        return false;
                }
            }

            return true;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private sealed class ValidationUpgrade : IRunUpgradeDefinition
        {
            public ValidationUpgrade(string upgradeId)
            {
                UpgradeId = upgradeId;
            }

            public string UpgradeId { get; }

            public bool TryValidate(out string error)
            {
                error = string.Empty;
                return !string.IsNullOrWhiteSpace(UpgradeId) &&
                       UpgradeId == UpgradeId.Trim();
            }
        }
    }
}
