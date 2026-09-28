using System;
using System.Collections.Generic;

namespace Titanhold.Run
{
    public sealed class RunChapterBossTransitionApplicationService
    {
        private readonly string runId;
        private readonly RunChapterFlowService chapterFlow;
        private readonly IRunChapterTransitionParticipantRoster participants;
        private readonly HashSet<string> processedEventIds =
            new HashSet<string>(StringComparer.Ordinal);

        private RunChapterBossTransitionRequest transitionRequest;

        public RunChapterBossTransitionApplicationService(
            string runId,
            RunChapterFlowService chapterFlow,
            IRunChapterTransitionParticipantRoster participants)
        {
            this.runId = runId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(this.runId))
                throw new ArgumentException("Run id is required.", nameof(runId));

            this.chapterFlow = chapterFlow ??
                throw new ArgumentNullException(nameof(chapterFlow));
            this.participants = participants ??
                throw new ArgumentNullException(nameof(participants));
        }

        public int ProcessedEventCount => processedEventIds.Count;
        public bool HasTransitionRequest => transitionRequest.IsValid;
        public RunChapterBossTransitionRequest TransitionRequest =>
            transitionRequest;

        public event Action<RunChapterBossTransitionRequest>
            TransitionRequested;

        public RunChapterBossTransitionApplicationResult TryEnterPortal(
            RunChapterBossPortalEntryCommand command)
        {
            RunChapterBossTransitionApplicationError validationError =
                ValidateManualCommand(command);
            if (validationError !=
                RunChapterBossTransitionApplicationError.None)
            {
                return RunChapterBossTransitionApplicationResult.Failed(
                    validationError);
            }

            processedEventIds.Add(command.EventId);
            RunChapterFlowResult flowResult =
                chapterFlow.TryEnterBossPortal(command.SimulationTime);
            if (!flowResult.Success)
            {
                return RunChapterBossTransitionApplicationResult.Failed(
                    RunChapterBossTransitionApplicationError.ChapterFlowRejected,
                    flowResult);
            }

            RunChapterBossTransitionRequest request = CreateRequest(
                command.EventId,
                command.ParticipantId,
                command.SimulationTime,
                flowResult.ForcedBossTransition);
            if (!TryPublish(request))
            {
                return RunChapterBossTransitionApplicationResult.Failed(
                    RunChapterBossTransitionApplicationError.
                        TransitionAlreadyPublished,
                    flowResult);
            }

            return RunChapterBossTransitionApplicationResult.Succeeded(
                flowResult,
                request);
        }

        public RunChapterBossTransitionApplicationResult TryAdvanceTime(
            double simulationTime)
        {
            RunChapterFlowResult flowResult =
                chapterFlow.TryAdvanceTime(simulationTime);
            if (!flowResult.Success)
            {
                return RunChapterBossTransitionApplicationResult.Failed(
                    RunChapterBossTransitionApplicationError.ChapterFlowRejected,
                    flowResult);
            }

            if (!flowResult.ForcedBossTransition)
            {
                return RunChapterBossTransitionApplicationResult.Succeeded(
                    flowResult);
            }

            RunChapterBossTransitionRequest request = CreateRequest(
                eventId: CreateForcedEventId(),
                participantId: string.Empty,
                simulationTime: simulationTime,
                forced: true);
            if (!TryPublish(request))
            {
                return RunChapterBossTransitionApplicationResult.Failed(
                    RunChapterBossTransitionApplicationError.
                        TransitionAlreadyPublished,
                    flowResult);
            }

            return RunChapterBossTransitionApplicationResult.Succeeded(
                flowResult,
                request);
        }

        private RunChapterBossTransitionApplicationError ValidateManualCommand(
            RunChapterBossPortalEntryCommand command)
        {
            if (string.IsNullOrWhiteSpace(command.EventId))
            {
                return RunChapterBossTransitionApplicationError.MissingEventId;
            }

            if (string.IsNullOrWhiteSpace(command.ParticipantId))
            {
                return RunChapterBossTransitionApplicationError.
                    MissingParticipantId;
            }

            if (command.ExpectedChapterNumber <= 0)
            {
                return RunChapterBossTransitionApplicationError.
                    InvalidExpectedChapter;
            }

            if (command.ExpectedChapterNumber !=
                chapterFlow.State.ChapterNumber)
            {
                return RunChapterBossTransitionApplicationError.StaleChapter;
            }

            if (processedEventIds.Contains(command.EventId))
            {
                return RunChapterBossTransitionApplicationError.DuplicateEvent;
            }

            if (!participants.Contains(command.ParticipantId))
            {
                return RunChapterBossTransitionApplicationError.
                    ParticipantNotRegistered;
            }

            if (chapterFlow.State.Phase != RunChapterPhase.RiftCollapse)
            {
                return RunChapterBossTransitionApplicationError.
                    PortalUnavailable;
            }

            if (HasTransitionRequest)
            {
                return RunChapterBossTransitionApplicationError.
                    TransitionAlreadyPublished;
            }

            return RunChapterBossTransitionApplicationError.None;
        }

        private RunChapterBossTransitionRequest CreateRequest(
            string eventId,
            string participantId,
            double simulationTime,
            bool forced)
        {
            return new RunChapterBossTransitionRequest(
                runId,
                forced ? CreateForcedEventId() : eventId,
                chapterFlow.State.ChapterNumber,
                forced ? string.Empty : participantId,
                forced
                    ? RunChapterBossTransitionCause.CollapseExpired
                    : RunChapterBossTransitionCause.ManualPortal,
                simulationTime,
                chapterFlow.State.BossScaling);
        }

        private bool TryPublish(RunChapterBossTransitionRequest request)
        {
            if (HasTransitionRequest)
                return false;

            transitionRequest = request;
            TransitionRequested?.Invoke(request);
            return true;
        }

        private string CreateForcedEventId()
        {
            return $"transition:{runId}:chapter:" +
                   $"{chapterFlow.State.ChapterNumber}:collapse-expired";
        }
    }
}
