using System;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Run.Editor
{
    public static class RunChapterFlowValidationRunner
    {
        [MenuItem("Tools/Titanhold/Validate Run Chapter Flow Foundation")]
        public static void Validate()
        {
            ValidateConfiguration();
            ValidateExplorationStages();
            ValidateManualBossTransition();
            ValidateForcedBossTransition();
            Debug.Log("Run Chapter Flow Foundation validation passed.");
        }

        private static void ValidateConfiguration()
        {
            RunChapterFlowConfiguration configuration =
                RunChapterFlowConfiguration.CreatePrototypeDefaults();
            Assert(configuration.ChapterNumber == 1,
                "Prototype chapter identity is incorrect.");
            Assert(Mathf.Approximately(configuration.MaximumProgress, 100f),
                "Prototype progress target is incorrect.");
            Assert(configuration.EscalationThresholds.Count == 4,
                "Prototype escalation stage count is incorrect.");
            Assert(configuration.MaximumInstabilityStacks == 6,
                "Prototype collapse stack count is incorrect.");

            bool rejectedInvalidThresholds = false;
            try
            {
                _ = new RunChapterFlowConfiguration(
                    1,
                    100f,
                    new[] { 0f, 0.6f, 0.3f },
                    120d,
                    20d,
                    0.1f,
                    0.05f);
            }
            catch (ArgumentException)
            {
                rejectedInvalidThresholds = true;
            }

            Assert(rejectedInvalidThresholds,
                "Unordered escalation thresholds were accepted.");
        }

        private static void ValidateExplorationStages()
        {
            RunChapterFlowService service = CreateService();
            Assert(service.State.Phase == RunChapterPhase.Exploration,
                "A chapter did not begin in exploration.");
            Assert(service.State.EscalationStage == 1,
                "A chapter did not begin in escalation stage one.");

            RunChapterFlowResult first =
                service.TryAddProgress(30f, 10d);
            Assert(first.Success &&
                   first.EscalationStageChanged &&
                   service.State.EscalationStage == 2,
                "Thirty percent progress did not enter stage two.");

            RunChapterFlowResult second =
                service.TryAddProgress(55f, 20d);
            Assert(second.Success &&
                   service.State.EscalationStage == 4 &&
                   service.State.Phase == RunChapterPhase.Exploration,
                "Eighty-five percent progress did not enter stage four.");

            RunChapterFlowResult fill =
                service.TryAddProgress(100f, 30d);
            Assert(fill.Success &&
                   fill.CollapseStarted &&
                   Mathf.Approximately(fill.ProgressAdded, 15f) &&
                   service.State.Phase == RunChapterPhase.RiftCollapse &&
                   service.State.IsProgressFull,
                "Full progress did not begin Rift Collapse atomically.");

            RunChapterFlowResult rejected =
                service.TryAddProgress(1f, 31d);
            Assert(!rejected.Success &&
                   rejected.Error == RunChapterFlowError.InvalidPhase,
                "Progress was accepted after Rift Collapse began.");
        }

        private static void ValidateManualBossTransition()
        {
            RunChapterFlowService service = CreateCollapsedService(100d);
            RunChapterFlowResult advanced =
                service.TryAdvanceTime(141d);
            Assert(advanced.Success &&
                   service.State.InstabilityStacks == 2 &&
                   Mathf.Approximately(
                       service.State.BossScaling.HealthMultiplier,
                       1.2f) &&
                   Mathf.Approximately(
                       service.State.BossScaling.DamageMultiplier,
                       1.1f),
                "Rift Collapse did not resolve deterministic boss scaling.");

            RunChapterFlowResult entered =
                service.TryEnterBossPortal(150d);
            Assert(entered.Success &&
                   !entered.ForcedBossTransition &&
                   service.State.Phase == RunChapterPhase.TransitionToBoss &&
                   service.State.InstabilityStacks == 2 &&
                   !service.State.BossTransitionWasForced,
                "Manual portal entry did not lock the current scaling.");

            Assert(service.TryStartBoss().Success &&
                   service.State.Phase == RunChapterPhase.Boss,
                "The chapter boss did not start.");
            Assert(service.TryDefeatBoss().Success &&
                   service.State.Phase == RunChapterPhase.Reward,
                "Boss defeat did not enter reward phase.");
            Assert(service.TryCompleteChapter().Success &&
                   service.State.Phase == RunChapterPhase.Completed,
                "Reward confirmation did not complete the chapter.");
        }

        private static void ValidateForcedBossTransition()
        {
            RunChapterFlowService service = CreateCollapsedService(50d);
            RunChapterFlowResult forced =
                service.TryAdvanceTime(170d);
            Assert(forced.Success &&
                   forced.ForcedBossTransition &&
                   service.State.Phase == RunChapterPhase.TransitionToBoss &&
                   service.State.BossTransitionWasForced &&
                   service.State.InstabilityStacks == 6 &&
                   Mathf.Approximately(
                       service.State.BossScaling.HealthMultiplier,
                       1.6f) &&
                   Mathf.Approximately(
                       service.State.BossScaling.DamageMultiplier,
                       1.3f),
                "Collapse deadline did not force the maximum authored scaling.");

            RunChapterFlowResult backwards =
                service.TryAdvanceTime(169d);
            Assert(!backwards.Success &&
                   backwards.Error ==
                       RunChapterFlowError.SimulationTimeMovedBackwards,
                "The chapter accepted simulation time moving backwards.");
        }

        private static RunChapterFlowService CreateService()
        {
            return new RunChapterFlowService(
                RunChapterFlowConfiguration.CreatePrototypeDefaults());
        }

        private static RunChapterFlowService CreateCollapsedService(
            double collapseStartTime)
        {
            RunChapterFlowService service = CreateService();
            RunChapterFlowResult fill =
                service.TryAddProgress(100f, collapseStartTime);
            Assert(fill.Success && fill.CollapseStarted,
                "Could not prepare a collapsed chapter.");
            return service;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
