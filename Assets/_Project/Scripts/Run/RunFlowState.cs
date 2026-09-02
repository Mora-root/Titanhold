using System;

namespace Titanhold.Run
{
    public sealed class RunFlowState
    {
        internal RunFlowState(
            RunFlowConfiguration configuration,
            EnemyScalingSnapshot roundScaling)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            MaxThreat = configuration.MaxThreat;
            FinalRoundNumber = configuration.FinalRoundNumber;
            RoundNumber = configuration.StartingRound;
            Phase = RunPhase.Exploration;
            RiftInstability = new RiftInstabilityState(configuration.InstabilityPointsPerLevel);
            RoundScaling = roundScaling;
            AssaultScaling = AssaultScalingSnapshot.NoneForRound(roundScaling.RoundNumber);
        }

        public RunPhase Phase { get; private set; }
        public int RoundNumber { get; private set; }
        public float CurrentThreat { get; private set; }
        public float MaxThreat { get; }
        public int FinalRoundNumber { get; }
        public bool IsThreatFull => CurrentThreat >= MaxThreat;
        public RunEncounterKind CurrentEncounterKind =>
            RoundNumber == FinalRoundNumber
                ? RunEncounterKind.Boss
                : RunEncounterKind.AssaultWave;
        public bool CanReturnToExploration =>
            CurrentEncounterKind == RunEncounterKind.AssaultWave;
        public RiftInstabilityState RiftInstability { get; }
        public EnemyScalingSnapshot RoundScaling { get; private set; }
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

        internal void BeginNextRound(EnemyScalingSnapshot roundScaling)
        {
            if (RoundNumber < int.MaxValue)
                RoundNumber++;

            CurrentThreat = 0f;
            RiftInstability.Reset();
            RoundScaling = roundScaling;
            AssaultScaling = AssaultScalingSnapshot.NoneForRound(roundScaling.RoundNumber);
            Phase = RunPhase.Exploration;
        }
    }
}
