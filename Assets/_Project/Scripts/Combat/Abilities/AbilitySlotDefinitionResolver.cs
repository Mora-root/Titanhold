using System;

namespace Titanhold.Combat.Abilities
{
    public interface IAbilitySlotSource
    {
        int SlotCount { get; }
        bool TryGetAbilitySlot(int slotIndex, out string abilityId);
    }

    public sealed class AbilitySlotDefinitionResolver
    {
        private readonly IAbilitySlotSource slots;
        private readonly IAbilityDefinitionResolver definitions;

        public AbilitySlotDefinitionResolver(
            IAbilitySlotSource slots,
            IAbilityDefinitionResolver definitions)
        {
            this.slots = slots ??
                throw new ArgumentNullException(nameof(slots));
            this.definitions = definitions ??
                throw new ArgumentNullException(nameof(definitions));
            if (slots.SlotCount <= 0)
                throw new ArgumentException(
                    "Ability slot source must contain at least one slot.",
                    nameof(slots));
        }

        public int SlotCount => slots.SlotCount;

        public bool TryResolve(
            int slotIndex,
            out IAbilityDefinition definition)
        {
            definition = null;
            return slots.TryGetAbilitySlot(slotIndex, out string abilityId) &&
                   !string.IsNullOrWhiteSpace(abilityId) &&
                   definitions.TryResolve(abilityId, out definition) &&
                   definition != null &&
                   string.Equals(
                       definition.AbilityId,
                       abilityId,
                       StringComparison.Ordinal);
        }
    }
}
