using System;

namespace Titanhold.Run
{
    public sealed class RunFlowState
    {
        internal RunFlowState(
            RunFlowConfiguration configuration,
            RunRoundBalanceSnapshot roundBalance)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            FinalRoundNumber = configuration.FinalRoundNumber;
            Phase = RunPhase.Exploration;
            RiftInstability = new RiftInstabilityState(configuration.InstabilityPointsPerLevel);
            ApplyRoundBalance(roundBalance);
            AssaultScaling = AssaultScalingSnapshot.NoneForRound(RoundNumber);
        }

        public RunPhase Phase { get; private set; }
        public int RoundNumber { get; private set; }
        public float CurrentThreat { get; private set; }
        public float MaxThreat => RoundBalance.MaxThreat;
        public float ExperienceMultiplier =>
            RoundBalance.ExperienceMultiplier;
        public int FinalRoundNumber { get; }
        public bool IsTerminal =>
            Phase == RunPhase.Completed ||
            Phase == RunPhase.Failed ||
            Phase == RunPhase.Abandoned;
        public bool IsThreatFull => CurrentThreat >= MaxThreat;
        public RunEncounterKind CurrentEncounterKind =>
            RoundNumber == FinalRoundNumber
                ? RunEncounterKind.Boss
                : RunEncounterKind.AssaultWave;
        public bool CanReturnToExploration =>
            CurrentEncounterKind == RunEncounterKind.AssaultWave;
        public RiftInstabilityState RiftInstability { get; }
        public RunRoundBalanceSnapshot RoundBalance { get; private set; }
        public EnemyScalingSnapshot RoundScaling =>
            RoundBalance.EnemyScaling;
        public AssaultScalingSnapshot AssaultScaling { get; private set; }

        internal float AddThreat(float amount)
        {
            if (amount <= 0f || IsThreatFull)
                return 0f;

            float previousThreat = CurrentThreat;
            double total = (double)CurrentThreat + amount;
            CurrentThreat = total >= MaxThreat ? MaxThreat : (float)total;
            return CurrentThreat - previousThreat;
        }

        internal void SetPhase(RunPhase phase)
        {
            Phase = phase;
        }

        internal void SetAssaultScaling(AssaultScalingSnapshot snapshot)
        {
            AssaultScaling = snapshot;
        }

        internal void BeginNextRound(RunRoundBalanceSnapshot roundBalance)
        {
            CurrentThreat = 0f;
            RiftInstability.Reset();
            ApplyRoundBalance(roundBalance);
            AssaultScaling = AssaultScalingSnapshot.NoneForRound(RoundNumber);
            Phase = RunPhase.Exploration;
        }

        private void ApplyRoundBalance(
            RunRoundBalanceSnapshot roundBalance)
        {
            RoundBalance = roundBalance;
            RoundNumber = roundBalance.RoundNumber;
        }
    }
}
