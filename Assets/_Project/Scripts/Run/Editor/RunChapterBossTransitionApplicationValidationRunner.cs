using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Run.Editor
{
    public static class RunChapterBossTransitionApplicationValidationRunner
    {
        [MenuItem(
            "Tools/Titanhold/Validate Run Chapter Boss Transition Application")]
        public static void Validate()
        {
            ValidateManualTransition();
            ValidateForcedTransition();
            ValidateDeadlineRaceUsesForcedRequest();
            Debug.Log(
                "Run Chapter Boss Transition Application validation passed.");
        }

        private static void ValidateManualTransition()
        {
            RunChapterFlowService flow = CreateFlow();
            TestParticipantRoster roster = new("participant:one");
            RunChapterBossTransitionApplicationService application = new(
                "run:manual",
                flow,
                roster);

            RunChapterBossPortalEntryCommand early = new(
                "event:manual",
                "participant:one",
                expectedChapterNumber: 1,
                simulationTime: 1d);
            RunChapterBossTransitionApplicationResult unavailable =
                application.TryEnterPortal(early);
            Assert(!unavailable.Success &&
                   unavailable.Error ==
                       RunChapterBossTransitionApplicationError.
                           PortalUnavailable &&
                   application.ProcessedEventCount == 0,
                "An unavailable portal consumed the manual event.");

            Assert(flow.TryAddProgress(100f, 10d).Success,
                "Could not begin Rift Collapse for manual validation.");

            RunChapterBossPortalEntryCommand unregistered = new(
                "event:unregistered",
                "participant:other",
                expectedChapterNumber: 1,
                simulationTime: 20d);
            RunChapterBossTransitionApplicationResult rejected =
                application.TryEnterPortal(unregistered);
            Assert(!rejected.Success &&
                   rejected.Error ==
                       RunChapterBossTransitionApplicationError.
                           ParticipantNotRegistered &&
                   flow.State.Phase == RunChapterPhase.RiftCollapse,
                "An unregistered participant entered the boss portal.");

            int publishedCount = 0;
            RunChapterBossTransitionRequest observed = default;
            application.TransitionRequested += request =>
            {
                publishedCount++;
                observed = request;
            };

            RunChapterBossPortalEntryCommand accepted = new(
                "event:manual",
                "participant:one",
                expectedChapterNumber: 1,
                simulationTime: 51d);
            RunChapterBossTransitionApplicationResult result =
                application.TryEnterPortal(accepted);
            Assert(result.Success && result.HasTransitionRequest,
                "A valid manual portal command did not create a request.");
            Assert(flow.State.Phase == RunChapterPhase.TransitionToBoss &&
                   !flow.State.BossTransitionWasForced,
                "Manual entry did not use the manual transition path.");
            Assert(application.ProcessedEventCount == 1 &&
                   application.HasTransitionRequest &&
                   publishedCount == 1,
                "Manual transition publication was not exactly once.");

            RunChapterBossTransitionRequest request =
                result.TransitionRequest;
            Assert(request.IsValid &&
                   request.RunId == "run:manual" &&
                   request.EventId == "event:manual" &&
                   request.ChapterNumber == 1 &&
                   request.InitiatingParticipantId == "participant:one" &&
                   request.Cause ==
                       RunChapterBossTransitionCause.ManualPortal &&
                   !request.WasForced,
                "Manual transition request identity is incorrect.");
            Assert(request.BossScaling.InstabilityStacks == 2,
                "Manual request did not freeze current instability.");
            AssertApproximately(
                request.BossScaling.HealthMultiplier,
                1.2f,
                "Manual boss health multiplier");
            AssertApproximately(
                request.BossScaling.DamageMultiplier,
                1.1f,
                "Manual boss damage multiplier");
            Assert(observed.EventId == request.EventId,
                "Published request differs from the stored result.");

            RunChapterBossTransitionApplicationResult replay =
                application.TryEnterPortal(accepted);
            Assert(!replay.Success &&
                   replay.Error ==
                       RunChapterBossTransitionApplicationError.DuplicateEvent &&
                   publishedCount == 1,
                "A replayed manual command was not rejected.");
        }

        private static void ValidateForcedTransition()
        {
            RunChapterFlowService flow = CreateFlow();
            RunChapterBossTransitionApplicationService application = new(
                "run:forced",
                flow,
                new TestParticipantRoster("participant:one"));
            Assert(flow.TryAddProgress(100f, 50d).Success,
                "Could not begin Rift Collapse for forced validation.");

            int publishedCount = 0;
            application.TransitionRequested += _ => publishedCount++;
            RunChapterBossTransitionApplicationResult beforeDeadline =
                application.TryAdvanceTime(169d);
            Assert(beforeDeadline.Success &&
                   !beforeDeadline.HasTransitionRequest &&
                   publishedCount == 0,
                "A forced request appeared before the deadline.");

            RunChapterBossTransitionApplicationResult result =
                application.TryAdvanceTime(170d);
            Assert(result.Success &&
                   result.HasTransitionRequest &&
                   application.HasTransitionRequest &&
                   publishedCount == 1,
                "Collapse expiry did not publish a transition request.");

            RunChapterBossTransitionRequest request =
                result.TransitionRequest;
            Assert(request.WasForced &&
                   request.Cause ==
                       RunChapterBossTransitionCause.CollapseExpired &&
                   request.RunId == "run:forced" &&
                   request.EventId ==
                       "transition:run:forced:chapter:1:collapse-expired" &&
                   string.IsNullOrEmpty(request.InitiatingParticipantId),
                "Forced transition identity is incorrect.");
            Assert(request.BossScaling.InstabilityStacks == 6,
                "Forced request did not freeze maximum instability.");
            AssertApproximately(
                request.BossScaling.HealthMultiplier,
                1.6f,
                "Forced boss health multiplier");
            AssertApproximately(
                request.BossScaling.DamageMultiplier,
                1.3f,
                "Forced boss damage multiplier");

            RunChapterBossTransitionApplicationResult repeatedTick =
                application.TryAdvanceTime(171d);
            Assert(repeatedTick.Success &&
                   !repeatedTick.HasTransitionRequest &&
                   publishedCount == 1,
                "A later tick republished the forced transition.");
        }

        private static void ValidateDeadlineRaceUsesForcedRequest()
        {
            RunChapterFlowService flow = CreateFlow();
            RunChapterBossTransitionApplicationService application = new(
                "run:deadline",
                flow,
                new TestParticipantRoster("participant:one"));
            Assert(flow.TryAddProgress(100f, 10d).Success,
                "Could not begin Rift Collapse for deadline validation.");

            RunChapterBossPortalEntryCommand command = new(
                "event:deadline-click",
                "participant:one",
                expectedChapterNumber: 1,
                simulationTime: 130d);
            RunChapterBossTransitionApplicationResult result =
                application.TryEnterPortal(command);
            Assert(result.Success &&
                   result.TransitionRequest.WasForced &&
                   result.TransitionRequest.EventId ==
                       "transition:run:deadline:chapter:1:collapse-expired" &&
                   string.IsNullOrEmpty(
                       result.TransitionRequest.InitiatingParticipantId),
                "A deadline portal race did not resolve as forced transition.");
        }

        private static RunChapterFlowService CreateFlow()
        {
            return new RunChapterFlowService(
                RunChapterFlowConfiguration.CreatePrototypeDefaults());
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

        private sealed class TestParticipantRoster :
            IRunChapterTransitionParticipantRoster
        {
            private readonly HashSet<string> participantIds;

            public TestParticipantRoster(params string[] participantIds)
            {
                this.participantIds = new HashSet<string>(
                    participantIds,
                    StringComparer.Ordinal);
            }

            public bool Contains(string participantId)
            {
                return participantIds.Contains(participantId);
            }
        }
    }
}
