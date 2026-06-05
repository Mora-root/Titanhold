using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class CharacterEquipmentValidationRunner
{
    private const string MenuPath = "Tools/Titanhold/Validate CharacterEquipment";
    private static readonly BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.NonPublic;

    [MenuItem(MenuPath)]
    public static void ValidateFromMenu()
    {
        try
        {
            string report = RunValidation();
            Debug.Log(report);
        }
        catch (Exception exception)
        {
            Debug.LogError($"CharacterEquipment validation failed: {exception}");
        }
    }

    public static string RunValidation()
    {
        List<ItemDefinition> definitions = new List<ItemDefinition>();

        try
        {
            ItemDefinition sword = CreateDefinition(
                definitions,
                "character_equipment_sword_test",
                "Character Equipment Sword Test",
                ItemCategory.Equipment,
                EquipmentSlotType.Weapon,
                WeaponType.OneHandSword);

            ItemDefinition secondSword = CreateDefinition(
                definitions,
                "character_equipment_second_sword_test",
                "Character Equipment Second Sword Test",
                ItemCategory.Equipment,
                EquipmentSlotType.Weapon,
                WeaponType.OneHandSword);

            ItemDefinition shield = CreateDefinition(
                definitions,
                "character_equipment_shield_test",
                "Character Equipment Shield Test",
                ItemCategory.Equipment,
                EquipmentSlotType.Shield);

            ItemDefinition ring = CreateDefinition(
                definitions,
                "character_equipment_ring_test",
                "Character Equipment Ring Test",
                ItemCategory.Equipment,
                EquipmentSlotType.Ring);

            ItemDefinition nonEquippable = CreateDefinition(
                definitions,
                "character_equipment_misc_test",
                "Character Equipment Misc Test",
                ItemCategory.Misc,
                EquipmentSlotType.None);

            ValidateSetGetClear(sword, secondSword);
            ValidateRejectedItems(nonEquippable, shield, ring);
            ValidateClearEvents(sword, shield, ring);

            return "CharacterEquipment validation passed.";
        }
        finally
        {
            foreach (ItemDefinition definition in definitions)
            {
                if (definition != null)
                    UnityEngine.Object.DestroyImmediate(definition);
            }
        }
    }

    private static void ValidateSetGetClear(ItemDefinition sword, ItemDefinition secondSword)
    {
        CharacterEquipment equipment = new CharacterEquipment();
        ItemInstance swordInstance = new ItemInstance(sword);
        ItemInstance secondSwordInstance = new ItemInstance(secondSword);
        int changedCount = 0;
        int slotChangedCount = 0;
        EquipmentSlotId changedSlot = default;
        ItemInstance changedOldItem = null;
        ItemInstance changedNewItem = null;

        equipment.Changed += () => changedCount++;
        equipment.SlotChanged += (slotId, oldItem, newItem) =>
        {
            slotChangedCount++;
            changedSlot = slotId;
            changedOldItem = oldItem;
            changedNewItem = newItem;
        };

        Assert(equipment.TrySetSlot(EquipmentSlotId.MainHand, swordInstance), "Sword should equip into MainHand.");
        Assert(ReferenceEquals(equipment.GetEquipped(EquipmentSlotId.MainHand), swordInstance), "GetEquipped should return the same sword instance.");
        Assert(ReferenceEquals(equipment.GetDefinition(EquipmentSlotId.MainHand), sword), "GetDefinition should return sword definition.");
        Assert(equipment.IsOccupied(EquipmentSlotId.MainHand), "MainHand should be occupied.");
        Assert(changedCount == 1, "Changed should fire once after TrySetSlot.");
        Assert(slotChangedCount == 1, "SlotChanged should fire once after TrySetSlot.");
        Assert(changedSlot == EquipmentSlotId.MainHand, "SlotChanged slot mismatch after TrySetSlot.");
        Assert(changedOldItem == null, "SlotChanged old item should be null after TrySetSlot.");
        Assert(ReferenceEquals(changedNewItem, swordInstance), "SlotChanged new item should be sword instance.");

        Assert(!equipment.TrySetSlot(EquipmentSlotId.MainHand, secondSwordInstance), "Occupied MainHand should reject another item.");
        Assert(ReferenceEquals(equipment.GetEquipped(EquipmentSlotId.MainHand), swordInstance), "Occupied slot rejection should not replace item.");
        Assert(changedCount == 1, "Changed should not fire after rejected occupied slot set.");
        Assert(slotChangedCount == 1, "SlotChanged should not fire after rejected occupied slot set.");

        ItemInstance cleared = equipment.ClearSlot(EquipmentSlotId.MainHand);
        Assert(ReferenceEquals(cleared, swordInstance), "ClearSlot should return the same original sword instance.");
        Assert(!equipment.IsOccupied(EquipmentSlotId.MainHand), "MainHand should be empty after ClearSlot.");
        Assert(changedCount == 2, "Changed should fire once after ClearSlot.");
        Assert(slotChangedCount == 2, "SlotChanged should fire once after ClearSlot.");
        Assert(changedSlot == EquipmentSlotId.MainHand, "SlotChanged slot mismatch after ClearSlot.");
        Assert(ReferenceEquals(changedOldItem, swordInstance), "SlotChanged old item should be sword instance after ClearSlot.");
        Assert(changedNewItem == null, "SlotChanged new item should be null after ClearSlot.");
    }

    private static void ValidateRejectedItems(
        ItemDefinition nonEquippable,
        ItemDefinition shield,
        ItemDefinition ring)
    {
        CharacterEquipment equipment = new CharacterEquipment();

        Assert(!equipment.TrySetSlot(EquipmentSlotId.MainHand, null), "Null instance should be rejected.");
        Assert(!equipment.TrySetSlot(EquipmentSlotId.MainHand, new ItemInstance(nonEquippable)), "Non-equippable item should be rejected.");
        Assert(!equipment.TrySetSlot(EquipmentSlotId.MainHand, new ItemInstance(shield)), "Shield should be rejected in MainHand.");
        Assert(equipment.TrySetSlot(EquipmentSlotId.OffHand, new ItemInstance(shield)), "Shield should be accepted in OffHand.");
        Assert(!equipment.TrySetSlot(EquipmentSlotId.Head, new ItemInstance(ring)), "Ring should be rejected in Head.");
        Assert(equipment.TrySetSlot(EquipmentSlotId.Ring1, new ItemInstance(ring)), "Ring should be accepted in Ring1.");
    }

    private static void ValidateClearEvents(
        ItemDefinition sword,
        ItemDefinition shield,
        ItemDefinition ring)
    {
        CharacterEquipment equipment = new CharacterEquipment();
        ItemInstance swordInstance = new ItemInstance(sword);
        ItemInstance shieldInstance = new ItemInstance(shield);
        ItemInstance ringInstance = new ItemInstance(ring);

        Assert(equipment.TrySetSlot(EquipmentSlotId.MainHand, swordInstance), "Could not prepare MainHand for Clear test.");
        Assert(equipment.TrySetSlot(EquipmentSlotId.OffHand, shieldInstance), "Could not prepare OffHand for Clear test.");
        Assert(equipment.TrySetSlot(EquipmentSlotId.Ring1, ringInstance), "Could not prepare Ring1 for Clear test.");

        int changedCount = 0;
        int slotChangedCount = 0;
        HashSet<EquipmentSlotId> clearedSlots = new HashSet<EquipmentSlotId>();

        equipment.Changed += () => changedCount++;
        equipment.SlotChanged += (slotId, oldItem, newItem) =>
        {
            Assert(oldItem != null, "Clear SlotChanged old item should not be null.");
            Assert(newItem == null, "Clear SlotChanged new item should be null.");
            slotChangedCount++;
            clearedSlots.Add(slotId);
        };

        equipment.Clear();

        Assert(changedCount == 1, "Clear should fire Changed once.");
        Assert(slotChangedCount == 3, "Clear should fire SlotChanged for every occupied slot.");
        Assert(clearedSlots.Contains(EquipmentSlotId.MainHand), "Clear should report MainHand.");
        Assert(clearedSlots.Contains(EquipmentSlotId.OffHand), "Clear should report OffHand.");
        Assert(clearedSlots.Contains(EquipmentSlotId.Ring1), "Clear should report Ring1.");
        Assert(!equipment.IsOccupied(EquipmentSlotId.MainHand), "MainHand should be empty after Clear.");
        Assert(!equipment.IsOccupied(EquipmentSlotId.OffHand), "OffHand should be empty after Clear.");
        Assert(!equipment.IsOccupied(EquipmentSlotId.Ring1), "Ring1 should be empty after Clear.");

        changedCount = 0;
        slotChangedCount = 0;
        equipment.Clear();

        Assert(changedCount == 0, "Clear on empty equipment should not fire Changed.");
        Assert(slotChangedCount == 0, "Clear on empty equipment should not fire SlotChanged.");
    }

    private static ItemDefinition CreateDefinition(
        List<ItemDefinition> definitions,
        string id,
        string displayName,
        ItemCategory category,
        EquipmentSlotType equipmentSlotType,
        WeaponType weaponType = WeaponType.None)
    {
        ItemDefinition definition = ScriptableObject.CreateInstance<ItemDefinition>();
        definition.name = displayName;

        SetField(definition, "id", id);
        SetField(definition, "displayName", displayName);
        SetField(definition, "shortName", displayName);
        SetField(definition, "category", category);
        SetField(definition, "maxStack", 1);
        SetField(definition, "equipmentSlotType", equipmentSlotType);
        SetField(definition, "weaponType", weaponType);

        definitions.Add(definition);
        return definition;
    }

    private static void SetField<T>(ItemDefinition definition, string fieldName, T value)
    {
        FieldInfo field = typeof(ItemDefinition).GetField(fieldName, FieldFlags);

        if (field == null)
            throw new MissingFieldException(typeof(ItemDefinition).Name, fieldName);

        field.SetValue(definition, value);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
