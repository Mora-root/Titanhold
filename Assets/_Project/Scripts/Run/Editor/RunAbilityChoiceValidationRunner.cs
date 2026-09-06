using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Run.Editor
{
    public static class RunAbilityChoiceValidationRunner
    {
        private static readonly string[] CandidatePool =
        {
            "ability:spin",
            "ability:slash",
            "ability:guard",
            "ability:dash",
            "ability:nova"
        };

        [MenuItem("Tools/Titanhold/Validate Run Ability Choices")]
        public static void ValidateFromMenu()
        {
            try
            {
                ValidateDeterministicOfferAndSelection();
                ValidateRejectedAndConcurrentCommands();
                Debug.Log("Run Ability Choices validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Run Ability Choices validation failed: {exception}");
            }
        }

        private static void ValidateDeterministicOfferAndSelection()
        {
            RunAbilityLoadoutService loadout = CreateLoadout();
            RunAbilityChoiceService choices = new(loadout);
            int offeredEvents = 0;
            int resolvedEvents = 0;
            choices.ChoiceOffered += _ => offeredEvents++;
            choices.ChoiceResolved += (_, _) => resolvedEvents++;

            RunAbilityChoiceRequest request = new(
                " player:one ",
                " choice:level:5 ",
                targetSlotIndex: 1,
                candidateAbilityIds: CandidatePool,
                optionCount: 3,
                rollSeed: 731);
            RunAbilityChoiceResult offer = choices.TryOfferChoice(request);
            Assert(offer.Success &&
                   offer.State.PlayerId == "player:one" &&
                   offer.State.ChoiceId == "choice:level:5" &&
                   offer.State.TargetSlotIndex == 1 &&
                   offer.State.OfferedAbilityIds.Count == 3 &&
                   !offer.State.ContainsAbility("ability:spin") &&
                   choices.PendingChoiceCount == 1 &&
                   offeredEvents == 1,
                "Valid offer did not produce three unowned ability options.");

            RunAbilityChoiceService repeated = new(CreateLoadout());
            RunAbilityChoiceResult repeatedOffer =
                repeated.TryOfferChoice(request);
            Assert(repeatedOffer.Success &&
                   HaveSameOrder(
                       offer.State.OfferedAbilityIds,
                       repeatedOffer.State.OfferedAbilityIds),
                "Equal offer inputs did not produce a deterministic result.");

            string selected = offer.State.OfferedAbilityIds[0];
            RunAbilityChoiceResult selection = choices.TrySelectAbility(
                "player:one",
                "choice:level:5",
                selected);
            Assert(selection.Success &&
                   selection.SelectedAbilityId == selected &&
                   choices.PendingChoiceCount == 0 &&
                   choices.HasResolvedChoice(
                       "player:one",
                       "choice:level:5") &&
                   loadout.TryGetParticipant(
                       "player:one",
                       out RunParticipantAbilityState state) &&
                   state.HasAbility(selected) &&
                   state.TryGetAbilitySlot(1, out string assigned) &&
                   assigned == selected &&
                   resolvedEvents == 1,
                "Choice selection did not atomically grant and assign its ability.");
            Assert(choices.TryOfferChoice(request).Error ==
                       RunAbilityChoiceError.ChoiceAlreadyResolved &&
                   choices.TrySelectAbility(
                       "player:one",
                       "choice:level:5",
                       selected).Error ==
                       RunAbilityChoiceError.ChoiceAlreadyResolved,
                "Resolved choice could be replayed.");
        }

        private static void ValidateRejectedAndConcurrentCommands()
        {
            RunAbilityLoadoutService loadout = CreateLoadout();
            RunAbilityChoiceService choices = new(loadout);
            RunAbilityChoiceResult first = choices.TryOfferChoice(
                new RunAbilityChoiceRequest(
                    "player:one",
                    "choice:level:5",
                    1,
                    CandidatePool,
                    3,
                    42));
            Assert(first.Success,
                "Could not prepare rejected choice commands.");
            Assert(choices.TryOfferChoice(
                       new RunAbilityChoiceRequest(
                           "player:one",
                           "choice:level:10",
                           2,
                           CandidatePool,
                           3,
                           43)).Error ==
                   RunAbilityChoiceError.ChoiceAlreadyPending,
                "Participant received two concurrent choices.");
            Assert(choices.TrySelectAbility(
                       "player:one",
                       "choice:wrong",
                       first.State.OfferedAbilityIds[0]).Error ==
                   RunAbilityChoiceError.ChoiceMismatch &&
                   choices.TrySelectAbility(
                       "player:one",
                       "choice:level:5",
                       "ability:not-offered").Error ==
                   RunAbilityChoiceError.AbilityNotOffered &&
                   choices.PendingChoiceCount == 1,
                "Rejected selection mutated the pending choice.");

            RunAbilityChoiceResult concurrent = choices.TryOfferChoice(
                new RunAbilityChoiceRequest(
                    "player:two",
                    "choice:level:5",
                    1,
                    CandidatePool,
                    3,
                    42));
            Assert(concurrent.Success && choices.PendingChoiceCount == 2,
                "One participant blocked another participant's choice.");

            List<string> duplicatePool = new()
            {
                "ability:a",
                " ability:a ",
                "ability:b"
            };
            Assert(choices.TryOfferChoice(
                       new RunAbilityChoiceRequest(
                           "player:missing",
                           "choice:test",
                           0,
                           CandidatePool,
                           3,
                           1)).Error ==
                   RunAbilityChoiceError.ParticipantNotFound &&
                   new RunAbilityChoiceService(CreateLoadout())
                       .TryOfferChoice(
                           new RunAbilityChoiceRequest(
                               "player:one",
                               "choice:duplicate",
                               1,
                               duplicatePool,
                               2,
                               1)).Error ==
                   RunAbilityChoiceError.InvalidCandidatePool &&
                   new RunAbilityChoiceService(CreateLoadout())
                       .TryOfferChoice(
                           new RunAbilityChoiceRequest(
                               "player:one",
                               "choice:insufficient",
                               1,
                               CandidatePool,
                               5,
                               1)).Error ==
                   RunAbilityChoiceError.InsufficientEligibleAbilities,
                "Invalid participant or candidate pools were accepted.");

            string externallyGranted =
                concurrent.State.OfferedAbilityIds[0];
            Assert(loadout.TryGrantAbility(
                       "player:two",
                       externallyGranted).Success,
                "Could not prepare a loadout conflict.");
            RunAbilityChoiceResult conflict = choices.TrySelectAbility(
                "player:two",
                "choice:level:5",
                externallyGranted);
            Assert(!conflict.Success &&
                   conflict.Error == RunAbilityChoiceError.LoadoutRejected &&
                   conflict.LoadoutError ==
                       RunAbilityLoadoutError.AbilityAlreadyGranted &&
                   choices.TryGetPendingChoice(
                       "player:two",
                       out RunAbilityChoiceState preserved) &&
                   ReferenceEquals(preserved, concurrent.State) &&
                   !choices.HasResolvedChoice(
                       "player:two",
                       "choice:level:5"),
                "Loadout rejection consumed or resolved the pending choice.");
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
                       0).Success,
                "Could not prepare the ability loadout.");
            return loadout;
        }

        private static bool HaveSameOrder(
            IReadOnlyList<string> first,
            IReadOnlyList<string> second)
        {
            if (first.Count != second.Count)
                return false;

            for (int i = 0; i < first.Count; i++)
            {
                if (!string.Equals(
                        first[i],
                        second[i],
                        StringComparison.Ordinal))
                {
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
    }
}
