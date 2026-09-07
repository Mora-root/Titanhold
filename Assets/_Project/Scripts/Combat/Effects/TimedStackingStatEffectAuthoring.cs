using System;
using UnityEngine;

namespace Titanhold.Combat.Effects
{
    [Serializable]
    public sealed class TimedStackingStatEffectAuthoring
    {
        [SerializeField] private bool enabled;
        [SerializeField] private string effectId;
        [SerializeField] private StatType statType = StatType.Armor;
        [SerializeField]
        private StatModifierType modifierType = StatModifierType.Increased;
        [SerializeField] private float valuePerStack = -8f;
        [SerializeField, Min(1)] private int maximumStacks = 5;
        [SerializeField, Min(0.01f)] private float duration = 8f;

        public bool Enabled => enabled;

        public bool TryCreateDefinition(
            out TimedStackingStatEffectDefinition definition)
        {
            definition = null;
            if (!enabled)
                return true;

            try
            {
                definition = new TimedStackingStatEffectDefinition(
                    effectId,
                    statType,
                    modifierType,
                    valuePerStack,
                    maximumStacks,
                    duration);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}
