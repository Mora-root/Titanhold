using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Titanhold.Combat;

namespace Titanhold.Run
{
    public sealed class AssaultRewardState
    {
        private ReadOnlyCollection<LootDropResult> drops =
            Array.AsReadOnly(Array.Empty<LootDropResult>());

        public AssaultEncounterId EncounterId { get; private set; }
        public int RoundNumber { get; private set; }
        public int RollSeed { get; private set; }
        public IReadOnlyList<LootDropResult> Drops => drops;
        public bool HasReward { get; private set; }
        public bool IsClaimed { get; private set; }
        public CombatActorReference ClaimedBy { get; private set; }

        internal void Prepare(
            AssaultEncounterId encounterId,
            int roundNumber,
            int rollSeed,
            IReadOnlyList<LootDropResult> sourceDrops)
        {
            LootDropResult[] copy = new LootDropResult[sourceDrops.Count];
            for (int i = 0; i < sourceDrops.Count; i++)
                copy[i] = sourceDrops[i];

            EncounterId = encounterId;
            RoundNumber = roundNumber;
            RollSeed = rollSeed;
            drops = Array.AsReadOnly(copy);
            HasReward = true;
            IsClaimed = false;
            ClaimedBy = CombatActorReference.Unknown;
        }

        internal void Claim(CombatActorReference claimant)
        {
            IsClaimed = true;
            ClaimedBy = claimant;
        }

        internal void Clear()
        {
            EncounterId = default;
            RoundNumber = 0;
            RollSeed = 0;
            drops = Array.AsReadOnly(Array.Empty<LootDropResult>());
            HasReward = false;
            IsClaimed = false;
            ClaimedBy = CombatActorReference.Unknown;
        }
    }
}
