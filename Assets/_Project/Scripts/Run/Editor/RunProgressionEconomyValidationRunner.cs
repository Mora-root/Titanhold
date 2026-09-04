using System;
using Titanhold.Progression;
using Titanhold.Session;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Run.Editor
{
    public static class RunProgressionEconomyValidationRunner
    {
        [MenuItem("Tools/Titanhold/Validate Run Progression Economy")]
        public static void ValidateFromMenu()
        {
            try
            {
                ValidateRunProgression();
                ValidateAccountCrystals();
                ValidateConclusionRewards();
                Debug.Log("Run Progression Economy validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Run Progression Economy validation failed: {exception}");
            }
        }

        private static void ValidateRunProgression()
        {
            RunExperienceCurve curve = new(new[] { 10, 20, 40 });
            RunProgressionService service = new(curve, 2);
            int changeCount = 0;
            service.StateChanged += _ => changeCount++;

            RunProgressionResult registration =
                service.TryRegisterParticipant(
                    new RunParticipantIdentity(
                        "player:one",
                        "character:one"));
            Assert(registration.Success &&
                   registration.State.Level == 1 &&
                   registration.State.Experience == 0 &&
                   registration.State.Gold == 0,
                "Run participant registration failed.");

            RunProgressionResult experience =
                service.TryGrantExperience(" player:one ", 35);
            Assert(experience.Success &&
                   experience.LevelsGained == 2 &&
                   experience.ExperienceApplied == 35 &&
                   experience.State.Level == 3 &&
                   experience.State.Experience == 5,
                "Run experience did not cross levels deterministically.");

            RunProgressionResult addGold =
                service.TryAddGold("player:one", 100);
            RunProgressionResult spendGold =
                service.TrySpendGold("player:one", 60);
            RunProgressionResult rejectedSpend =
                service.TrySpendGold("player:one", 50);
            Assert(addGold.Success && spendGold.Success &&
                   spendGold.State.Gold == 40 &&
                   !rejectedSpend.Success &&
                   rejectedSpend.Error ==
                       RunProgressionError.InsufficientGold &&
                   rejectedSpend.State.Gold == 40,
                "Run gold transaction was not atomic.");

            Assert(!service.TryRegisterParticipant(
                       new RunParticipantIdentity(
                           "player:one",
                           "character:two")).Success &&
                   !service.TryRegisterParticipant(
                       new RunParticipantIdentity(
                           "player:two",
                           "character:one")).Success,
                "Run progression accepted duplicate participant identities.");

            Assert(changeCount == 4,
                "Rejected progression commands emitted state changes.");
        }

        private static void ValidateAccountCrystals()
        {
            AccountCrystalWallet wallet = new();
            int changeCount = 0;
            wallet.AmountChanged += _ => changeCount++;

            CrystalWalletResult restore = wallet.TryRestore(25);
            CrystalWalletResult add = wallet.TryAdd(10);
            CrystalWalletResult spend = wallet.TrySpend(20);
            CrystalWalletResult rejected = wallet.TrySpend(16);

            Assert(restore.Success && add.Success && spend.Success &&
                   wallet.Amount == 15 &&
                   !rejected.Success &&
                   rejected.Error ==
                       CrystalWalletError.InsufficientCrystals &&
                   changeCount == 3,
                "Account crystal transactions are inconsistent.");
        }

        private static void ValidateConclusionRewards()
        {
            RunConclusionRewardCalculator calculator = new(
                new RunConclusionRewardConfiguration(
                    characterExperiencePerCompletedRound: 100,
                    crystalsPerCompletedRound: 5,
                    victoryCharacterExperienceBonus: 200,
                    victoryCrystalBonus: 10));

            RunConclusionRewardResult defeat = calculator.Calculate(
                new RunResultSummary(
                    "run:defeat",
                    RunOutcome.Defeat,
                    completedRoundCount: 2),
                difficultyMultiplierPercent: 125);
            Assert(defeat.Success &&
                   defeat.CharacterExperience == 250 &&
                   defeat.Crystals == 12,
                "Defeat rewards did not use completed rounds and deterministic rounding.");

            RunConclusionRewardResult victory = calculator.Calculate(
                new RunResultSummary(
                    "run:victory",
                    RunOutcome.Victory,
                    completedRoundCount: 4),
                difficultyMultiplierPercent: 150);
            Assert(victory.Success &&
                   victory.CharacterExperience == 900 &&
                   victory.Crystals == 45,
                "Victory rewards did not include the completion bonus.");

            RunConclusionRewardResult invalid = calculator.Calculate(
                null,
                difficultyMultiplierPercent: 100);
            Assert(!invalid.Success &&
                   invalid.Error ==
                       RunConclusionRewardError.InvalidRunResult,
                "Conclusion reward calculator accepted an invalid result.");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
