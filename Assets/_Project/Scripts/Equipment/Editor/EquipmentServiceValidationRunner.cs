using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class EquipmentServiceValidationRunner
{
    private const string MenuPath = "Tools/Titanhold/Validate EquipmentService";
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
            Debug.LogError($"EquipmentService validation failed: {exception}");
        }
    }

    public static string RunValidation()
    {
        List<ItemDefinition> definitions = new List<ItemDefinition>();
        List<GameObject> gameObjects = new List<GameObject>();

        try
        {
            ItemDefinition oneHandSword = CreateDefinition(
                definitions,
                "equipment_service_one_hand_sword",
                "Equipment Service One-Hand Sword",
                ItemCategory.Equipment,
                1,
                EquipmentSlotType.Weapon,
                WeaponType.OneHandSword);

            ItemDefinition shield = CreateDefinition(
                definitions,
                "equipment_service_shield",
                "Equipment Service Shield",
                ItemCategory.Equipment,
                1,
                EquipmentSlotType.Shield);

            ItemDefinition twoHandWeapon = CreateDefinition(
                definitions,
                "equipment_service_two_hand_weapon",
                "Equipment Service Two-Hand Weapon",
                ItemCategory.Equipment,
                1,
                EquipmentSlotType.Weapon,
                WeaponType.TwoHandSword);

            ItemDefinition ring = CreateDefinition(
                definitions,
                "equipment_service_ring",
                "Equipment Service Ring",
                ItemCategory.Equipment,
                1,
                EquipmentSlotType.Ring);

            ItemDefinition artifact = CreateDefinition(
                definitions,
                "equipment_service_artifact",
                "Equipment Service Artifact",
                ItemCategory.Equipment,
                1,
                EquipmentSlotType.Artifact);

            ItemDefinition nonEquippable = CreateDefinition(
                definitions,
                "equipment_service_misc",
                "Equipment Service Misc",
                ItemCategory.Misc,
                1,
                EquipmentSlotType.None);

            ItemDefinition stackableEquipment = CreateDefinition(
                definitions,
                "equipment_service_stackable_equipment",
                "Equipment Service Stackable Equipment",
                ItemCategory.Equipment,
                99,
                EquipmentSlotType.Weapon,
                WeaponType.OneHandSword);

            ValidateEquipShieldAndTwoHandReplacement(gameObjects, oneHandSword, shield, twoHandWeapon);
            ValidateRingReplacement(gameObjects, ring);
            ValidateUnequipPreservesIdentity(gameObjects, artifact);
            ValidateFullInventoryPreventsUnequip(gameObjects, oneHandSword, shield);
            ValidateRejectedItems(gameObjects, stackableEquipment, nonEquippable);

            return "EquipmentService validation passed.";
        }
        finally
        {
            foreach (GameObject gameObject in gameObjects)
            {
                if (gameObject != null)
                    UnityEngine.Object.DestroyImmediate(gameObject);
            }

            foreach (ItemDefinition definition in definitions)
            {
                if (definition != null)
                    UnityEngine.Object.DestroyImmediate(definition);
            }
        }
    }

    private static void ValidateEquipShieldAndTwoHandReplacement(
        List<GameObject> gameObjects,
        ItemDefinition oneHandSword,
        ItemDefinition shield,
        ItemDefinition twoHandWeapon)
    {
        PlayerInventory inventory = CreateInventory(gameObjects, 4);
        CharacterEquipment equipment = new CharacterEquipment();
        EquipmentService service = new EquipmentService(inventory, equipment);
        ItemInstance swordInstance = new ItemInstance(oneHandSword);
        ItemInstance shieldInstance = new ItemInstance(shield);
        ItemInstance twoHandInstance = new ItemInstance(twoHandWeapon);
        string swordId = swordInstance.InstanceId;
        string shieldId = shieldInstance.InstanceId;
        string twoHandId = twoHandInstance.InstanceId;

        Assert(inventory.TryAddInstance(swordInstance).FullyAdded, "Could not add sword to inventory.");
        EquipmentOperationResult swordResult = service.TryEquipFromInventory(ItemCategory.Equipment, 0);

        Assert(swordResult.Success, "One-hand sword equip failed.");
        Assert(ReferenceEquals(equipment.GetEquipped(EquipmentSlotId.MainHand), swordInstance), "MainHand should contain the same sword instance.");
        Assert(inventory.GetSlot(ItemCategory.Equipment, 0).IsEmpty, "Sword source inventory slot should be empty.");
        Assert(equipment.GetEquipped(EquipmentSlotId.MainHand).InstanceId == swordId, "Sword instance id changed.");

        Assert(inventory.TryAddInstance(shieldInstance).FullyAdded, "Could not add shield to inventory.");
        EquipmentOperationResult shieldResult = service.TryEquipFromInventory(ItemCategory.Equipment, 0);

        Assert(shieldResult.Success, "Shield equip failed.");
        Assert(ReferenceEquals(equipment.GetEquipped(EquipmentSlotId.OffHand), shieldInstance), "OffHand should contain the same shield instance.");
        Assert(equipment.GetEquipped(EquipmentSlotId.OffHand).InstanceId == shieldId, "Shield instance id changed.");

        Assert(inventory.TryAddInstance(twoHandInstance).FullyAdded, "Could not add two-hand weapon to inventory.");
        EquipmentOperationResult twoHandResult = service.TryEquipFromInventory(ItemCategory.Equipment, 0);

        Assert(twoHandResult.Success, "Two-hand weapon equip failed.");
        Assert(ReferenceEquals(equipment.GetEquipped(EquipmentSlotId.MainHand), twoHandInstance), "MainHand should contain the same two-hand instance.");
        Assert(equipment.GetEquipped(EquipmentSlotId.OffHand) == null, "OffHand should be empty after equipping a two-hand weapon.");
        Assert(twoHandResult.UnequippedInstances.Count == 2, "Two-hand equip should unequip sword and shield.");
        Assert(ContainsInstance(inventory, swordId), "Old sword should return to inventory with same id.");
        Assert(ContainsInstance(inventory, shieldId), "Shield should return to inventory with same id.");
        Assert(equipment.GetEquipped(EquipmentSlotId.MainHand).InstanceId == twoHandId, "Two-hand instance id changed.");
    }

    private static void ValidateRingReplacement(List<GameObject> gameObjects, ItemDefinition ring)
    {
        PlayerInventory inventory = CreateInventory(gameObjects, 4);
        CharacterEquipment equipment = new CharacterEquipment();
        EquipmentService service = new EquipmentService(inventory, equipment);
        ItemInstance firstRing = new ItemInstance(ring);
        ItemInstance secondRing = new ItemInstance(ring);
        ItemInstance thirdRing = new ItemInstance(ring);
        string firstRingId = firstRing.InstanceId;

        Assert(inventory.TryAddInstance(firstRing).FullyAdded, "Could not add first ring.");
        Assert(service.TryEquipFromInventory(ItemCategory.Equipment, 0).Success, "First ring equip failed.");
        Assert(ReferenceEquals(equipment.GetEquipped(EquipmentSlotId.Ring1), firstRing), "First ring should equip into Ring1.");

        Assert(inventory.TryAddInstance(secondRing).FullyAdded, "Could not add second ring.");
        Assert(service.TryEquipFromInventory(ItemCategory.Equipment, 0).Success, "Second ring equip failed.");
        Assert(ReferenceEquals(equipment.GetEquipped(EquipmentSlotId.Ring2), secondRing), "Second ring should equip into Ring2.");

        Assert(inventory.TryAddInstance(thirdRing).FullyAdded, "Could not add third ring.");
        EquipmentOperationResult thirdResult = service.TryEquipFromInventory(ItemCategory.Equipment, 0);

        Assert(thirdResult.Success, "Third ring replacement failed.");
        Assert(ReferenceEquals(equipment.GetEquipped(EquipmentSlotId.Ring1), thirdRing), "Third ring should replace Ring1.");
        Assert(ReferenceEquals(equipment.GetEquipped(EquipmentSlotId.Ring2), secondRing), "Ring2 should remain unchanged.");
        Assert(ContainsInstance(inventory, firstRingId), "Replaced first ring should return to inventory with same id.");
    }

    private static void ValidateUnequipPreservesIdentity(List<GameObject> gameObjects, ItemDefinition artifact)
    {
        PlayerInventory inventory = CreateInventory(gameObjects, 4);
        CharacterEquipment equipment = new CharacterEquipment();
        EquipmentService service = new EquipmentService(inventory, equipment);
        ItemInstance artifactInstance = new ItemInstance(artifact);
        string artifactId = artifactInstance.InstanceId;

        Assert(inventory.TryAddInstance(artifactInstance).FullyAdded, "Could not add artifact.");
        Assert(service.TryEquipFromInventory(ItemCategory.Equipment, 0).Success, "Artifact equip failed.");

        EquipmentOperationResult result = service.TryUnequipToInventory(EquipmentSlotId.Artifact);

        Assert(result.Success, "Artifact unequip failed.");
        Assert(equipment.GetEquipped(EquipmentSlotId.Artifact) == null, "Artifact slot should be empty after unequip.");
        Assert(ContainsInstance(inventory, artifactId), "Unequipped artifact should return to inventory with same id.");
    }

    private static void ValidateFullInventoryPreventsUnequip(
        List<GameObject> gameObjects,
        ItemDefinition oneHandSword,
        ItemDefinition shield)
    {
        PlayerInventory inventory = CreateInventory(gameObjects, 1);
        CharacterEquipment equipment = new CharacterEquipment();
        EquipmentService service = new EquipmentService(inventory, equipment);
        ItemInstance equippedSword = new ItemInstance(oneHandSword);
        ItemInstance inventoryShield = new ItemInstance(shield);

        Assert(equipment.TrySetSlot(EquipmentSlotId.MainHand, equippedSword), "Could not prepare equipped sword.");
        Assert(inventory.TryAddInstance(inventoryShield).FullyAdded, "Could not fill inventory.");

        EquipmentOperationResult result = service.TryUnequipToInventory(EquipmentSlotId.MainHand);

        Assert(!result.Success, "Full inventory should prevent unequip.");
        Assert(result.Error == EquipmentOperationError.InventoryFull, "Full inventory unequip should return InventoryFull.");
        Assert(ReferenceEquals(equipment.GetEquipped(EquipmentSlotId.MainHand), equippedSword), "Equipment should remain unchanged after failed unequip.");
    }

    private static void ValidateRejectedItems(
        List<GameObject> gameObjects,
        ItemDefinition stackableEquipment,
        ItemDefinition nonEquippable)
    {
        PlayerInventory stackableInventory = CreateInventory(gameObjects, 4);
        CharacterEquipment stackableEquipmentModel = new CharacterEquipment();
        EquipmentService stackableService = new EquipmentService(stackableInventory, stackableEquipmentModel);

        Assert(stackableInventory.TryAdd(stackableEquipment, 1).FullyAdded, "Could not add stackable equipment test item.");
        EquipmentOperationResult stackableResult = stackableService.TryEquipFromInventory(ItemCategory.Equipment, 0);

        Assert(!stackableResult.Success, "Stackable item should not equip.");
        Assert(stackableResult.Error == EquipmentOperationError.StackableItemCannotBeEquipped, "Stackable item should return StackableItemCannotBeEquipped.");

        PlayerInventory nonEquippableInventory = CreateInventory(gameObjects, 4);
        CharacterEquipment nonEquippableEquipment = new CharacterEquipment();
        EquipmentService nonEquippableService = new EquipmentService(nonEquippableInventory, nonEquippableEquipment);
        ItemInstance miscInstance = new ItemInstance(nonEquippable);

        Assert(nonEquippableInventory.TryAddInstance(miscInstance).FullyAdded, "Could not add non-equippable item.");
        EquipmentOperationResult nonEquippableResult = nonEquippableService.TryEquipFromInventory(ItemCategory.Misc, 0);

        Assert(!nonEquippableResult.Success, "Non-equippable item should not equip.");
        Assert(nonEquippableResult.Error == EquipmentOperationError.ItemNotEquippable, "Non-equippable item should return ItemNotEquippable.");
    }

    private static PlayerInventory CreateInventory(List<GameObject> gameObjects, int equipmentCapacity)
    {
        GameObject gameObject = new GameObject("EquipmentServiceValidationInventory");
        gameObjects.Add(gameObject);

        PlayerInventory inventory = gameObject.AddComponent<PlayerInventory>();
        SetField(inventory, "equipmentCapacity", equipmentCapacity);
        SetField<PlayerInventory, ItemContainer>(inventory, "container", null);
        inventory.EnsureInitialized();
        return inventory;
    }

    private static bool ContainsInstance(PlayerInventory inventory, string instanceId)
    {
        ItemContainerSection section = inventory.GetSection(ItemCategory.Equipment);
        if (section == null)
            return false;

        for (int i = 0; i < section.Slots.Length; i++)
        {
            ItemStack stack = section.Slots[i].Stack;
            if (stack?.Instance != null && stack.Instance.InstanceId == instanceId)
                return true;
        }

        return false;
    }

    private static ItemDefinition CreateDefinition(
        List<ItemDefinition> definitions,
        string id,
        string displayName,
        ItemCategory category,
        int maxStack,
        EquipmentSlotType equipmentSlotType,
        WeaponType weaponType = WeaponType.None)
    {
        ItemDefinition definition = ScriptableObject.CreateInstance<ItemDefinition>();
        definition.name = displayName;

        SetField(definition, "id", id);
        SetField(definition, "displayName", displayName);
        SetField(definition, "shortName", displayName);
        SetField(definition, "category", category);
        SetField(definition, "maxStack", maxStack);
        SetField(definition, "equipmentSlotType", equipmentSlotType);
        SetField(definition, "weaponType", weaponType);

        definitions.Add(definition);
        return definition;
    }

    private static void SetField<TTarget, TValue>(TTarget target, string fieldName, TValue value)
    {
        FieldInfo field = typeof(TTarget).GetField(fieldName, FieldFlags);

        if (field == null)
            throw new MissingFieldException(typeof(TTarget).Name, fieldName);

        field.SetValue(target, value);
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
