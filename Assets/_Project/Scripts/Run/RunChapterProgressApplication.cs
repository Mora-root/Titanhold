using System;
using System.Collections.Generic;

namespace Titanhold.Run
{
    public readonly struct RunChapterProgressContribution
    {
        public RunChapterProgressContribution(string sourceId, float amount)
        {
            SourceId = sourceId ?? string.Empty;
            Amount = amount;
        }

        public string SourceId { get; }
        public float Amount { get; }
    }

    public sealed class RunChapterProgressCommand
    {
        private readonly RunChapterProgressContribution[] contributions;

        public RunChapterProgressCommand(
            string eventId,
            string participantId,
            IReadOnlyList<RunChapterProgressContribution> contributions)
        {
            EventId = eventId ?? string.Empty;
            ParticipantId = participantId ?? string.Empty;

            if (contributions == null)
            {
                this.contributions = Array.Empty<RunChapterProgressContribution>();
                return;
            }

            this.contributions = new RunChapterProgressContribution[
                contributions.Count];
            for (int i = 0; i < contributions.Count; i++)
                this.contributions[i] = contributions[i];
        }

        public string EventId { get; }
        public string ParticipantId { get; }
        public IReadOnlyList<RunChapterProgressContribution> Contributions =>
            contributions;
    }

    public enum RunChapterProgressApplicationError
    {
        None,
        MissingEventId,
        MissingParticipantId,
        EmptyBatch,
        MissingSourceId,
        InvalidAmount,
        DuplicateSource,
        DuplicateEvent,
        ChapterFlowRejected
    }

    public readonly struct RunChapterProgressApplicationResult
    {
        private RunChapterProgressApplicationResult(
            bool success,
            RunChapterProgressApplicationError error,
            string eventId,
            string participantId,
            int acceptedContributionCount,
            RunChapterFlowResult chapterFlowResult)
        {
            Success = success;
            Error = error;
            EventId = eventId ?? string.Empty;
            ParticipantId = participantId ?? string.Empty;
            AcceptedContributionCount = acceptedContributionCount;
            ChapterFlowResult = chapterFlowResult;
        }

        public bool Success { get; }
        public RunChapterProgressApplicationError Error { get; }
        public string EventId { get; }
        public string ParticipantId { get; }
        public int AcceptedContributionCount { get; }
        public RunChapterFlowResult ChapterFlowResult { get; }

        internal static RunChapterProgressApplicationResult Succeeded(
            RunChapterProgressCommand command,
            RunChapterFlowResult chapterFlowResult)
        {
            return new RunChapterProgressApplicationResult(
                true,
                RunChapterProgressApplicationError.None,
                command.EventId,
                command.ParticipantId,
                command.Contributions.Count,
                chapterFlowResult);
        }

        internal static RunChapterProgressApplicationResult Failed(
            RunChapterProgressApplicationError error,
            RunChapterProgressCommand command = null,
            RunChapterFlowResult chapterFlowResult = default)
        {
            return new RunChapterProgressApplicationResult(
                false,
                error,
                command?.EventId,
                command?.ParticipantId,
                0,
                chapterFlowResult);
        }
    }

    public sealed class RunChapterProgressApplicationService
    {
        private readonly RunChapterFlowService chapterFlow;
        private readonly HashSet<string> processedEventIds =
            new HashSet<string>(StringComparer.Ordinal);

        public RunChapterProgressApplicationService(
            RunChapterFlowService chapterFlow)
        {
            this.chapterFlow = chapterFlow ??
                throw new ArgumentNullException(nameof(chapterFlow));
        }

        public int ProcessedEventCount => processedEventIds.Count;

        public RunChapterProgressApplicationResult TryApply(
            RunChapterProgressCommand command,
            double simulationTime)
        {
            RunChapterProgressApplicationError validationError =
                Validate(command, out float totalAmount);
            if (validationError != RunChapterProgressApplicationError.None)
            {
                return RunChapterProgressApplicationResult.Failed(
                    validationError,
                    command);
            }

            if (!processedEventIds.Add(command.EventId))
            {
                return RunChapterProgressApplicationResult.Failed(
                    RunChapterProgressApplicationError.DuplicateEvent,
                    command);
            }

            RunChapterFlowResult flowResult = chapterFlow.TryAddProgress(
                totalAmount,
                simulationTime);
            if (!flowResult.Success)
            {
                return RunChapterProgressApplicationResult.Failed(
                    RunChapterProgressApplicationError.ChapterFlowRejected,
                    command,
                    flowResult);
            }

            return RunChapterProgressApplicationResult.Succeeded(
                command,
                flowResult);
        }

        private static RunChapterProgressApplicationError Validate(
            RunChapterProgressCommand command,
            out float totalAmount)
        {
            totalAmount = 0f;
            if (command == null || string.IsNullOrWhiteSpace(command.EventId))
                return RunChapterProgressApplicationError.MissingEventId;

            if (string.IsNullOrWhiteSpace(command.ParticipantId))
                return RunChapterProgressApplicationError.MissingParticipantId;

            if (command.Contributions.Count == 0)
                return RunChapterProgressApplicationError.EmptyBatch;

            HashSet<string> sourceIds = new HashSet<string>(
                StringComparer.Ordinal);
            double total = 0d;
            for (int i = 0; i < command.Contributions.Count; i++)
            {
                RunChapterProgressContribution contribution =
                    command.Contributions[i];
                if (string.IsNullOrWhiteSpace(contribution.SourceId))
                    return RunChapterProgressApplicationError.MissingSourceId;

                if (!sourceIds.Add(contribution.SourceId))
                    return RunChapterProgressApplicationError.DuplicateSource;

                if (contribution.Amount <= 0f ||
                    float.IsNaN(contribution.Amount) ||
                    float.IsInfinity(contribution.Amount))
                {
                    return RunChapterProgressApplicationError.InvalidAmount;
                }

                total += contribution.Amount;
                if (total > float.MaxValue)
                    return RunChapterProgressApplicationError.InvalidAmount;
            }

            totalAmount = (float)total;
            return RunChapterProgressApplicationError.None;
        }
    }
}
