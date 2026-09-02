using System;
using System.Collections.Generic;
using UnityEngine;

namespace Titanhold.Session
{
    [Serializable]
    public sealed class ItemInstanceSnapshot
    {
        [SerializeField] private string definitionId;
        [SerializeField] private string instanceId;
        [SerializeField] private StatModifierData[] generatedModifiers;

        public ItemInstanceSnapshot(
            string definitionId,
            string instanceId,
            IReadOnlyList<StatModifierData> generatedModifiers)
        {
            this.definitionId = definitionId?.Trim() ?? string.Empty;
            this.instanceId = instanceId?.Trim() ?? string.Empty;

            int count = generatedModifiers?.Count ?? 0;
            this.generatedModifiers = new StatModifierData[count];
            for (int i = 0; i < count; i++)
                this.generatedModifiers[i] = generatedModifiers[i];
        }

        public string DefinitionId => definitionId;
        public string InstanceId => instanceId;
        public IReadOnlyList<StatModifierData> GeneratedModifiers =>
            Array.AsReadOnly(generatedModifiers ?? Array.Empty<StatModifierData>());
    }

    [Serializable]
    public sealed class ItemStackSnapshot
    {
        [SerializeField] private string definitionId;
        [SerializeField] private int amount;
        [SerializeField] private bool hasInstance;
        [SerializeField] private ItemInstanceSnapshot instance;

        public ItemStackSnapshot(
            string definitionId,
            int amount,
            ItemInstanceSnapshot instance = null)
        {
            this.definitionId = definitionId?.Trim() ?? string.Empty;
            this.amount = amount;
            hasInstance = instance != null;
            this.instance = instance;
        }

        public string DefinitionId => definitionId;
        public int Amount => amount;
        public bool HasInstance => hasInstance;
        public ItemInstanceSnapshot Instance => instance;
    }

    [Serializable]
    public sealed class InventorySlotSnapshot
    {
        [SerializeField] private ItemCategory category;
        [SerializeField] private int slotIndex;
        [SerializeField] private ItemStackSnapshot stack;

        public InventorySlotSnapshot(
            ItemCategory category,
            int slotIndex,
            ItemStackSnapshot stack)
        {
            this.category = category;
            this.slotIndex = slotIndex;
            this.stack = stack;
        }

        public ItemCategory Category => category;
        public int SlotIndex => slotIndex;
        public ItemStackSnapshot Stack => stack;
    }

    [Serializable]
    public sealed class EquipmentSlotSnapshot
    {
        [SerializeField] private EquipmentSlotId slotId;
        [SerializeField] private ItemInstanceSnapshot item;

        public EquipmentSlotSnapshot(
            EquipmentSlotId slotId,
            ItemInstanceSnapshot item)
        {
            this.slotId = slotId;
            this.item = item;
        }

        public EquipmentSlotId SlotId => slotId;
        public ItemInstanceSnapshot Item => item;
    }

    [Serializable]
    public sealed class CharacterSnapshot
    {
        public const int CurrentSchemaVersion = 1;

        [SerializeField] private int schemaVersion;
        [SerializeField] private string characterId;
        [SerializeField] private int level;
        [SerializeField] private int experience;
        [SerializeField] private int gold;
        [SerializeField] private InventorySlotSnapshot[] inventorySlots;
        [SerializeField] private EquipmentSlotSnapshot[] equipmentSlots;

        public CharacterSnapshot(
            string characterId,
            int level,
            int experience,
            int gold,
            IReadOnlyList<InventorySlotSnapshot> inventorySlots,
            IReadOnlyList<EquipmentSlotSnapshot> equipmentSlots,
            int schemaVersion = CurrentSchemaVersion)
        {
            this.schemaVersion = schemaVersion;
            this.characterId = characterId?.Trim() ?? string.Empty;
            this.level = level;
            this.experience = experience;
            this.gold = gold;
            this.inventorySlots = Copy(inventorySlots);
            this.equipmentSlots = Copy(equipmentSlots);
        }

        public int SchemaVersion => schemaVersion;
        public string CharacterId => characterId;
        public int Level => level;
        public int Experience => experience;
        public int Gold => gold;
        public IReadOnlyList<InventorySlotSnapshot> InventorySlots =>
            Array.AsReadOnly(inventorySlots ?? Array.Empty<InventorySlotSnapshot>());
        public IReadOnlyList<EquipmentSlotSnapshot> EquipmentSlots =>
            Array.AsReadOnly(equipmentSlots ?? Array.Empty<EquipmentSlotSnapshot>());

        private static T[] Copy<T>(IReadOnlyList<T> source)
        {
            int count = source?.Count ?? 0;
            T[] copy = new T[count];
            for (int i = 0; i < count; i++)
                copy[i] = source[i];

            return copy;
        }
    }
}
