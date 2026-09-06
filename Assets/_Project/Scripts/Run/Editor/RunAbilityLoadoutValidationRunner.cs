using System;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Run.Editor
{
    public static class RunAbilityLoadoutValidationRunner
    {
        [MenuItem("Tools/Titanhold/Validate Run Ability Loadout")]
        public static void ValidateFromMenu()
        {
            try
            {
                ValidateParticipantRoster();
                ValidateAbilityCommands();
                Debug.Log("Run Ability Loadout validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Run Ability Loadout validation failed: {exception}");
            }
        }

        private static void ValidateParticipantRoster()
        {
            RunAbilityLoadoutService service = new(
                abilitySlotCount: 5,
                maximumParticipantCount: 2);

            RunAbilityLoadoutResult first = service.TryRegisterParticipant(
                new RunParticipantIdentity("player:one", "character:one"));
            Assert(first.Success &&
                   first.State.PlayerId == "player:one" &&
                   first.State.CharacterId == "character:one" &&
                   first.State.AbilitySlotCount == 5,
                "Ability roster did not register a valid participant.");

            Assert(service.TryRegisterParticipant(
                       new RunParticipantIdentity(
                           "player:two",
                           "character:two")).Success,
                "Ability roster did not register its second participant.");
            Assert(service.TryRegisterParticipant(
                       new RunParticipantIdentity(
                           "player:one",
                           "character:other")).Error ==
                   RunAbilityLoadoutError.DuplicatePlayer,
                "Ability roster accepted a duplicate player id.");
            Assert(service.TryRegisterParticipant(
                       new RunParticipantIdentity(
                           "player:other",
                           "character:one")).Error ==
                   RunAbilityLoadoutError.DuplicateCharacter,
                "Ability roster accepted a duplicate character id.");
            Assert(service.TryRegisterParticipant(
                       new RunParticipantIdentity(
                           "player:three",
                           "character:three")).Error ==
                   RunAbilityLoadoutError.ParticipantLimitExceeded,
                "Ability roster exceeded its participant limit.");
        }

        private static void ValidateAbilityCommands()
        {
            RunAbilityLoadoutService service = new();
            int changeCount = 0;
            service.StateChanged += _ => changeCount++;
            Assert(service.TryRegisterParticipant(
                       new RunParticipantIdentity(
                           "player:local",
                           "character:warrior")).Success,
                "Could not prepare the ability command participant.");

            RunAbilityLoadoutResult grant = service.TryGrantAbility(
                " player:local ",
                " ability:spin ");
            Assert(grant.Success &&
                   grant.AbilityId == "ability:spin" &&
                   grant.State.HasAbility("ability:spin") &&
                   grant.State.GrantedAbilityCount == 1,
                "Grant command did not normalize and add the ability.");
            Assert(service.TryGrantAbility(
                       "player:local",
                       "ability:spin").Error ==
                   RunAbilityLoadoutError.AbilityAlreadyGranted,
                "Grant command accepted the same ability twice.");
            Assert(service.TryAssignAbility(
                       "player:local",
                       "ability:unknown",
                       0).Error == RunAbilityLoadoutError.AbilityNotGranted,
                "Assignment accepted an ability the participant does not own.");
            Assert(service.TryAssignAbility(
                       "player:local",
                       "ability:spin",
                       5).Error == RunAbilityLoadoutError.InvalidSlot,
                "Assignment accepted an out-of-range slot.");

            RunAbilityLoadoutResult assign = service.TryAssignAbility(
                "player:local",
                "ability:spin",
                0);
            Assert(assign.Success && assign.Changed &&
                   assign.PreviousSlotIndex == -1 &&
                   assign.State.TryGetAbilitySlot(0, out string firstSlot) &&
                   firstSlot == "ability:spin",
                "Granted ability was not assigned to its slot.");

            RunAbilityLoadoutResult repeated = service.TryAssignAbility(
                "player:local",
                "ability:spin",
                0);
            Assert(repeated.Success && !repeated.Changed,
                "Repeated slot assignment was not idempotent.");

            RunAbilityLoadoutResult dash =
                service.TryGrantAndAssignAbility(
                    "player:local",
                    "ability:dash",
                    1);
            Assert(dash.Success &&
                   dash.State.HasAbility("ability:dash") &&
                   dash.State.TryGetAbilitySlot(1, out string secondSlot) &&
                   secondSlot == "ability:dash",
                "Atomic grant and assignment command failed.");

            RunAbilityLoadoutResult rejectedAtomic =
                service.TryGrantAndAssignAbility(
                    "player:local",
                    "ability:invalid-slot",
                    9);
            Assert(!rejectedAtomic.Success &&
                   !dash.State.HasAbility("ability:invalid-slot"),
                "Rejected atomic command partially granted an ability.");

            Assert(service.TryGrantAndAssignAbility(
                       "player:local",
                       "ability:guard",
                       1).Success &&
                   dash.State.TryGetAbilitySlot(1, out secondSlot) &&
                   secondSlot == "ability:guard" &&
                   dash.State.HasAbility("ability:dash"),
                "Slot replacement removed ownership or kept the old assignment.");

            RunAbilityLoadoutResult move = service.TryAssignAbility(
                "player:local",
                "ability:spin",
                2);
            Assert(move.Success &&
                   move.PreviousSlotIndex == 0 &&
                   move.State.TryGetAbilitySlot(0, out firstSlot) &&
                   firstSlot.Length == 0 &&
                   move.State.TryGetAbilitySlot(2, out string thirdSlot) &&
                   thirdSlot == "ability:spin",
                "Moving an ability duplicated it or retained its old slot.");

            RunAbilityLoadoutResult clear = service.TryClearAbilitySlot(
                "player:local",
                2);
            RunAbilityLoadoutResult repeatedClear =
                service.TryClearAbilitySlot("player:local", 2);
            Assert(clear.Success && clear.Changed &&
                   repeatedClear.Success && !repeatedClear.Changed &&
                   clear.State.HasAbility("ability:spin") &&
                   clear.State.TryGetAbilitySlot(2, out thirdSlot) &&
                   thirdSlot.Length == 0,
                "Clearing a slot changed ownership or was not idempotent.");

            Assert(changeCount == 7,
                "Rejected or idempotent ability commands emitted state changes.");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
