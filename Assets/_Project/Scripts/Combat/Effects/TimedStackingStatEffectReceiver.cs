using Titanhold.Combat;
using UnityEngine;

namespace Titanhold.Combat.Effects
{
    [DisallowMultipleComponent]
    public sealed class TimedStackingStatEffectReceiver :
        MonoBehaviour,
        ITimedStackingStatEffectReceiver
    {
        [SerializeField] private CharacterStats characterStats;

        private TimedStackingStatEffectService effects;

        public int ActiveEffectCount => effects?.ActiveCount ?? 0;

        private void Awake()
        {
            ResolveReferences();
            if (characterStats != null)
            {
                effects = new TimedStackingStatEffectService(
                    new CharacterStatsGateway(characterStats));
            }
        }

        private void Update()
        {
            if (effects?.ActiveCount > 0)
                effects.Tick(Time.timeAsDouble);
        }

        private void OnDisable()
        {
            effects?.Clear();
        }

        public bool TryApply(
            TimedStackingStatEffectDefinition definition,
            CombatActorReference source,
            out TimedStatEffectSnapshot snapshot)
        {
            return TryApply(
                definition,
                source,
                Time.timeAsDouble,
                out snapshot);
        }

        public bool TryApply(
            TimedStackingStatEffectDefinition definition,
            CombatActorReference source,
            double simulationTime,
            out TimedStatEffectSnapshot snapshot)
        {
            snapshot = default;
            return effects != null && effects.TryApply(
                definition,
                source,
                simulationTime,
                out snapshot);
        }

        public bool TryGet(
            string effectId,
            CombatActorReference source,
            out TimedStatEffectSnapshot snapshot)
        {
            snapshot = default;
            return effects != null &&
                   effects.TryGet(effectId, source, out snapshot);
        }

        private void ResolveReferences()
        {
            if (characterStats == null)
                characterStats = GetComponent<CharacterStats>();
            if (characterStats == null)
                characterStats = GetComponentInParent<CharacterStats>();
        }

        private sealed class CharacterStatsGateway :
            ITimedStatModifierGateway
        {
            private readonly CharacterStats stats;

            public CharacterStatsGateway(CharacterStats stats)
            {
                this.stats = stats;
            }

            public bool TrySetModifier(
                StatModifierSource modifierSource,
                StatModifier modifier)
            {
                return stats != null &&
                       stats.TrySetModifierFromSource(
                           modifier,
                           modifierSource);
            }

            public void RemoveModifier(
                StatModifierSource modifierSource)
            {
                if (stats != null)
                    stats.RemoveModifiersFromSource(modifierSource);
            }
        }
    }
}
