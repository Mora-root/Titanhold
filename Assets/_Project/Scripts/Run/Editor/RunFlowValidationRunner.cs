using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Run.Editor
{
    public static class RunFlowValidationRunner
    {
        private const string MenuPath = "Tools/Titanhold/Validate Run Flow Foundation";

        [MenuItem(MenuPath)]
        public static void ValidateFromMenu()
        {
            try
            {
                Debug.Log(RunValidation());
            }
            catch (Exception exception)
            {
                Debug.LogError($"Run Flow foundation validation failed: {exception}");
            }
        }

        public static string RunValidation()
        {
            ValidateConfiguration();
            ValidateAtomicThreatBatch();
            ValidateInstabilityProgression();
            ValidateInvalidBatchIsAtomic();
            ValidateAssaultLifecycle();
            ValidateFinalEncounterLifecycle();
            ValidateTerminalState();

            return "Run Flow foundation validation passed.";
        }

        private static void ValidateConfiguration()
        {
            RunFlowConfiguration defaults = RunFlowConfiguration.CreateVerticalSliceDefaults();

            AssertApproximately(defaults.MaxThreat, 100f, "Default max Threat");
            Assert(defaults.InstabilityPointsPerLevel == 10, "Default instability threshold mismatch.");
            AssertApproximately(defaults.EnemyHealthBonusPerRound, 0.20f,
                "Default round health bonus");
            AssertApproximately(defaults.EnemyDamageBonusPerRound, 0.10f,
                "Default round damage bonus");
            AssertApproximately(defaults.AssaultHealthBonusPerLevel, 0.10f, "Default Assault health bonus");
            AssertApproximately(defaults.AssaultDamageBonusPerLevel, 0.05f, "Default Assault damage bonus");
            Assert(defaults.RegularRoundCount == 3,
                "Default regular round count mismatch.");
            Assert(defaults.FinalRoundNumber == 4,
                "Default final round number mismatch.");

            AssertThrows<ArgumentOutOfRangeException>(
                () => new RunFlowConfiguration(0f, 10, 0.2f, 0.1f, 0.1f, 0.05f),
                "Zero max Threat should be rejected.");
            AssertThrows<ArgumentOutOfRangeException>(
                () => new RunFlowConfiguration(100f, 0, 0.2f, 0.1f, 0.1f, 0.05f),
                "Zero instability threshold should be rejected.");
            AssertThrows<ArgumentOutOfRangeException>(
                () => new RunFlowConfiguration(100f, 10, 0.2f, 0.1f, -0.1f, 0.05f),
                "Negative health scaling should be rejected.");
            AssertThrows<ArgumentOutOfRangeException>(
                () => new RunFlowConfiguration(
                    100f,
                    10,
                    0.2f,
                    0.1f,
                    0.1f,
                    0.05f,
                    regularRoundCount: 0),
                "Zero regular rounds should be rejected.");
            AssertThrows<ArgumentOutOfRangeException>(
                () => new RunFlowConfiguration(
                    100f,
                    10,
                    0.2f,
                    0.1f,
                    0.1f,
                    0.05f,
                    regularRoundCount: 3,
                    startingRound: 5),
                "A starting round after the boss should be rejected.");
        }

        private static void ValidateAtomicThreatBatch()
        {
            RunFlowService service = CreateService();
            int changeCount = 0;
            service.StateChanged += _ => changeCount++;

            ExplorationKillBatchResult firstKill = service.TryRegisterExplorationKill(
                new ExplorationKillContribution(50f, 1));

            Assert(firstKill.Success, "Initial exploration kill was rejected.");
            AssertApproximately(firstKill.ThreatAdded, 50f, "Initial Threat contribution");
            Assert(service.State.Phase == RunPhase.Exploration, "Portal opened too early.");

            ExplorationKillContribution[] finalAttackBatch =
            {
                new ExplorationKillContribution(30f, 2),
                new ExplorationKillContribution(30f, 5)
            };

            ExplorationKillBatchResult thresholdResult =
                service.TryRegisterExplorationKillBatch(finalAttackBatch);

            Assert(thresholdResult.Success, "Final attack batch was rejected.");
            Assert(thresholdResult.PortalOpened, "Final attack batch did not open the portal.");
            Assert(thresholdResult.PreviousPhase == RunPhase.Exploration, "Unexpected batch start phase.");
            Assert(thresholdResult.CurrentPhase == RunPhase.PortalOpen, "Portal phase was not entered.");
            AssertApproximately(thresholdResult.ThreatAdded, 50f, "Clamped final Threat contribution");
            Assert(service.State.RiftInstability.Points == 0,
                "Kills from the Threat-filling attack batch leaked into Rift Instability.");
            Assert(changeCount == 2, "State change count mismatch for two accepted batches.");
        }

        private static void ValidateInstabilityProgression()
        {
            RunFlowService service = CreatePortalOpenService();
            List<ExplorationKillContribution> firstBatch = new List<ExplorationKillContribution>
            {
                new ExplorationKillContribution(10f, 6),
                new ExplorationKillContribution(10f, 5)
            };

            ExplorationKillBatchResult firstResult = service.TryRegisterExplorationKillBatch(firstBatch);

            Assert(firstResult.Success, "Post-portal kill batch was rejected.");
            Assert(firstResult.InstabilityPointsAdded == 11, "Unexpected first instability contribution.");
            AssertApproximately(firstResult.ThreatAdded, 0f, "Post-portal Threat contribution");
            AssertApproximately(service.State.CurrentThreat, 100f, "Threat changed after portal opened");
            Assert(service.State.RiftInstability.Level == 1, "Instability level should be one.");
            Assert(service.State.RiftInstability.PointsIntoCurrentLevel == 1,
                "Instability progress inside level mismatch.");
            Assert(service.State.RiftInstability.PointsToNextLevel == 9,
                "Instability points-to-next mismatch.");

            ExplorationKillBatchResult secondResult = service.TryRegisterExplorationKill(
                new ExplorationKillContribution(10f, 9));

            Assert(secondResult.Success, "Second post-portal kill was rejected.");
            Assert(service.State.RiftInstability.Points == 20, "Instability points should equal twenty.");
            Assert(service.State.RiftInstability.Level == 2, "Instability level should be two.");
        }

        private static void ValidateInvalidBatchIsAtomic()
        {
            RunFlowService service = CreateService();
            ExplorationKillContribution[] invalidBatch =
            {
                new ExplorationKillContribution(40f, 1),
                new ExplorationKillContribution(-1f, 1)
            };

            ExplorationKillBatchResult result = service.TryRegisterExplorationKillBatch(invalidBatch);

            Assert(!result.Success, "Invalid kill batch should fail.");
            Assert(result.Error == RunFlowError.InvalidKillContribution, "Unexpected invalid-batch error.");
            AssertApproximately(service.State.CurrentThreat, 0f, "Invalid batch mutated Threat");
            Assert(service.State.RiftInstability.Points == 0, "Invalid batch mutated Rift Instability.");

            ExplorationKillBatchResult emptyResult = service.TryRegisterExplorationKillBatch(
                Array.Empty<ExplorationKillContribution>());
            Assert(!emptyResult.Success && emptyResult.Error == RunFlowError.EmptyKillBatch,
                "Empty kill batch should be rejected explicitly.");
        }

        private static void ValidateAssaultLifecycle()
        {
            RunFlowService service = CreatePortalOpenService();
            Assert(service.TryRegisterExplorationKill(
                new ExplorationKillContribution(1f, 20)).Success,
                "Could not prepare instability for Assault snapshot.");

            RunFlowTransitionResult transitionResult = service.TryBeginAssaultTransition();

            Assert(transitionResult.Success, "Portal entry transition failed.");
            Assert(service.State.Phase == RunPhase.TransitionToAssault,
                "Run did not enter Assault transition phase.");
            Assert(service.State.AssaultScaling.InstabilityPoints == 20,
                "Assault snapshot lost instability points.");
            Assert(service.State.AssaultScaling.InstabilityLevel == 2,
                "Assault snapshot level mismatch.");
            AssertApproximately(service.State.AssaultScaling.HealthMultiplier, 1.20f,
                "Assault health multiplier");
            AssertApproximately(service.State.AssaultScaling.DamageMultiplier, 1.10f,
                "Assault damage multiplier");

            ExplorationKillBatchResult transitionKill = service.TryRegisterExplorationKill(
                new ExplorationKillContribution(10f, 10));
            Assert(!transitionKill.Success && transitionKill.Error == RunFlowError.InvalidPhase,
                "Exploration kill should not mutate a locked Assault transition.");

            Assert(service.TryStartAssault().Success, "Assault did not start.");
            Assert(service.TryCompleteAssault().Success, "Assault did not complete.");
            Assert(service.TryBeginReturnToExploration().Success, "Return transition did not begin.");
            Assert(service.TryResumeExploration().Success, "Exploration did not resume.");

            Assert(service.State.Phase == RunPhase.Exploration, "Run did not return to exploration.");
            Assert(service.State.RoundNumber == 2, "Round number did not advance.");
            AssertApproximately(service.State.RoundScaling.HealthMultiplier, 1.20f,
                "Round-two health multiplier");
            AssertApproximately(service.State.RoundScaling.DamageMultiplier, 1.10f,
                "Round-two damage multiplier");
            AssertApproximately(service.State.CurrentThreat, 0f, "Threat did not reset for next cycle");
            Assert(service.State.RiftInstability.Points == 0,
                "Rift Instability did not reset for next cycle.");
            AssertApproximately(service.State.AssaultScaling.HealthMultiplier, 1f,
                "Assault health snapshot did not reset");
            AssertApproximately(service.State.AssaultScaling.DamageMultiplier, 1f,
                "Assault damage snapshot did not reset");
            Assert(service.State.AssaultScaling.RoundNumber == 2,
                "Reset Assault snapshot retained a stale round identity.");
        }

        private static void ValidateTerminalState()
        {
            RunFlowService service = CreateService();
            RunFlowTransitionResult failed = service.TryFailRun();

            Assert(failed.Success, "Run failure transition was rejected.");
            Assert(service.State.Phase == RunPhase.Failed, "Run did not enter failed state.");

            RunFlowTransitionResult repeatedFailure = service.TryFailRun();
            Assert(!repeatedFailure.Success && repeatedFailure.Error == RunFlowError.TerminalState,
                "Terminal run accepted another failure transition.");

            ExplorationKillBatchResult terminalKill = service.TryRegisterExplorationKill(
                new ExplorationKillContribution(10f, 1));
            Assert(!terminalKill.Success && terminalKill.Error == RunFlowError.InvalidPhase,
                "Terminal run accepted an exploration kill.");
        }

        private static void ValidateFinalEncounterLifecycle()
        {
            RunFlowService service = CreateService();

            for (int round = 1; round <= 3; round++)
            {
                Assert(service.State.RoundNumber == round,
                    $"Expected regular round {round}.");
                Assert(service.State.CurrentEncounterKind == RunEncounterKind.AssaultWave,
                    $"Round {round} was not classified as a regular Assault wave.");

                CompleteCurrentEncounter(service);
                RunFlowTransitionResult earlyCompletion = service.TryCompleteRun();
                Assert(!earlyCompletion.Success &&
                       earlyCompletion.Error == RunFlowError.NotFinalEncounter,
                    $"Regular round {round} completed the run early.");
                Assert(service.TryBeginReturnToExploration().Success,
                    $"Regular round {round} could not begin its return.");
                Assert(service.TryResumeExploration().Success,
                    $"Regular round {round} could not resume exploration.");
            }

            Assert(service.State.RoundNumber == 4,
                "The run did not advance to round four.");
            Assert(service.State.FinalRoundNumber == 4 &&
                   service.State.CurrentEncounterKind == RunEncounterKind.Boss,
                "Round four was not classified as the final boss round.");
            AssertApproximately(service.State.RoundScaling.HealthMultiplier, 1.60f,
                "Boss-round health multiplier");
            AssertApproximately(service.State.RoundScaling.DamageMultiplier, 1.30f,
                "Boss-round damage multiplier");

            CompleteCurrentEncounter(service);
            RunFlowTransitionResult rejectedReturn =
                service.TryBeginReturnToExploration();
            Assert(!rejectedReturn.Success &&
                   rejectedReturn.Error == RunFlowError.FinalEncounterCompleted,
                "Final boss intermission allowed another exploration round.");

            RunFlowTransitionResult completion = service.TryCompleteRun();
            Assert(completion.Success && service.State.Phase == RunPhase.Completed,
                "Final boss intermission did not complete the run.");
        }

        private static void CompleteCurrentEncounter(RunFlowService service)
        {
            ExplorationKillBatchResult fill = service.TryRegisterExplorationKill(
                new ExplorationKillContribution(service.State.MaxThreat, 0));
            Assert(fill.Success && service.State.Phase == RunPhase.PortalOpen,
                "Could not fill the current round meter.");
            Assert(service.TryBeginAssaultTransition().Success,
                "Could not begin the current encounter transition.");
            Assert(service.TryStartAssault().Success,
                "Could not start the current encounter.");
            Assert(service.TryCompleteAssault().Success,
                "Could not complete the current encounter.");
        }

        private static RunFlowService CreateService()
        {
            return new RunFlowService(RunFlowConfiguration.CreateVerticalSliceDefaults());
        }

        private static RunFlowService CreatePortalOpenService()
        {
            RunFlowService service = CreateService();
            ExplorationKillBatchResult result = service.TryRegisterExplorationKill(
                new ExplorationKillContribution(100f, 1));

            Assert(result.Success && result.PortalOpened, "Could not prepare portal-open state.");
            return service;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static void AssertApproximately(float actual, float expected, string label)
        {
            if (Math.Abs(actual - expected) <= 0.0001f)
                return;

            throw new InvalidOperationException($"{label} failed. Expected {expected}, got {actual}.");
        }

        private static void AssertThrows<TException>(Action action, string message)
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
    }
}
