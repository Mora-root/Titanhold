using Titanhold.Combat;

namespace Titanhold.Run
{
    public enum AssaultRewardError
    {
        None,
        InvalidEncounterId,
        InvalidExpectedRound,
        InvalidDrops,
        InvalidClaimant,
        InvalidPhase,
        RewardNotPrepared,
        RewardAlreadyPrepared,
        StaleReward,
        RewardAlreadyClaimed
    }

    public readonly struct AssaultRewardResult
    {
        private AssaultRewardResult(
            bool success,
            AssaultRewardError error,
            AssaultEncounterId encounterId,
            int expectedRound,
            CombatActorReference claimant)
        {
            Success = success;
            Error = error;
            EncounterId = encounterId;
            ExpectedRound = expectedRound;
            Claimant = claimant;
        }

        public bool Success { get; }
        public AssaultRewardError Error { get; }
        public AssaultEncounterId EncounterId { get; }
        public int ExpectedRound { get; }
        public CombatActorReference Claimant { get; }

        public static AssaultRewardResult Succeeded(
            AssaultEncounterId encounterId,
            int expectedRound,
            CombatActorReference claimant = default)
        {
            return new AssaultRewardResult(
                true,
                AssaultRewardError.None,
                encounterId,
                expectedRound,
                claimant);
        }

        public static AssaultRewardResult Failed(
            AssaultRewardError error,
            AssaultEncounterId encounterId = default,
            int expectedRound = 0,
            CombatActorReference claimant = default)
        {
            return new AssaultRewardResult(
                false,
                error,
                encounterId,
                expectedRound,
                claimant);
        }
    }
}
