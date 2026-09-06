using System;
using Titanhold.Combat.Abilities;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Run.Editor
{
    public static class RunStartingAbilityPoolValidationRunner
    {
        [MenuItem("Tools/Titanhold/Validate Starting Ability Pools")]
        public static void ValidateFromMenu()
        {
            try
            {
                ValidateRegistryAndSelectionIntegration();
                ValidateInvalidRegistries();
                Debug.Log("Starting Ability Pools validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Starting Ability Pools validation failed: {exception}");
            }
        }

        private static void ValidateRegistryAndSelectionIntegration()
        {
            AbilityDefinitionRegistry abilities = CreateAbilityRegistry(
                "ability:strike",
                "ability:slash",
                "ability:bash",
                "ability:bolt",
                "ability:frost",
                "ability:universal-guard");
            RunStartingAbilityPool warrior = CreatePool(
                "starting-pool:warrior",
                "archetype:warrior",
                "ability:strike",
                "ability:slash",
                "ability:universal-guard");
            RunStartingAbilityPool mage = CreatePool(
                "starting-pool:mage",
                "archetype:mage",
                "ability:bolt",
                "ability:frost",
                "ability:universal-guard");
            Assert(RunStartingAbilityPoolRegistry.TryCreate(
                       new[] { warrior, mage },
                       abilities,
                       out RunStartingAbilityPoolRegistry pools,
                       out string error),
                $"Valid starting pools were rejected: {error}");
            Assert(pools.Count == 2 &&
                   pools.TryResolve(
                       " archetype:warrior ",
                       out RunStartingAbilityPool resolved) &&
                   ReferenceEquals(warrior, resolved) &&
                   !pools.TryResolve("archetype:missing", out _),
                "Starting pool resolution did not use stable archetype ids.");

            RunAbilityLoadoutService loadout = new();
            RunParticipantIdentity participant = new(
                "player:local",
                "character:warrior");
            Assert(loadout.TryRegisterParticipant(participant).Success,
                "Could not prepare the starting-pool participant.");
            using RunStartReadinessService readiness = new(
                loadout,
                new[] { participant });
            RunAbilityChoiceService choices = new(loadout);
            RunStartingAbilitySelectionService selection = new(
                choices,
                readiness);
            Assert(selection.TryOfferStartingChoice(
                       "player:local",
                       "choice:starter",
                       "archetype:missing",
                       pools,
                       41).Error ==
                   RunStartingAbilitySelectionError.StartingPoolNotFound,
                "A missing archetype pool was accepted.");

            RunStartingAbilitySelectionResult offered =
                selection.TryOfferStartingChoice(
                    "player:local",
                    "choice:starter",
                    "archetype:warrior",
                    pools,
                    41);
            Assert(offered.Success &&
                   offered.Choice.OfferedAbilityIds.Count == 3 &&
                   ContainsEvery(
                       offered.Choice.OfferedAbilityIds,
                       warrior.AbilityIds),
                "Resolved warrior pool was not offered intact.");
        }

        private static void ValidateInvalidRegistries()
        {
            AbilityDefinitionRegistry abilities = CreateAbilityRegistry(
                "ability:one",
                "ability:two",
                "ability:three");
            AssertRejected(
                null,
                abilities,
                "A missing starting pool list was accepted.");
            AssertRejected(
                Array.Empty<RunStartingAbilityPool>(),
                abilities,
                "An empty starting pool list was accepted.");
            AssertRejected(
                new[]
                {
                    CreatePool(
                        "starting-pool:invalid-count",
                        "archetype:warrior",
                        "ability:one",
                        "ability:two")
                },
                abilities,
                "A pool with fewer than three abilities was accepted.");
            AssertRejected(
                new[]
                {
                    CreatePool(
                        "starting-pool:duplicate-ability",
                        "archetype:warrior",
                        "ability:one",
                        "ability:one",
                        "ability:three")
                },
                abilities,
                "A pool with duplicate abilities was accepted.");
            AssertRejected(
                new[]
                {
                    CreatePool(
                        "starting-pool:unknown-ability",
                        "archetype:warrior",
                        "ability:one",
                        "ability:two",
                        "ability:missing")
                },
                abilities,
                "A pool with an unknown ability was accepted.");
            AssertRejected(
                new[]
                {
                    CreatePool(
                        " starting-pool:whitespace ",
                        "archetype:warrior",
                        "ability:one",
                        "ability:two",
                        "ability:three")
                },
                abilities,
                "A pool with a non-strict stable id was accepted.");
            AssertRejected(
                new[]
                {
                    CreatePool(
                        "starting-pool:one",
                        "archetype:warrior",
                        "ability:one",
                        "ability:two",
                        "ability:three"),
                    CreatePool(
                        "starting-pool:two",
                        "archetype:warrior",
                        "ability:one",
                        "ability:two",
                        "ability:three")
                },
                abilities,
                "Two pools for one archetype were accepted.");
            AssertRejected(
                new[]
                {
                    CreatePool(
                        "starting-pool:duplicate",
                        "archetype:warrior",
                        "ability:one",
                        "ability:two",
                        "ability:three"),
                    CreatePool(
                        "starting-pool:duplicate",
                        "archetype:mage",
                        "ability:one",
                        "ability:two",
                        "ability:three")
                },
                abilities,
                "A duplicate starting pool id was accepted.");
        }

        private static AbilityDefinitionRegistry CreateAbilityRegistry(
            params string[] abilityIds)
        {
            IAbilityDefinition[] definitions =
                new IAbilityDefinition[abilityIds.Length];
            for (int i = 0; i < abilityIds.Length; i++)
                definitions[i] = new TestAbilityDefinition(abilityIds[i]);

            Assert(AbilityDefinitionRegistry.TryCreate(
                       definitions,
                       out AbilityDefinitionRegistry registry,
                       out string error),
                $"Could not prepare ability definitions: {error}");
            return registry;
        }

        private static RunStartingAbilityPool CreatePool(
            string poolId,
            string archetypeId,
            params string[] abilityIds)
        {
            return new RunStartingAbilityPool(
                poolId,
                archetypeId,
                abilityIds);
        }

        private static bool ContainsEvery(
            System.Collections.Generic.IReadOnlyList<string> actual,
            System.Collections.Generic.IReadOnlyList<string> expected)
        {
            if (actual.Count != expected.Count)
                return false;

            for (int i = 0; i < expected.Count; i++)
            {
                bool found = false;
                for (int j = 0; j < actual.Count; j++)
                {
                    if (!string.Equals(
                            actual[j],
                            expected[i],
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    found = true;
                    break;
                }

                if (!found)
                    return false;
            }

            return true;
        }

        private static void AssertRejected(
            System.Collections.Generic.IReadOnlyList<
                RunStartingAbilityPool> pools,
            IAbilityDefinitionResolver abilities,
            string message)
        {
            Assert(!RunStartingAbilityPoolRegistry.TryCreate(
                       pools,
                       abilities,
                       out RunStartingAbilityPoolRegistry registry,
                       out string error) &&
                   registry == null &&
                   !string.IsNullOrWhiteSpace(error),
                message);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private sealed class TestAbilityDefinition : IAbilityDefinition
        {
            public TestAbilityDefinition(string abilityId)
            {
                AbilityId = abilityId;
            }

            public string AbilityId { get; }
        }
    }
}
