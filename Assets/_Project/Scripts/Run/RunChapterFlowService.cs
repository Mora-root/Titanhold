using System;

namespace Titanhold.Run
{
    public sealed class RunChapterFlowService
    {
        public RunChapterFlowService(
            RunChapterFlowConfiguration configuration)
        {
            State = new RunChapterFlowState(
                configuration ??
                throw new ArgumentNullException(nameof(configuration)));
        }

        public RunChapterFlowState State { get; }

        public event Action<RunChapterFlowState> StateChanged;

        public RunChapterFlowResult TryAddProgress(
            float amount,
            double simulationTime)
        {
            if (amount <= 0f ||
                float.IsNaN(amount) ||
                float.IsInfinity(amount))
            {
                return RunChapterFlowResult.Failed(
                    RunChapterFlowError.InvalidProgress,
                    State.Phase);
            }

            if (State.Phase != RunChapterPhase.Exploration)
            {
                return RunChapterFlowResult.Failed(
                    RunChapterFlowError.InvalidPhase,
                    State.Phase);
            }

            if (!State.TryObserveTime(
                    simulationTime,
                    out RunChapterFlowError timeError))
            {
                return RunChapterFlowResult.Failed(timeError, State.Phase);
            }

            RunChapterPhase previousPhase = State.Phase;
            int previousStage = State.EscalationStage;
            float progressAdded = State.AddProgress(amount);
            bool collapseStarted = State.IsProgressFull;
            if (collapseStarted)
                State.BeginCollapse(simulationTime);

            NotifyStateChanged();
            return RunChapterFlowResult.Succeeded(
                previousPhase,
                State.Phase,
                progressAdded,
                State.EscalationStage != previousStage,
                collapseStarted);
        }

        public RunChapterFlowResult TryAdvanceTime(double simulationTime)
        {
            if (!State.TryObserveTime(
                    simulationTime,
                    out RunChapterFlowError timeError))
            {
                return RunChapterFlowResult.Failed(timeError, State.Phase);
            }

            RunChapterPhase previousPhase = State.Phase;
            if (State.Phase != RunChapterPhase.RiftCollapse)
            {
                return RunChapterFlowResult.Succeeded(
                    previousPhase,
                    State.Phase);
            }

            bool stacksChanged = State.RefreshCollapse();
            bool forcedTransition =
                simulationTime >= State.CollapseDeadline;
            if (forcedTransition)
                State.BeginBossTransition(forced: true);

            if (stacksChanged || forcedTransition)
                NotifyStateChanged();

            return RunChapterFlowResult.Succeeded(
                previousPhase,
                State.Phase,
                forcedBossTransition: forcedTransition);
        }

        public RunChapterFlowResult TryEnterBossPortal(
            double simulationTime)
        {
            if (State.Phase != RunChapterPhase.RiftCollapse)
            {
                return RunChapterFlowResult.Failed(
                    RunChapterFlowError.InvalidPhase,
                    State.Phase);
            }

            if (!State.TryObserveTime(
                    simulationTime,
                    out RunChapterFlowError timeError))
            {
                return RunChapterFlowResult.Failed(timeError, State.Phase);
            }

            RunChapterPhase previousPhase = State.Phase;
            State.RefreshCollapse();
            bool forcedTransition =
                simulationTime >= State.CollapseDeadline;
            State.BeginBossTransition(forcedTransition);
            NotifyStateChanged();
            return RunChapterFlowResult.Succeeded(
                previousPhase,
                State.Phase,
                forcedBossTransition: forcedTransition);
        }

        public RunChapterFlowResult TryStartBoss()
        {
            return TryTransition(
                RunChapterPhase.TransitionToBoss,
                RunChapterPhase.Boss);
        }

        public RunChapterFlowResult TryDefeatBoss()
        {
            return TryTransition(
                RunChapterPhase.Boss,
                RunChapterPhase.Reward);
        }

        public RunChapterFlowResult TryCompleteChapter()
        {
            return TryTransition(
                RunChapterPhase.Reward,
                RunChapterPhase.Completed);
        }

        private RunChapterFlowResult TryTransition(
            RunChapterPhase expected,
            RunChapterPhase next)
        {
            if (State.Phase != expected)
            {
                return RunChapterFlowResult.Failed(
                    RunChapterFlowError.InvalidPhase,
                    State.Phase);
            }

            RunChapterPhase previousPhase = State.Phase;
            State.SetPhase(next);
            NotifyStateChanged();
            return RunChapterFlowResult.Succeeded(
                previousPhase,
                State.Phase);
        }

        private void NotifyStateChanged()
        {
            StateChanged?.Invoke(State);
        }
    }
}
