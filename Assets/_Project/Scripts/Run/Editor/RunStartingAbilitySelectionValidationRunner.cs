using System;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Run.Editor
{
    public static class RunStartingAbilitySelectionValidationRunner
    {
        [MenuItem("Tools/Titanhold/Validate Starting Ability Selection")]
        public static void ValidateFromMenu()
        {
            try
            {
                ValidateStartingSelections();
                ValidateWrongSlotRejection();
                Debug.Log("Starting Ability Selection validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Starting Ability Selection validation failed: {exception}");
            }
        }

        private static void ValidateStartingSelections()
        {
            RunAbilityLoadoutService loadout = CreateLoadout();
            using RunStartReadinessService readiness = CreateReadiness(loadout);
            RunAbilityChoiceService choices = new(loadout);
            RunStartingAbilitySelectionService selection = new(
                choices,
                readiness);

            Assert(selection.TryOfferStartingChoice(null).Error ==
                   RunStartingAbilitySelectionError.InvalidRequest,
                "Null starting choice was accepted.");
            Assert(selection.TryOfferStartingChoice(
                       CreateRequest(
                           "player:one",
                           "choice:invalid-count",
                           "ability:strike",
                           "ability:slash")).Error ==
                   RunStartingAbilitySelectionError.InvalidStartingPool,
                "A starting pool with fewer than three abilities was accepted.");
            Assert(selection.TryOfferStartingChoice(
                       CreateRequest(
                           "player:one",
                           "choice:duplicate",
                           "ability:strike",
                           "ability:strike",
                           "ability:slash")).Error ==
                   RunStartingAbilitySelectionError.InvalidStartingPool,
                "A duplicated starting ability was accepted.");
            Assert(selection.TryOfferStartingChoice(
                       CreateRequest(
                           "player:missing",
                           "choice:missing",
                           "ability:strike",
                           "ability:slash",
                           "ability:bash")).Error ==
                   RunStartingAbilitySelectionError.ParticipantNotFound,
                "A starting choice was offered to a missing participant.");

            RunStartingAbilitySelectionResult firstOffer =
                selection.TryOfferStartingChoice(
                    CreateRequest(
                        "player:one",
                        "choice:starter:one",
                        "ability:strike",
                        "ability:slash",
                        "ability:bash"));
            RunStartingAbilitySelectionResult secondOffer =
                selection.TryOfferStartingChoice(
                    CreateRequest(
                        "player:two",
                        "choice:starter:two",
                        "ability:bolt",
                        "ability:frost",
                        "ability:shock"));
            Assert(firstOffer.Success && secondOffer.Success &&
                   firstOffer.Choice.OfferedAbilityIds.Count ==
                       RunStartingAbilitySelectionService
                           .StartingAbilityOptionCount &&
                   firstOffer.Choice.TargetSlotIndex ==
                       RunStartReadinessService.StartingAbilitySlotIndex &&
                   secondOffer.Choice.TargetSlotIndex ==
                       RunStartReadinessService.StartingAbilitySlotIndex,
                "Valid starting choices were not offered into slot zero.");
            RunStartingAbilitySelectionResult repeatedOffer =
                selection.TryOfferStartingChoice(
                    CreateRequest(
                        "player:one",
                        "choice:starter:one:repeated",
                        "ability:strike",
                        "ability:slash",
                        "ability:bash"));
            Assert(!repeatedOffer.Success &&
                   repeatedOffer.Error ==
                       RunStartingAbilitySelectionError.ChoiceRejected &&
                   repeatedOffer.ChoiceError ==
                       RunAbilityChoiceError.ChoiceAlreadyPending,
                "A second pending starting choice was accepted.");

            Assert(selection.TrySelectStartingAbility(
                       "player:one",
                       "choice:starter:one",
                       "ability:missing").ChoiceError ==
                   RunAbilityChoiceError.AbilityNotOffered &&
                   readiness.ConfirmedParticipantCount == 0,
                "An unoffered starting ability changed readiness.");

            string firstAbilityId = firstOffer.Choice.OfferedAbilityIds[0];
            RunStartingAbilitySelectionResult firstSelection =
                selection.TrySelectStartingAbility(
                    " player:one ",
                    " choice:starter:one ",
                    $" {firstAbilityId} ");
            Assert(firstSelection.Success &&
                   firstSelection.SelectedAbilityId == firstAbilityId &&
                   !firstSelection.RosterSealed &&
                   loadout.TryGetParticipant(
                       "player:one",
                       out RunParticipantAbilityState firstLoadout) &&
                   firstLoadout.HasAbility(firstAbilityId) &&
                   firstLoadout.TryGetAbilitySlot(
                       RunStartReadinessService.StartingAbilitySlotIndex,
                       out string assignedFirstAbilityId) &&
                   assignedFirstAbilityId == firstAbilityId &&
                   readiness.TryGetParticipant(
                       "player:one",
                       out RunParticipantStartReadinessState firstReadiness) &&
                   firstReadiness.StartingAbilityId == firstAbilityId &&
                   readiness.ConfirmedParticipantCount == 1 &&
                   !readiness.IsSealed,
                "The first participant's selection was not committed.");
            Assert(selection.TrySelectStartingAbility(
                       "player:one",
                       "choice:starter:one",
                       firstAbilityId).Error ==
                   RunStartingAbilitySelectionError
                       .ParticipantAlreadyConfirmed,
                "A confirmed participant selected a second starter.");

            string secondAbilityId = secondOffer.Choice.OfferedAbilityIds[1];
            RunStartingAbilitySelectionResult secondSelection =
                selection.TrySelectStartingAbility(
                    "player:two",
                    "choice:starter:two",
                    secondAbilityId);
            Assert(secondSelection.Success &&
                   secondSelection.SelectedAbilityId == secondAbilityId &&
                   secondSelection.RosterSealed &&
                   readiness.AllParticipantsConfirmed &&
                   readiness.IsSealed,
                "The final participant did not seal start readiness.");
            Assert(selection.TryOfferStartingChoice(
                       CreateRequest(
                           "player:one",
                           "choice:after-seal",
                           "ability:new-one",
                           "ability:new-two",
                           "ability:new-three")).Error ==
                   RunStartingAbilitySelectionError.ReadinessAlreadySealed,
                "A starting choice was offered after readiness was sealed.");
        }

        private static void ValidateWrongSlotRejection()
        {
            RunAbilityLoadoutService loadout = new();
            RunParticipantIdentity participant = new(
                "player:one",
                "character:one");
            Assert(loadout.TryRegisterParticipant(participant).Success,
                "Could not prepare wrong-slot loadout.");
            using RunStartReadinessService readiness = new(
                loadout,
                new[] { participant });
            RunAbilityChoiceService choices = new(loadout);
            RunStartingAbilitySelectionService selection = new(
                choices,
                readiness);

            Assert(choices.TryOfferChoice(
                       new RunAbilityChoiceRequest(
                           "player:one",
                           "choice:later-milestone",
                           1,
                           new[] { "ability:spin" },
                           1,
                           31)).Success,
                "Could not prepare a non-starting ability choice.");
            RunStartingAbilitySelectionResult result =
                selection.TrySelectStartingAbility(
                    "player:one",
                    "choice:later-milestone",
                    "ability:spin");
            Assert(!result.Success &&
                   result.Error ==
                       RunStartingAbilitySelectionError.ChoiceTargetsWrongSlot &&
                   choices.TryGetPendingChoice("player:one", out _) &&
                   readiness.ConfirmedParticipantCount == 0,
                "A later-slot choice was accepted as a starting ability.");
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
                           "character:two")).Success,
                "Could not prepare starting ability loadouts.");
            return loadout;
        }

        private static RunStartReadinessService CreateReadiness(
            RunAbilityLoadoutService loadout)
        {
            return new RunStartReadinessService(
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
        }

        private static RunStartingAbilityChoiceRequest CreateRequest(
            string playerId,
            string choiceId,
            params string[] abilities)
        {
            return new RunStartingAbilityChoiceRequest(
                playerId,
                choiceId,
                abilities,
                17);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
