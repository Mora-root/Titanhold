using System;
using System.Collections.Generic;

namespace Titanhold.Run
{
    public sealed class RunFlowService
    {
        private readonly AssaultScalingCalculator assaultScalingCalculator;
        private readonly IRunRoundBalanceResolver roundBalance;

        public RunFlowService(RunFlowConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            roundBalance = configuration.RoundBalance;
            if (!roundBalance.TryResolve(
                    configuration.StartingRound,
                    out RunRoundBalanceSnapshot startingBalance))
            {
                throw new ArgumentException(
                    "Starting round balance is unavailable.",
                    nameof(configuration));
            }

            State = new RunFlowState(
                configuration,
                startingBalance);
            assaultScalingCalculator = new AssaultScalingCalculator(
                configuration.AssaultHealthBonusPerLevel,
                configuration.AssaultDamageBonusPerLevel);
        }

        public RunFlowState State { get; }

        public event Action<RunFlowState> StateChanged;

        public ExplorationKillBatchResult TryRegisterExplorationKill(ExplorationKillContribution contribution)
        {
            if (!IsKillContributionValid(contribution))
                return ExplorationKillBatchResult.Failed(RunFlowError.InvalidKillContribution, State.Phase);

            return ApplyKillContributionTotals(contribution.ThreatAmount, contribution.InstabilityPoints);
        }

        public ExplorationKillBatchResult TryRegisterExplorationKillBatch(
            IReadOnlyList<ExplorationKillContribution> contributions)
        {
            if (contributions == null || contributions.Count == 0)
                return ExplorationKillBatchResult.Failed(RunFlowError.EmptyKillBatch, State.Phase);

            double threatTotal = 0d;
            long instabilityTotal = 0L;

            for (int i = 0; i < contributions.Count; i++)
            {
                ExplorationKillContribution contribution = contributions[i];
                if (!IsKillContributionValid(contribution))
                {
                    return ExplorationKillBatchResult.Failed(
                        RunFlowError.InvalidKillContribution,
                        State.Phase);
                }

                threatTotal += contribution.ThreatAmount;
                instabilityTotal += contribution.InstabilityPoints;

                if (threatTotal > float.MaxValue || instabilityTotal > int.MaxValue)
                {
                    return ExplorationKillBatchResult.Failed(
                        RunFlowError.InvalidKillContribution,
                        State.Phase);
                }
            }

            return ApplyKillContributionTotals((float)threatTotal, (int)instabilityTotal);
        }

        public RunFlowTransitionResult TryBeginAssaultTransition()
        {
            if (State.Phase != RunPhase.PortalOpen)
                return RunFlowTransitionResult.Failed(RunFlowError.InvalidPhase, State.Phase);

            State.SetAssaultScaling(
                assaultScalingCalculator.CreateSnapshot(
                    State.RiftInstability,
                    State.RoundScaling));
            return TransitionTo(RunPhase.TransitionToAssault);
        }

        public RunFlowTransitionResult TryStartAssault()
        {
            return TryTransition(RunPhase.TransitionToAssault, RunPhase.Assault);
        }

        public RunFlowTransitionResult TryCompleteAssault()
        {
            return TryTransition(RunPhase.Assault, RunPhase.Intermission);
        }

        public RunFlowTransitionResult TryBeginReturnToExploration()
        {
            if (State.Phase == RunPhase.Intermission &&
                !State.CanReturnToExploration)
            {
                return RunFlowTransitionResult.Failed(
                    RunFlowError.FinalEncounterCompleted,
                    State.Phase);
            }

            return TryTransition(RunPhase.Intermission, RunPhase.ReturningToExploration);
        }

        public RunFlowTransitionResult TryResumeExploration()
        {
            if (State.Phase != RunPhase.ReturningToExploration)
                return RunFlowTransitionResult.Failed(RunFlowError.InvalidPhase, State.Phase);

            RunPhase previousPhase = State.Phase;
            int nextRound = State.RoundNumber < int.MaxValue
                ? State.RoundNumber + 1
                : State.RoundNumber;
            if (!roundBalance.TryResolve(
                    nextRound,
                    out RunRoundBalanceSnapshot nextBalance))
            {
                return RunFlowTransitionResult.Failed(
                    RunFlowError.RoundBalanceUnavailable,
                    State.Phase);
            }

            State.BeginNextRound(nextBalance);
            NotifyStateChanged();
            return RunFlowTransitionResult.Succeeded(previousPhase, State.Phase);
        }

        public RunFlowTransitionResult TryCompleteRun()
        {
            if (State.Phase == RunPhase.Intermission &&
                State.CurrentEncounterKind != RunEncounterKind.Boss)
            {
                return RunFlowTransitionResult.Failed(
                    RunFlowError.NotFinalEncounter,
                    State.Phase);
            }

            return TryTransition(RunPhase.Intermission, RunPhase.Completed);
        }

        public RunFlowTransitionResult TryFailRun()
        {
            if (State.IsTerminal)
                return RunFlowTransitionResult.Failed(RunFlowError.TerminalState, State.Phase);

            return TransitionTo(RunPhase.Failed);
        }

        public RunFlowTransitionResult TryAbandonRun()
        {
            if (State.IsTerminal)
                return RunFlowTransitionResult.Failed(
                    RunFlowError.TerminalState,
                    State.Phase);

            return TransitionTo(RunPhase.Abandoned);
        }

        private ExplorationKillBatchResult ApplyKillContributionTotals(float threatTotal, int instabilityTotal)
        {
            RunPhase previousPhase = State.Phase;

            switch (previousPhase)
            {
                case RunPhase.Exploration:
                    if (threatTotal <= 0f)
                    {
                        return ExplorationKillBatchResult.Failed(
                            RunFlowError.NoApplicableContribution,
                            State.Phase);
                    }

                    float threatAdded = State.AddThreat(threatTotal);
                    bool portalOpened = State.IsThreatFull;
                    if (portalOpened)
                        State.SetPhase(RunPhase.PortalOpen);

                    NotifyStateChanged();
                    return ExplorationKillBatchResult.Succeeded(
                        previousPhase,
                        State.Phase,
                        threatAdded,
                        0,
                        portalOpened);

                case RunPhase.PortalOpen:
                    if (instabilityTotal <= 0)
                    {
                        return ExplorationKillBatchResult.Failed(
                            RunFlowError.NoApplicableContribution,
                            State.Phase);
                    }

                    int pointsAdded = State.RiftInstability.AddPoints(instabilityTotal);
                    if (pointsAdded <= 0)
                    {
                        return ExplorationKillBatchResult.Failed(
                            RunFlowError.NoApplicableContribution,
                            State.Phase);
                    }

                    NotifyStateChanged();
                    return ExplorationKillBatchResult.Succeeded(
                        previousPhase,
                        State.Phase,
                        0f,
                        pointsAdded,
                        false);

                default:
                    return ExplorationKillBatchResult.Failed(RunFlowError.InvalidPhase, State.Phase);
            }
        }

        private RunFlowTransitionResult TryTransition(RunPhase expectedPhase, RunPhase nextPhase)
        {
            if (State.Phase != expectedPhase)
                return RunFlowTransitionResult.Failed(RunFlowError.InvalidPhase, State.Phase);

            return TransitionTo(nextPhase);
        }

        private RunFlowTransitionResult TransitionTo(RunPhase nextPhase)
        {
            RunPhase previousPhase = State.Phase;
            State.SetPhase(nextPhase);
            NotifyStateChanged();
            return RunFlowTransitionResult.Succeeded(previousPhase, nextPhase);
        }

        private void NotifyStateChanged()
        {
            StateChanged?.Invoke(State);
        }

        private static bool IsKillContributionValid(ExplorationKillContribution contribution)
        {
            return contribution.ThreatAmount >= 0f &&
                   !float.IsNaN(contribution.ThreatAmount) &&
                   !float.IsInfinity(contribution.ThreatAmount) &&
                   contribution.InstabilityPoints >= 0;
        }
    }
}
