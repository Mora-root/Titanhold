using System;
using System.Collections.Generic;
using Titanhold.Combat;

namespace Titanhold.Run
{
    public sealed class ExplorationKillApplicationService
    {
        private readonly RunFlowService runFlowService;
        private readonly HashSet<CombatExecutionId> processedExecutions = new HashSet<CombatExecutionId>();

        public ExplorationKillApplicationService(RunFlowService runFlowService)
        {
            this.runFlowService = runFlowService ?? throw new ArgumentNullException(nameof(runFlowService));
        }

        public int ProcessedExecutionCount => processedExecutions.Count;

        public ExplorationKillApplicationResult TryApplyBatch(
            IReadOnlyList<ExplorationKillRecord> killRecords)
        {
            if (killRecords == null || killRecords.Count == 0)
            {
                return ExplorationKillApplicationResult.Failed(
                    ExplorationKillApplicationError.EmptyBatch);
            }

            ExplorationKillRecord firstRecord = killRecords[0];
            ExplorationKillApplicationError firstError = ValidateFirstRecord(firstRecord);
            if (firstError != ExplorationKillApplicationError.None)
                return ExplorationKillApplicationResult.Failed(firstError, firstRecord.ExecutionId);

            CombatExecutionId executionId = firstRecord.ExecutionId;
            CombatActorReference source = firstRecord.Source;

            if (processedExecutions.Contains(executionId))
            {
                return ExplorationKillApplicationResult.Failed(
                    ExplorationKillApplicationError.DuplicateExecution,
                    executionId);
            }

            HashSet<CombatActorReference> defeatedActors = new HashSet<CombatActorReference>();
            List<ExplorationKillContribution> contributions =
                new List<ExplorationKillContribution>(killRecords.Count);

            for (int i = 0; i < killRecords.Count; i++)
            {
                ExplorationKillRecord record = killRecords[i];
                ExplorationKillApplicationError error = ValidateRecord(record, executionId, source);
                if (error != ExplorationKillApplicationError.None)
                    return ExplorationKillApplicationResult.Failed(error, executionId);

                if (!defeatedActors.Add(record.DefeatedActor))
                {
                    return ExplorationKillApplicationResult.Failed(
                        ExplorationKillApplicationError.DuplicateDefeatedActor,
                        executionId);
                }

                contributions.Add(record.Contribution);
            }

            processedExecutions.Add(executionId);
            ExplorationKillBatchResult runFlowResult =
                runFlowService.TryRegisterExplorationKillBatch(contributions);
            if (!runFlowResult.Success)
            {
                return ExplorationKillApplicationResult.Failed(
                    ExplorationKillApplicationError.RunFlowRejected,
                    executionId,
                    runFlowResult);
            }

            return ExplorationKillApplicationResult.Succeeded(
                executionId,
                killRecords.Count,
                runFlowResult);
        }

        private static ExplorationKillApplicationError ValidateFirstRecord(ExplorationKillRecord record)
        {
            if (!record.DeathContext.IsValid)
                return ExplorationKillApplicationError.InvalidDeathContext;

            if (!record.DeathContext.IsPlayerAttributed)
                return ExplorationKillApplicationError.NonPlayerSource;

            if (!record.DefeatedActor.IsValid || !record.DefeatedActor.IsEnemy)
                return ExplorationKillApplicationError.InvalidDefeatedActor;

            return ExplorationKillApplicationError.None;
        }

        private static ExplorationKillApplicationError ValidateRecord(
            ExplorationKillRecord record,
            CombatExecutionId expectedExecutionId,
            CombatActorReference expectedSource)
        {
            ExplorationKillApplicationError basicError = ValidateFirstRecord(record);
            if (basicError != ExplorationKillApplicationError.None)
                return basicError;

            if (record.ExecutionId != expectedExecutionId)
                return ExplorationKillApplicationError.MixedExecution;

            if (record.Source != expectedSource)
                return ExplorationKillApplicationError.MixedSource;

            return ExplorationKillApplicationError.None;
        }
    }
}
