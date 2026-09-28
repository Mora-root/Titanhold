using System;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Run.Editor
{
    public static class RunChapterFlowPresentationValidationRunner
    {
        [MenuItem("Tools/Titanhold/Validate Run Chapter Presentation")]
        public static void Validate()
        {
            ValidateManualFlowAndReadOnlyProjection();
            ValidateForcedTransition();
            Debug.Log("Run Chapter Presentation validation passed.");
        }

        private static void ValidateManualFlowAndReadOnlyProjection()
        {
            RunChapterFlowService service = CreateService();
            RunChapterFlowPresentationProjection projection =
                new RunChapterFlowPresentationProjection(service.State);
            int stateChangedCount = 0;
            service.StateChanged += _ => stateChangedCount++;

            RunChapterFlowPresentationSnapshot initial =
                projection.Capture();
            Assert(initial.ChapterNumber == 1,
                "The presentation has the wrong chapter number.");
            AssertApproximately(initial.CurrentProgress, 0f,
                "Initial progress");
            AssertApproximately(initial.MaximumProgress, 100f,
                "Maximum progress");
            AssertApproximately(initial.NormalizedProgress, 0f,
                "Initial normalized progress");
            Assert(initial.CurrentEscalationStage == 1 &&
                   initial.EscalationStageCount == 4,
                "The initial escalation stage is incorrect.");
            AssertApproximately(
                (float)initial.CollapseDurationSeconds,
                120f,
                "Collapse duration");
            AssertApproximately(
                (float)initial.CollapseTimeRemainingSeconds,
                0f,
                "Initial collapse time remaining");
            Assert(initial.CurrentInstabilityStacks == 0 &&
                   initial.MaximumInstabilityStacks == 6,
                "The initial instability bounds are incorrect.");
            AssertApproximately(initial.BossHealthMultiplier, 1f,
                "Initial boss health multiplier");
            AssertApproximately(initial.BossDamageMultiplier, 1f,
                "Initial boss damage multiplier");
            AssertSemanticFlags(
                initial,
                RunChapterPhase.Exploration,
                portalAvailable: false,
                forcedTransition: false,
                "initial exploration");

            Assert(service.TryAddProgress(30f, 10d).Success,
                "Could not advance to escalation stage two.");
            RunChapterFlowPresentationSnapshot stageTwo =
                projection.Capture();
            AssertApproximately(stageTwo.CurrentProgress, 30f,
                "Stage-two progress");
            AssertApproximately(stageTwo.NormalizedProgress, 0.3f,
                "Stage-two normalized progress");
            Assert(stageTwo.CurrentEscalationStage == 2,
                "The presentation did not expose escalation stage two.");
            AssertSemanticFlags(
                stageTwo,
                RunChapterPhase.Exploration,
                portalAvailable: false,
                forcedTransition: false,
                "stage-two exploration");

            Assert(service.TryAddProgress(30f, 15d).Success,
                "Could not advance to escalation stage three.");
            RunChapterFlowPresentationSnapshot stageThree =
                projection.Capture();
            AssertApproximately(stageThree.NormalizedProgress, 0.6f,
                "Stage-three normalized progress");
            Assert(stageThree.CurrentEscalationStage == 3,
                "The presentation did not expose escalation stage three.");

            Assert(service.TryAddProgress(25f, 18d).Success,
                "Could not advance to escalation stage four.");
            RunChapterFlowPresentationSnapshot stageFour =
                projection.Capture();
            AssertApproximately(stageFour.NormalizedProgress, 0.85f,
                "Stage-four normalized progress");
            Assert(stageFour.CurrentEscalationStage == 4,
                "The presentation did not expose escalation stage four.");

            Assert(service.TryAddProgress(15f, 20d).Success,
                "Could not fill chapter progress.");
            RunChapterFlowPresentationSnapshot collapse =
                projection.Capture();
            AssertApproximately(collapse.CurrentProgress, 100f,
                "Full progress");
            AssertApproximately(collapse.NormalizedProgress, 1f,
                "Full normalized progress");
            Assert(collapse.CurrentEscalationStage == 4,
                "Full progress did not expose the final escalation stage.");
            AssertApproximately(
                (float)collapse.CollapseTimeRemainingSeconds,
                120f,
                "Initial collapse countdown");
            AssertSemanticFlags(
                collapse,
                RunChapterPhase.RiftCollapse,
                portalAvailable: true,
                forcedTransition: false,
                "Rift Collapse");

            double timeBeforeReads = service.State.LastSimulationTime;
            int eventsBeforeReads = stateChangedCount;
            RunChapterFlowPresentationSnapshot repeatedRead =
                projection.Capture();
            _ = projection.Capture();
            Assert(service.State.LastSimulationTime == timeBeforeReads &&
                   service.State.Phase == RunChapterPhase.RiftCollapse &&
                   service.State.InstabilityStacks == 0 &&
                   stateChangedCount == eventsBeforeReads,
                "Reading presentation mutated chapter state or emitted an event.");
            AssertApproximately(
                (float)repeatedRead.CollapseTimeRemainingSeconds,
                120f,
                "Countdown changed without an explicit domain tick");

            RunChapterFlowResult tick = service.TryAdvanceTime(61d);
            Assert(tick.Success,
                "The explicit domain tick was rejected.");
            RunChapterFlowPresentationSnapshot advanced =
                projection.Capture();
            AssertApproximately(
                (float)advanced.CollapseTimeRemainingSeconds,
                79f,
                "Advanced collapse countdown");
            Assert(advanced.CurrentInstabilityStacks == 2,
                "The presentation did not expose current instability stacks.");
            AssertApproximately(advanced.BossHealthMultiplier, 1.2f,
                "Instability boss health multiplier");
            AssertApproximately(advanced.BossDamageMultiplier, 1.1f,
                "Instability boss damage multiplier");
            AssertSemanticFlags(
                advanced,
                RunChapterPhase.RiftCollapse,
                portalAvailable: true,
                forcedTransition: false,
                "advanced Rift Collapse");

            Assert(service.TryEnterBossPortal(70d).Success,
                "Manual boss portal entry was rejected.");
            RunChapterFlowPresentationSnapshot transition =
                projection.Capture();
            Assert(transition.CurrentInstabilityStacks == 2,
                "Manual transition did not retain its scaling snapshot.");
            AssertApproximately(transition.BossHealthMultiplier, 1.2f,
                "Manual transition boss health multiplier");
            AssertApproximately(transition.BossDamageMultiplier, 1.1f,
                "Manual transition boss damage multiplier");
            AssertApproximately(
                (float)transition.CollapseTimeRemainingSeconds,
                0f,
                "Manual transition collapse time remaining");
            AssertSemanticFlags(
                transition,
                RunChapterPhase.TransitionToBoss,
                portalAvailable: false,
                forcedTransition: false,
                "manual boss transition");

            Assert(service.TryStartBoss().Success,
                "Could not start the boss phase.");
            AssertSemanticFlags(
                projection.Capture(),
                RunChapterPhase.Boss,
                portalAvailable: false,
                forcedTransition: false,
                "boss phase");
            Assert(service.TryDefeatBoss().Success,
                "Could not enter the reward phase.");
            AssertSemanticFlags(
                projection.Capture(),
                RunChapterPhase.Reward,
                portalAvailable: false,
                forcedTransition: false,
                "reward phase");
            Assert(service.TryCompleteChapter().Success,
                "Could not complete the chapter.");
            AssertSemanticFlags(
                projection.Capture(),
                RunChapterPhase.Completed,
                portalAvailable: false,
                forcedTransition: false,
                "completed phase");
        }

        private static void ValidateForcedTransition()
        {
            RunChapterFlowService service = CreateService();
            RunChapterFlowPresentationProjection projection =
                new RunChapterFlowPresentationProjection(service.State);
            Assert(service.TryAddProgress(100f, 50d).Success,
                "Could not prepare forced-transition validation.");
            Assert(service.TryAdvanceTime(170d).Success,
                "The deadline domain tick was rejected.");

            RunChapterFlowPresentationSnapshot forced =
                projection.Capture();
            Assert(forced.CurrentInstabilityStacks == 6 &&
                   forced.MaximumInstabilityStacks == 6,
                "Forced transition did not expose maximum instability.");
            AssertApproximately(forced.BossHealthMultiplier, 1.6f,
                "Forced boss health multiplier");
            AssertApproximately(forced.BossDamageMultiplier, 1.3f,
                "Forced boss damage multiplier");
            AssertApproximately(
                (float)forced.CollapseTimeRemainingSeconds,
                0f,
                "Forced transition collapse time remaining");
            AssertSemanticFlags(
                forced,
                RunChapterPhase.TransitionToBoss,
                portalAvailable: false,
                forcedTransition: true,
                "forced boss transition");

            Assert(service.TryStartBoss().Success,
                "Could not start the forced boss phase.");
            AssertSemanticFlags(
                projection.Capture(),
                RunChapterPhase.Boss,
                portalAvailable: false,
                forcedTransition: true,
                "forced boss phase");
        }

        private static RunChapterFlowService CreateService()
        {
            return new RunChapterFlowService(
                RunChapterFlowConfiguration.CreatePrototypeDefaults());
        }

        private static void AssertSemanticFlags(
            RunChapterFlowPresentationSnapshot snapshot,
            RunChapterPhase phase,
            bool portalAvailable,
            bool forcedTransition,
            string context)
        {
            Assert(snapshot.Phase == phase,
                $"The {context} presentation has the wrong phase.");
            Assert(snapshot.IsBossPortalAvailable == portalAvailable,
                $"The {context} portal availability is incorrect.");
            Assert(snapshot.BossTransitionWasForced == forcedTransition,
                $"The {context} forced-transition flag is incorrect.");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static void AssertApproximately(
            float actual,
            float expected,
            string label)
        {
            if (Math.Abs(actual - expected) <= 0.0001f)
                return;

            throw new InvalidOperationException(
                $"{label} failed. Expected {expected}, got {actual}.");
        }
    }
}
