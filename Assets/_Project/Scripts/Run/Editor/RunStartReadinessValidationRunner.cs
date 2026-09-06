using System;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Run.Editor
{
    public static class RunStartReadinessValidationRunner
    {
        [MenuItem("Tools/Titanhold/Validate Run Start Ability Readiness")]
        public static void ValidateFromMenu()
        {
            try
            {
                ValidateParticipantReadiness();
                ValidateRosterRequirements();
                Debug.Log("Run Start Ability Readiness validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Run Start Ability Readiness validation failed: {exception}");
            }
        }

        private static void ValidateParticipantReadiness()
        {
            RunAbilityLoadoutService loadout = CreateLoadout();
            using RunStartReadinessService readiness = new(
                loadout,
                new[]
                {
                    new RunParticipantIdentity(
                        "player:one",
                        "character:one"),
                    new RunParticipantIdentity(
                        "player:two",
                        "character:two")
                });
            int stateChanges = 0;
            int sealedEvents = 0;
            readiness.ParticipantReadinessChanged += _ => stateChanges++;
            readiness.ReadinessSealed += () => sealedEvents++;

            Assert(readiness.ParticipantCount == 2 &&
                   readiness.ConfirmedParticipantCount == 0 &&
                   !readiness.AllParticipantsConfirmed &&
                   !readiness.IsSealed,
                "Readiness did not start with an unconfirmed roster.");
            Assert(readiness.TryConfirmStartingAbility(
                       "player:missing",
                       "ability:spin").Error ==
                   RunStartReadinessError.ParticipantNotFound &&
                   readiness.TryConfirmStartingAbility(
                       "player:one",
                       "ability:missing").Error ==
                   RunStartReadinessError.AbilityNotAssignedToStartSlot &&
                   readiness.TrySeal().Error ==
                   RunStartReadinessError.NotAllParticipantsConfirmed,
                "Invalid readiness command was accepted.");

            RunStartReadinessResult first =
                readiness.TryConfirmStartingAbility(
                    " player:one ",
                    " ability:spin ");
            RunStartReadinessResult repeated =
                readiness.TryConfirmStartingAbility(
                    "player:one",
                    "ability:spin");
            Assert(first.Success && first.Changed &&
                   repeated.Success && !repeated.Changed &&
                   first.State.IsConfirmed &&
                   first.State.StartingAbilityId == "ability:spin" &&
                   readiness.ConfirmedParticipantCount == 1 &&
                   stateChanges == 1,
                "Valid starting ability was not confirmed idempotently.");

            Assert(loadout.TryAssignAbility(
                       "player:one",
                       "ability:spin",
                       1).Success &&
                   !first.State.IsConfirmed &&
                   readiness.ConfirmedParticipantCount == 0 &&
                   stateChanges == 2,
                "Changing the unsealed start slot did not revoke readiness.");
            Assert(loadout.TryAssignAbility(
                       "player:one",
                       "ability:spin",
                       RunStartReadinessService.StartingAbilitySlotIndex)
                       .Success &&
                   readiness.TryConfirmStartingAbility(
                       "player:one",
                       "ability:spin").Success,
                "Participant could not reconfirm the restored start slot.");

            Assert(loadout.TryGrantAndAssignAbility(
                       "player:two",
                       "ability:slash",
                       RunStartReadinessService.StartingAbilitySlotIndex)
                       .Success &&
                   readiness.TryConfirmStartingAbility(
                       "player:two",
                       "ability:slash").Success &&
                   readiness.AllParticipantsConfirmed &&
                   readiness.ConfirmedParticipantCount == 2,
                "Independent participant readiness did not complete the roster.");
            RunStartReadinessResult seal = readiness.TrySeal();
            RunStartReadinessResult repeatedSeal = readiness.TrySeal();
            Assert(seal.Success && seal.Changed &&
                   repeatedSeal.Success && !repeatedSeal.Changed &&
                   readiness.IsSealed && sealedEvents == 1,
                "Complete readiness could not be sealed idempotently.");

            Assert(loadout.TryAssignAbility(
                       "player:two",
                       "ability:slash",
                       2).Success &&
                   readiness.TryGetParticipant(
                       "player:two",
                       out RunParticipantStartReadinessState second) &&
                   second.IsConfirmed && readiness.IsSealed,
                "Sealed readiness changed with the active-run loadout.");
            Assert(readiness.TryConfirmStartingAbility(
                       "player:one",
                       "ability:spin").Error ==
                   RunStartReadinessError.ReadinessAlreadySealed,
                "Sealed readiness accepted a new confirmation command.");
        }

        private static void ValidateRosterRequirements()
        {
            RunAbilityLoadoutService loadout = CreateLoadout();
            AssertThrows<ArgumentException>(
                () => new RunStartReadinessService(
                    loadout,
                    Array.Empty<RunParticipantIdentity>()),
                "Empty readiness roster was accepted.");
            AssertThrows<ArgumentException>(
                () => new RunStartReadinessService(
                    loadout,
                    new[]
                    {
                        new RunParticipantIdentity(
                            "player:one",
                            "character:wrong")
                    }),
                "Readiness roster accepted a mismatched character.");
        }

        private static RunAbilityLoadoutService CreateLoadout()
        {
            RunAbilityLoadoutService loadout = new();
            Assert(loadout.TryRegisterParticipant(
                       new RunParticipantIdentity(
                           "player:one",
                           "character:one")).Success &&
                   loadout.TryRegisterParticipant(
                       new RunParticipantIdentity(
                           "player:two",
                           "character:two")).Success &&
                   loadout.TryGrantAndAssignAbility(
                       "player:one",
                       "ability:spin",
                       RunStartReadinessService.StartingAbilitySlotIndex)
                       .Success,
                "Could not prepare readiness loadout.");
            return loadout;
        }

        private static void AssertThrows<TException>(
            Action action,
            string message)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException(message);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
