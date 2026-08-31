using System;
using System.Collections.Generic;

namespace Titanhold.Run
{
    public sealed class AssaultRewardApplicationService : IDisposable
    {
        private readonly RunFlowService runFlowService;
        private bool disposed;

        public AssaultRewardApplicationService(RunFlowService runFlowService)
        {
            this.runFlowService = runFlowService ??
                throw new ArgumentNullException(nameof(runFlowService));
            State = new AssaultRewardState();
            runFlowService.StateChanged += HandleRunFlowStateChanged;
        }

        public AssaultRewardState State { get; }

        public event Action<AssaultRewardState> StateChanged;

        public AssaultRewardResult TryPrepare(
            PrepareAssaultRewardCommand command)
        {
            if (!command.EncounterId.IsValid)
            {
                return AssaultRewardResult.Failed(
                    AssaultRewardError.InvalidEncounterId);
            }

            if (command.ExpectedRound <= 0 ||
                command.ExpectedRound != runFlowService.State.RoundNumber)
            {
                return AssaultRewardResult.Failed(
                    AssaultRewardError.InvalidExpectedRound,
                    command.EncounterId,
                    command.ExpectedRound);
            }

            if (runFlowService.State.Phase != RunPhase.Intermission)
            {
                return AssaultRewardResult.Failed(
                    AssaultRewardError.InvalidPhase,
                    command.EncounterId,
                    command.ExpectedRound);
            }

            if (State.HasReward)
            {
                return AssaultRewardResult.Failed(
                    AssaultRewardError.RewardAlreadyPrepared,
                    command.EncounterId,
                    command.ExpectedRound);
            }

            if (!AreDropsValid(command.Drops))
            {
                return AssaultRewardResult.Failed(
                    AssaultRewardError.InvalidDrops,
                    command.EncounterId,
                    command.ExpectedRound);
            }

            State.Prepare(
                command.EncounterId,
                command.ExpectedRound,
                command.RollSeed,
                command.Drops);
            NotifyStateChanged();
            return AssaultRewardResult.Succeeded(
                command.EncounterId,
                command.ExpectedRound);
        }

        public AssaultRewardResult TryClaim(
            ClaimAssaultRewardCommand command)
        {
            if (!command.Claimant.IsValid || !command.Claimant.IsPlayer)
            {
                return AssaultRewardResult.Failed(
                    AssaultRewardError.InvalidClaimant,
                    command.EncounterId,
                    command.ExpectedRound,
                    command.Claimant);
            }

            if (runFlowService.State.Phase != RunPhase.Intermission)
            {
                return AssaultRewardResult.Failed(
                    AssaultRewardError.InvalidPhase,
                    command.EncounterId,
                    command.ExpectedRound,
                    command.Claimant);
            }

            if (!State.HasReward)
            {
                return AssaultRewardResult.Failed(
                    AssaultRewardError.RewardNotPrepared,
                    command.EncounterId,
                    command.ExpectedRound,
                    command.Claimant);
            }

            if (State.EncounterId != command.EncounterId ||
                State.RoundNumber != command.ExpectedRound ||
                runFlowService.State.RoundNumber != command.ExpectedRound)
            {
                return AssaultRewardResult.Failed(
                    AssaultRewardError.StaleReward,
                    command.EncounterId,
                    command.ExpectedRound,
                    command.Claimant);
            }

            if (State.IsClaimed)
            {
                return AssaultRewardResult.Failed(
                    AssaultRewardError.RewardAlreadyClaimed,
                    command.EncounterId,
                    command.ExpectedRound,
                    command.Claimant);
            }

            State.Claim(command.Claimant);
            NotifyStateChanged();
            return AssaultRewardResult.Succeeded(
                command.EncounterId,
                command.ExpectedRound,
                command.Claimant);
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            runFlowService.StateChanged -= HandleRunFlowStateChanged;
        }

        private void HandleRunFlowStateChanged(RunFlowState runState)
        {
            if (!State.HasReward || runState.RoundNumber == State.RoundNumber)
                return;

            State.Clear();
            NotifyStateChanged();
        }

        private void NotifyStateChanged()
        {
            StateChanged?.Invoke(State);
        }

        private static bool AreDropsValid(
            IReadOnlyList<LootDropResult> drops)
        {
            if (drops == null || drops.Count == 0)
                return false;

            for (int i = 0; i < drops.Count; i++)
            {
                LootDropResult drop = drops[i];
                switch (drop.Kind)
                {
                    case LootDropKind.Gold:
                        if (drop.GoldAmount <= 0)
                            return false;
                        break;

                    case LootDropKind.Item:
                        if (drop.Stack == null ||
                            drop.Stack.Definition == null ||
                            drop.Stack.Amount <= 0)
                        {
                            return false;
                        }
                        break;

                    default:
                        return false;
                }
            }

            return true;
        }
    }
}
