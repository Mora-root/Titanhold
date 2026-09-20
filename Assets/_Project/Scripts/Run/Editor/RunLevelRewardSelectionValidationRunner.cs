using System;
using Titanhold.Combat.Abilities;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Run.Editor
{
    public static class RunLevelRewardSelectionValidationRunner
    {
        [MenuItem("Tools/Titanhold/Validate Run Level Reward Selection")]
        public static void Validate()
        {
            try
            {
                ValidateCrossedLevelsRemainOrdered();
                ValidateConflictingMilestonesFail();
                Debug.Log(
                    "Run Level Reward Selection validation passed (2 scenarios).");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Run Level Reward Selection validation failed: {exception}");
            }
        }

        private static void ValidateCrossedLevelsRemainOrdered()
        {
            Fixture fixture = new(
                abilityLevels: new[] { 3, 7 },
                upgradeLevels: new[] { 2, 4, 5, 6 });
            using (fixture)
            {
                Assert(
                    fixture.Progression.TryGrantExperience(
                        fixture.PlayerId,
                        50).Success,
                    "Could not cross the configured reward levels.");
                AssertPendingUpgrade(fixture, 2);

                ResolvePendingUpgrade(fixture);
                AssertPendingAbility(fixture, 3);

                ResolvePendingAbility(fixture);
                AssertPendingUpgrade(fixture, 4);
                ResolvePendingUpgrade(fixture);
                AssertPendingUpgrade(fixture, 5);
                ResolvePendingUpgrade(fixture);
                AssertPendingUpgrade(fixture, 6);
                ResolvePendingUpgrade(fixture);

                Assert(
                    !fixture.AbilityChoices.TryGetPendingChoice(
                        fixture.PlayerId,
                        out _) &&
                    !fixture.UpgradeChoices.TryGetPendingChoice(
                        fixture.PlayerId,
                        out _) &&
                    fixture.Upgrades.GetStackCount(
                        fixture.UpgradeIds[0]) +
                    fixture.Upgrades.GetStackCount(
                        fixture.UpgradeIds[1]) +
                    fixture.Upgrades.GetStackCount(
                        fixture.UpgradeIds[2]) == 4,
                    "Queued rewards did not resolve exactly once.");
            }
        }

        private static void ValidateConflictingMilestonesFail()
        {
            Fixture fixture = new(
                abilityLevels: new[] { 2 },
                upgradeLevels: new[] { 2 });
            using (fixture)
            {
                RunLevelRewardSelectionResult failure = default;
                fixture.Rewards.OfferFailed += (_, result) =>
                    failure = result;
                fixture.Progression.TryGrantExperience(
                    fixture.PlayerId,
                    10);
                Assert(
                    !failure.Success &&
                    failure.Error ==
                        RunLevelRewardSelectionError
                            .ConflictingMilestones &&
                    !fixture.AbilityChoices.TryGetPendingChoice(
                        fixture.PlayerId,
                        out _) &&
                    !fixture.UpgradeChoices.TryGetPendingChoice(
                        fixture.PlayerId,
                        out _),
                    "Conflicting level rewards did not fail before offering.");
            }
        }

        private static void AssertPendingAbility(
            Fixture fixture,
            int expectedLevel)
        {
            Assert(
                fixture.AbilityChoices.TryGetPendingChoice(
                    fixture.PlayerId,
                    out RunAbilityChoiceState choice) &&
                choice.ChoiceId.Contains(
                    $"level:{expectedLevel}",
                    StringComparison.Ordinal) &&
                !fixture.UpgradeChoices.TryGetPendingChoice(
                    fixture.PlayerId,
                    out _),
                $"Run level {expectedLevel} did not offer only its ability.");
        }

        private static void AssertPendingUpgrade(
            Fixture fixture,
            int expectedLevel)
        {
            Assert(
                fixture.UpgradeChoices.TryGetPendingChoice(
                    fixture.PlayerId,
                    out RunUpgradeChoiceState choice) &&
                choice.ChoiceId.EndsWith(
                    $"level:{expectedLevel}",
                    StringComparison.Ordinal) &&
                !fixture.AbilityChoices.TryGetPendingChoice(
                    fixture.PlayerId,
                    out _),
                $"Run level {expectedLevel} did not offer only its upgrade.");
        }

        private static void ResolvePendingAbility(Fixture fixture)
        {
            Assert(
                fixture.AbilityChoices.TryGetPendingChoice(
                    fixture.PlayerId,
                    out RunAbilityChoiceState choice) &&
                fixture.AbilityChoices.TrySelectAbility(
                    fixture.PlayerId,
                    choice.ChoiceId,
                    choice.OfferedAbilityIds[0]).Success,
                "Pending ability could not be resolved.");
        }

        private static void ResolvePendingUpgrade(Fixture fixture)
        {
            Assert(
                fixture.UpgradeChoices.TryGetPendingChoice(
                    fixture.PlayerId,
                    out RunUpgradeChoiceState choice) &&
                fixture.UpgradeChoices.TrySelectUpgrade(
                    fixture.PlayerId,
                    choice.ChoiceId,
                    choice.OfferedUpgradeIds[0]).Success,
                "Pending upgrade could not be resolved.");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private sealed class Fixture : IDisposable
        {
            private readonly RunLevelAbilitySelectionService abilitySelection;
            private readonly RunLevelUpgradeSelectionService upgradeSelection;

            public Fixture(int[] abilityLevels, int[] upgradeLevels)
            {
                PlayerId = "player:local";
                RunParticipantIdentity identity = new(
                    PlayerId,
                    "character:warrior");
                Progression = new RunProgressionService(
                    new RunExperienceCurve(
                        new[] { 10, 10, 10, 10, 10, 10, 10 }));
                Progression.TryRegisterParticipant(identity);

                RunAbilityLoadoutService loadout = new();
                loadout.TryRegisterParticipant(identity);
                AbilityChoices = new RunAbilityChoiceService(loadout);
                string[] abilityIds =
                {
                    "ability:a",
                    "ability:b",
                    "ability:c",
                    "ability:d"
                };
                RunAbilityUnlockMilestone[] abilityMilestones =
                    new RunAbilityUnlockMilestone[abilityLevels.Length];
                for (int i = 0; i < abilityLevels.Length; i++)
                {
                    abilityMilestones[i] = new RunAbilityUnlockMilestone(
                        abilityLevels[i],
                        i + 1,
                        1,
                        abilityIds);
                }

                RunAbilityUnlockSchedule.TryCreate(
                    "schedule:abilities",
                    "archetype:warrior",
                    abilityMilestones,
                    out RunAbilityUnlockSchedule abilitySchedule,
                    out _);
                abilitySelection = new RunLevelAbilitySelectionService(
                    Progression,
                    AbilityChoices,
                    new[]
                    {
                        new RunAbilityUnlockParticipantPlan(
                            PlayerId,
                            0,
                            abilitySchedule)
                    },
                    123,
                    observeChanges: false);

                UpgradeIds = new[]
                {
                    "upgrade:damage",
                    "upgrade:health",
                    "upgrade:armor"
                };
                ValidationUpgrade[] definitions =
                {
                    new(UpgradeIds[0]),
                    new(UpgradeIds[1]),
                    new(UpgradeIds[2])
                };
                RunUpgradeDefinitionRegistry.TryCreate(
                    definitions,
                    out RunUpgradeDefinitionRegistry registry,
                    out _);
                UpgradeChoices = new RunUpgradeChoiceService(registry);
                UpgradeChoices.TryRegisterParticipant(identity);
                UpgradeChoices.TryGetParticipant(
                    PlayerId,
                    out RunParticipantUpgradeState upgrades);
                Upgrades = upgrades;

                RunUpgradeUnlockMilestone[] upgradeMilestones =
                    new RunUpgradeUnlockMilestone[upgradeLevels.Length];
                for (int i = 0; i < upgradeLevels.Length; i++)
                {
                    upgradeMilestones[i] = new RunUpgradeUnlockMilestone(
                        upgradeLevels[i],
                        3,
                        UpgradeIds);
                }

                RunUpgradeUnlockSchedule.TryCreate(
                    "schedule:upgrades",
                    "archetype:warrior",
                    upgradeMilestones,
                    out RunUpgradeUnlockSchedule upgradeSchedule,
                    out _);
                upgradeSelection = new RunLevelUpgradeSelectionService(
                    Progression,
                    UpgradeChoices,
                    new[]
                    {
                        new RunUpgradeUnlockParticipantPlan(
                            PlayerId,
                            0,
                            upgradeSchedule)
                    },
                    123,
                    observeChanges: false);

                Rewards = new RunLevelRewardSelectionService(
                    Progression,
                    AbilityChoices,
                    UpgradeChoices,
                    abilitySelection,
                    upgradeSelection,
                    new[] { PlayerId });
            }

            public string PlayerId { get; }
            public string[] UpgradeIds { get; }
            public RunProgressionService Progression { get; }
            public RunAbilityChoiceService AbilityChoices { get; }
            public RunUpgradeChoiceService UpgradeChoices { get; }
            public RunParticipantUpgradeState Upgrades { get; }
            public RunLevelRewardSelectionService Rewards { get; }

            public void Dispose()
            {
                Rewards.Dispose();
                abilitySelection.Dispose();
                upgradeSelection.Dispose();
            }
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
                return true;
            }
        }
    }
}
