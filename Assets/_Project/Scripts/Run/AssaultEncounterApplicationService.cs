using System;

namespace Titanhold.Run
{
    public sealed class AssaultEncounterApplicationService
    {
        private readonly RunFlowService runFlowService;

        public AssaultEncounterApplicationService(RunFlowService runFlowService)
        {
            this.runFlowService = runFlowService ??
                throw new ArgumentNullException(nameof(runFlowService));
            State = new AssaultEncounterState();
        }

        public AssaultEncounterState State { get; }

        public event Action<AssaultEncounterState> StateChanged;

        public AssaultEncounterResult TryBegin(BeginAssaultEncounterCommand command)
        {
            if (!command.EncounterId.IsValid)
            {
                return AssaultEncounterResult.Failed(
                    AssaultEncounterError.InvalidEncounterId);
            }

            if (command.ExpectedRound <= 0 ||
                command.ExpectedRound != runFlowService.State.RoundNumber)
            {
                return AssaultEncounterResult.Failed(
                    AssaultEncounterError.InvalidExpectedRound);
            }

            if (command.PlannedEnemyCount <= 0)
            {
                return AssaultEncounterResult.Failed(
                    AssaultEncounterError.InvalidPlannedEnemyCount);
            }

            if (runFlowService.State.Phase != RunPhase.TransitionToAssault)
                return AssaultEncounterResult.Failed(AssaultEncounterError.InvalidPhase);

            State.Begin(
                command.EncounterId,
                command.ExpectedRound,
                command.PlannedEnemyCount);

            RunFlowTransitionResult transition = runFlowService.TryStartAssault();
            if (!transition.Success)
            {
                State.Reset();
                return AssaultEncounterResult.Failed(
                    AssaultEncounterError.RunFlowRejected);
            }

            NotifyStateChanged();
            return AssaultEncounterResult.Succeeded(
                runFlowTransition: transition);
        }

        public AssaultEncounterResult TryRegisterSpawn(AssaultEnemyCommand command)
        {
            AssaultEncounterError validationError = ValidateEnemyCommand(command);
            if (validationError != AssaultEncounterError.None)
                return AssaultEncounterResult.Failed(validationError);

            if (State.ContainsSpawnedEnemy(command.Enemy))
                return AssaultEncounterResult.Failed(AssaultEncounterError.DuplicateEnemy);

            if (State.SpawnedEnemyCount >= State.PlannedEnemyCount)
                return AssaultEncounterResult.Failed(AssaultEncounterError.SpawnLimitReached);

            State.RegisterSpawn(command.Enemy);
            NotifyStateChanged();
            return AssaultEncounterResult.Succeeded();
        }

        public AssaultEncounterResult TryRegisterDefeat(AssaultEnemyCommand command)
        {
            AssaultEncounterError validationError = ValidateEnemyCommand(command);
            if (validationError != AssaultEncounterError.None)
                return AssaultEncounterResult.Failed(validationError);

            if (!State.ContainsAliveEnemy(command.Enemy))
                return AssaultEncounterResult.Failed(AssaultEncounterError.EnemyNotAlive);

            bool completesEncounter =
                State.IsSpawnSequenceCompleted && State.AliveEnemyCount == 1;

            State.RegisterDefeat(command.Enemy);

            RunFlowTransitionResult transition = default;
            if (completesEncounter)
            {
                transition = runFlowService.TryCompleteAssault();
                if (!transition.Success)
                {
                    State.RollbackDefeat(command.Enemy);
                    return AssaultEncounterResult.Failed(
                        AssaultEncounterError.RunFlowRejected);
                }

                State.MarkCompleted();
            }

            NotifyStateChanged();
            return AssaultEncounterResult.Succeeded(
                completesEncounter,
                transition);
        }

        private AssaultEncounterError ValidateEnemyCommand(AssaultEnemyCommand command)
        {
            if (!command.Enemy.IsValid || !command.Enemy.IsEnemy)
                return AssaultEncounterError.InvalidEnemy;

            if (!State.IsActive)
                return AssaultEncounterError.EncounterNotActive;

            if (runFlowService.State.Phase != RunPhase.Assault)
                return AssaultEncounterError.InvalidPhase;

            if (command.EncounterId != State.EncounterId)
                return AssaultEncounterError.StaleEncounter;

            return AssaultEncounterError.None;
        }

        private void NotifyStateChanged()
        {
            StateChanged?.Invoke(State);
        }
    }
}
