using System;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Run.Editor
{
    public static class RunChapterProgressApplicationValidationRunner
    {
        [MenuItem("Tools/Titanhold/Validate Run Chapter Progress Application")]
        public static void Validate()
        {
            try
            {
                ValidateAtomicApplication();
                ValidateReplayProtection();
                ValidateRejectedBatches();
                ValidatePhaseRejectionCannotReplay();
                Debug.Log("Run Chapter Progress Application validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Run Chapter Progress Application validation failed: " +
                    $"{exception}");
            }
        }

        private static void ValidateAtomicApplication()
        {
            Fixture fixture = CreateFixture();
            RunChapterProgressCommand command = CreateCommand(
                "combat:atomic",
                "player:one",
                ("enemy:one", 40f),
                ("enemy:two", 70f));

            RunChapterProgressApplicationResult result =
                fixture.Application.TryApply(command, 10d);

            Assert(result.Success, "Valid progress batch was rejected.");
            Assert(result.AcceptedContributionCount == 2,
                "Accepted contribution count is incorrect.");
            AssertApproximately(
                fixture.Flow.State.CurrentProgress,
                100f,
                "Clamped chapter progress");
            Assert(result.ChapterFlowResult.CollapseStarted,
                "Full progress did not start Rift Collapse.");
            Assert(fixture.Flow.State.Phase == RunChapterPhase.RiftCollapse,
                "Chapter did not enter Rift Collapse.");
            Assert(fixture.Application.ProcessedEventCount == 1,
                "Processed event count is incorrect.");
        }

        private static void ValidateReplayProtection()
        {
            Fixture fixture = CreateFixture();
            RunChapterProgressCommand command = CreateCommand(
                "activity:replay",
                "player:one",
                ("activity:shrine:one", 25f));

            Assert(fixture.Application.TryApply(command, 1d).Success,
                "Initial progress event was rejected.");
            RunChapterProgressApplicationResult replay =
                fixture.Application.TryApply(command, 2d);

            Assert(!replay.Success &&
                   replay.Error ==
                   RunChapterProgressApplicationError.DuplicateEvent,
                "Repeated progress event was not rejected.");
            AssertApproximately(
                fixture.Flow.State.CurrentProgress,
                25f,
                "Progress after replay");
        }

        private static void ValidateRejectedBatches()
        {
            Fixture fixture = CreateFixture();

            AssertRejected(
                fixture.Application.TryApply(null, 0d),
                RunChapterProgressApplicationError.MissingEventId,
                "Null command");
            AssertRejected(
                fixture.Application.TryApply(
                    CreateCommand(string.Empty, "player:one", ("enemy:one", 1f)),
                    0d),
                RunChapterProgressApplicationError.MissingEventId,
                "Missing event id");
            AssertRejected(
                fixture.Application.TryApply(
                    CreateCommand("combat:no-player", string.Empty, ("enemy:one", 1f)),
                    0d),
                RunChapterProgressApplicationError.MissingParticipantId,
                "Missing participant id");
            AssertRejected(
                fixture.Application.TryApply(
                    CreateCommand("combat:empty", "player:one"),
                    0d),
                RunChapterProgressApplicationError.EmptyBatch,
                "Empty batch");
            AssertRejected(
                fixture.Application.TryApply(
                    CreateCommand("combat:no-source", "player:one", (string.Empty, 1f)),
                    0d),
                RunChapterProgressApplicationError.MissingSourceId,
                "Missing source id");
            AssertRejected(
                fixture.Application.TryApply(
                    CreateCommand("combat:amount", "player:one", ("enemy:one", 0f)),
                    0d),
                RunChapterProgressApplicationError.InvalidAmount,
                "Invalid amount");
            AssertRejected(
                fixture.Application.TryApply(
                    CreateCommand(
                        "combat:duplicate-source",
                        "player:one",
                        ("enemy:one", 1f),
                        ("enemy:one", 2f)),
                    0d),
                RunChapterProgressApplicationError.DuplicateSource,
                "Duplicate source");

            AssertApproximately(
                fixture.Flow.State.CurrentProgress,
                0f,
                "Progress after rejected batches");
            Assert(fixture.Application.ProcessedEventCount == 0,
                "Invalid batches were marked as processed.");
        }

        private static void ValidatePhaseRejectionCannotReplay()
        {
            Fixture fixture = CreateFixture();
            Assert(fixture.Flow.TryAddProgress(100f, 1d).Success,
                "Could not prepare Rift Collapse.");

            RunChapterProgressCommand command = CreateCommand(
                "combat:late",
                "player:one",
                ("enemy:late", 10f));
            RunChapterProgressApplicationResult rejected =
                fixture.Application.TryApply(command, 2d);

            AssertRejected(
                rejected,
                RunChapterProgressApplicationError.ChapterFlowRejected,
                "Wrong-phase event");
            Assert(rejected.ChapterFlowResult.Error ==
                   RunChapterFlowError.InvalidPhase,
                "Chapter-flow rejection reason was lost.");
            Assert(fixture.Application.ProcessedEventCount == 1,
                "Attempted event was not retained for replay protection.");

            RunChapterProgressApplicationResult replay =
                fixture.Application.TryApply(command, 3d);
            AssertRejected(
                replay,
                RunChapterProgressApplicationError.DuplicateEvent,
                "Wrong-phase replay");
        }

        private static RunChapterProgressCommand CreateCommand(
            string eventId,
            string participantId,
            params (string sourceId, float amount)[] entries)
        {
            RunChapterProgressContribution[] contributions =
                new RunChapterProgressContribution[entries.Length];
            for (int i = 0; i < entries.Length; i++)
            {
                contributions[i] = new RunChapterProgressContribution(
                    entries[i].sourceId,
                    entries[i].amount);
            }

            return new RunChapterProgressCommand(
                eventId,
                participantId,
                contributions);
        }

        private static Fixture CreateFixture()
        {
            RunChapterFlowService flow = new RunChapterFlowService(
                RunChapterFlowConfiguration.CreatePrototypeDefaults());
            return new Fixture(
                flow,
                new RunChapterProgressApplicationService(flow));
        }

        private static void AssertRejected(
            RunChapterProgressApplicationResult result,
            RunChapterProgressApplicationError expected,
            string label)
        {
            Assert(!result.Success && result.Error == expected,
                $"{label} returned {result.Error} instead of {expected}.");
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

        private readonly struct Fixture
        {
            public Fixture(
                RunChapterFlowService flow,
                RunChapterProgressApplicationService application)
            {
                Flow = flow;
                Application = application;
            }

            public RunChapterFlowService Flow { get; }
            public RunChapterProgressApplicationService Application { get; }
        }
    }
}
