using System.Collections.Generic;
using Titanhold.Combat;

namespace Titanhold.Run
{
    public readonly struct PrepareAssaultRewardCommand
    {
        public PrepareAssaultRewardCommand(
            AssaultEncounterId encounterId,
            int expectedRound,
            int rollSeed,
            IReadOnlyList<LootDropResult> drops)
        {
            EncounterId = encounterId;
            ExpectedRound = expectedRound;
            RollSeed = rollSeed;
            Drops = drops;
        }

        public AssaultEncounterId EncounterId { get; }
        public int ExpectedRound { get; }
        public int RollSeed { get; }
        public IReadOnlyList<LootDropResult> Drops { get; }
    }

    public readonly struct ClaimAssaultRewardCommand
    {
        public ClaimAssaultRewardCommand(
            AssaultEncounterId encounterId,
            int expectedRound,
            CombatActorReference claimant)
        {
            EncounterId = encounterId;
            ExpectedRound = expectedRound;
            Claimant = claimant;
        }

        public AssaultEncounterId EncounterId { get; }
        public int ExpectedRound { get; }
        public CombatActorReference Claimant { get; }
    }
}
