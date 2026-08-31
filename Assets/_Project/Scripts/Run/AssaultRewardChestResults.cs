using Titanhold.Combat;

namespace Titanhold.Run
{
    public enum AssaultRewardChestError
    {
        None,
        NotInitialized,
        InvalidEntrant,
        EntrantNotRegistered,
        InvalidExpectedRound,
        InvalidEncounterId,
        InvalidPhase,
        StaleChest,
        RewardAlreadyClaimed,
        InvalidDropConfiguration,
        ClaimRejected,
        EmissionRejected
    }

    public readonly struct AssaultRewardChestResult
    {
        private AssaultRewardChestResult(
            bool success,
            AssaultRewardChestError error,
            CombatActorReference entrant,
            AssaultEncounterId encounterId,
            int expectedRound,
            AssaultRewardResult rewardResult,
            WorldLootEmissionResult emissionResult)
        {
            Success = success;
            Error = error;
            Entrant = entrant;
            EncounterId = encounterId;
            ExpectedRound = expectedRound;
            RewardResult = rewardResult;
            EmissionResult = emissionResult;
        }

        public bool Success { get; }
        public AssaultRewardChestError Error { get; }
        public CombatActorReference Entrant { get; }
        public AssaultEncounterId EncounterId { get; }
        public int ExpectedRound { get; }
        public AssaultRewardResult RewardResult { get; }
        public WorldLootEmissionResult EmissionResult { get; }

        public static AssaultRewardChestResult Succeeded(
            CombatActorReference entrant,
            AssaultEncounterId encounterId,
            int expectedRound,
            AssaultRewardResult rewardResult,
            WorldLootEmissionResult emissionResult)
        {
            return new AssaultRewardChestResult(
                true,
                AssaultRewardChestError.None,
                entrant,
                encounterId,
                expectedRound,
                rewardResult,
                emissionResult);
        }

        public static AssaultRewardChestResult Failed(
            AssaultRewardChestError error,
            CombatActorReference entrant = default,
            AssaultEncounterId encounterId = default,
            int expectedRound = 0,
            AssaultRewardResult rewardResult = default,
            WorldLootEmissionResult emissionResult = default)
        {
            return new AssaultRewardChestResult(
                false,
                error,
                entrant,
                encounterId,
                expectedRound,
                rewardResult,
                emissionResult);
        }
    }
}
