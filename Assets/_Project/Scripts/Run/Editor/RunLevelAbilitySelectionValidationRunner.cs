using System;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Run.Editor
{
    public static class RunLevelAbilitySelectionValidationRunner
    {
        [MenuItem("Tools/Titanhold/Validate Run Level Ability Selection")]
        public static void Validate()
        {
            try
            {
                ValidateMilestoneSequence();
                ValidateScheduleRules();
                Debug.Log(
                    "Run Level Ability Selection validation passed (2 scenarios).");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Run Level Ability Selection validation failed: {exception}");
            }
        }

        private static void ValidateMilestoneSequence()
        {
            RunParticipantIdentity identity = new(
                "player:local",
                "character:warrior");
            RunProgressionService progression = new(
                new RunExperienceCurve(new[] { 10, 10, 10, 10 }));
            Assert(
                progression.TryRegisterParticipant(identity).Success,
                "Progression participant registration failed.");

            RunAbilityLoadoutService loadout = new();
            Assert(
                loadout.TryRegisterParticipant(identity).Success,
                "Ability participant registration failed.");
            RunAbilityChoiceService choices = new(loadout);

            RunAbilityUnlockMilestone levelTwo = new(
                2,
                1,
                3,
                new[] { "ability:a", "ability:b", "ability:c" });
            RunAbilityUnlockMilestone levelThree = new(
                3,
                2,
                2,
                new[] { "ability:d", "ability:e", "ability:f" });
            Assert(
                RunAbilityUnlockSchedule.TryCreate(
                    "schedule:warrior",
                    "archetype:warrior",
                    new[] { levelThree, levelTwo },
                    out RunAbilityUnlockSchedule schedule,
                    out _),
                "Valid ability schedule was rejected.");

            using RunLevelAbilitySelectionService service = new(
                progression,
                choices,
                new[]
                {
                    new RunAbilityUnlockParticipantPlan(
                        identity.PlayerId,
                        0,
                        schedule)
                },
                runSeed: 12345);

            Assert(
                progression.TryGrantExperience(identity.PlayerId, 9).Success &&
                !choices.TryGetPendingChoice(identity.PlayerId, out _),
                "An ability choice appeared before its milestone.");
            RunProgressionResult crossedMilestones =
                progression.TryGrantExperience(identity.PlayerId, 21);
            bool hasFirstChoice = choices.TryGetPendingChoice(
                identity.PlayerId,
                out RunAbilityChoiceState first);
            Assert(
                crossedMilestones.Success &&
                hasFirstChoice,
                "The first crossed milestone was not offered.");
            Assert(
                first.TargetSlotIndex == 1 &&
                first.OfferedAbilityIds.Count == 3 &&
                first.ChoiceId == RunLevelAbilitySelectionService.CreateChoiceId(
                    schedule.ScheduleId,
                    levelTwo),
                "The first crossed milestone was not offered.");

            Assert(
                progression.TryAddGold(identity.PlayerId, 1).Success &&
                choices.TryGetPendingChoice(
                    identity.PlayerId,
                    out RunAbilityChoiceState retained) &&
                ReferenceEquals(first, retained),
                "An unrelated progression change replaced the pending choice.");

            string firstSelection = first.OfferedAbilityIds[0];
            RunAbilityChoiceResult firstResolution =
                choices.TrySelectAbility(
                    identity.PlayerId,
                    first.ChoiceId,
                    firstSelection);
            bool hasSecondChoice = choices.TryGetPendingChoice(
                identity.PlayerId,
                out RunAbilityChoiceState second);
            Assert(
                firstResolution.Success &&
                hasSecondChoice,
                "Crossed milestones were not sequenced after resolution.");
            Assert(
                second.TargetSlotIndex == 2 &&
                second.OfferedAbilityIds.Count == 2 &&
                second.ChoiceId == RunLevelAbilitySelectionService.CreateChoiceId(
                    schedule.ScheduleId,
                    levelThree),
                "Crossed milestones were not sequenced after resolution.");

            string secondSelection = second.OfferedAbilityIds[0];
            Assert(
                choices.TrySelectAbility(
                    identity.PlayerId,
                    second.ChoiceId,
                    secondSelection).Success &&
                !choices.TryGetPendingChoice(identity.PlayerId, out _) &&
                loadout.TryGetParticipant(
                    identity.PlayerId,
                    out RunParticipantAbilityState abilities) &&
                abilities.TryGetAbilitySlot(1, out string slotOne) &&
                abilities.TryGetAbilitySlot(2, out string slotTwo) &&
                slotOne == firstSelection &&
                slotTwo == secondSelection,
                "Resolved milestone choices did not update their target slots.");

            service.Dispose();
            Assert(
                service.TryOfferNext(identity.PlayerId).Error ==
                RunLevelAbilitySelectionError.ServiceDisposed,
                "Disposed milestone service still accepted commands.");
        }

        private static void ValidateScheduleRules()
        {
            RunAbilityUnlockMilestone first = new(
                2,
                1,
                1,
                new[] { "ability:a" });
            RunAbilityUnlockMilestone duplicateLevel = new(
                2,
                2,
                1,
                new[] { "ability:b" });
            RunAbilityUnlockMilestone duplicateSlot = new(
                3,
                1,
                1,
                new[] { "ability:c" });
            RunAbilityUnlockMilestone invalidPool = new(
                4,
                3,
                2,
                new[] { "ability:d" });

            Assert(
                !RunAbilityUnlockSchedule.TryCreate(
                    "schedule:duplicate-level",
                    "archetype:warrior",
                    new[] { first, duplicateLevel },
                    out _,
                    out _) &&
                !RunAbilityUnlockSchedule.TryCreate(
                    "schedule:duplicate-slot",
                    "archetype:warrior",
                    new[] { first, duplicateSlot },
                    out _,
                    out _) &&
                !RunAbilityUnlockSchedule.TryCreate(
                    "schedule:invalid-pool",
                    "archetype:warrior",
                    new[] { invalidPool },
                    out _,
                    out _) &&
                !RunAbilityUnlockSchedule.TryCreate(
                    " schedule:whitespace ",
                    "archetype:warrior",
                    new[] { first },
                    out _,
                    out _),
                "Invalid ability schedules were accepted.");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
