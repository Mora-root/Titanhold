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

            ItemDefinition oneHandAxe = CreateDefinition(
                definitions,
                "equipment_service_one_hand_axe",
                "Equipment Service One-Hand Axe",
                ItemCategory.Equipment,
                1,
                EquipmentSlotType.Weapon,
                WeaponType.OneHandAxe);

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

            ItemDefinition miscWeaponLike = CreateDefinition(
                definitions,
                "equipment_service_misc_weapon_like",
                "Equipment Service Misc Weapon-Like",
                ItemCategory.Misc,
                1,
                EquipmentSlotType.Weapon,
                WeaponType.OneHandSword);

            ItemDefinition stackableEquipment = CreateDefinition(
                definitions,
                "equipment_service_stackable_equipment",
                "Equipment Service Stackable Equipment",
                ItemCategory.Equipment,
                99,
                EquipmentSlotType.Weapon,
                WeaponType.OneHandSword);

            ValidateCompatibleOneHandSequence(gameObjects, oneHandSword);
            ValidateCompatibleOneHandSwap(gameObjects, oneHandSword);
            ValidateIncompatibleOneHandSequence(gameObjects, oneHandSword, oneHandAxe);
            ValidateReverseIncompatibleOneHandSequence(gameObjects, oneHandSword, oneHandAxe);
            ValidateShieldKeepsMainHand(gameObjects, oneHandSword, shield);
            ValidateTwoHandClearsBothHands(gameObjects, oneHandSword, twoHandWeapon);
            ValidateEquipShieldAndTwoHandReplacement(gameObjects, oneHandSword, shield, twoHandWeapon);
            ValidatePreferredOffHandRejectsIncompatibleOneHand(gameObjects, oneHandSword, oneHandAxe);
            ValidatePreferredOffHandRejectsEmptyMainHand(gameObjects, oneHandSword);
            ValidateRingReplacement(gameObjects, ring);
            ValidateUnequipPreservesIdentity(gameObjects, artifact);
            ValidateUnequipMainHandNormalizesDualWield(gameObjects, oneHandSword);
            ValidateUnequipMainHandKeepsShield(gameObjects, oneHandSword, shield);
            ValidateFullInventoryPreventsDualWieldMainHandUnequip(gameObjects, oneHandSword, shield);
            ValidateUnequipOffHandWeaponKeepsMainHand(gameObjects, oneHandSword);
            ValidateUnequipToSpecificInventorySlot(gameObjects, oneHandSword);
            ValidateUnequipMainHandNormalizesDualWieldToSpecificSlot(gameObjects, oneHandSword);
            ValidateUnequipToOccupiedInventorySlotFails(gameObjects, oneHandSword, shield);
            ValidateUnequipToWrongInventoryCategoryFails(gameObjects, oneHandSword);
            ValidateReplacementReturnsToSourceSlot(gameObjects, oneHandSword);
            ValidateTwoHandReplacementReturnsFirstConflictToSourceSlot(gameObjects, oneHandSword, shield, twoHandWeapon);
            ValidateIncompatibleDualWieldReplacementReturnsFirstConflictToSourceSlot(gameObjects, oneHandSword, oneHandAxe);
            ValidateCompatibleDualWieldReplacementReturnsMainHandToSourceSlot(gameObjects, oneHandSword);
            ValidateReplacementFailsBeforeMutationWhenAdditionalConflictCannotReturn(gameObjects, oneHandSword, shield, twoHandWeapon);
            ValidateFullInventoryPreventsUnequip(gameObjects, oneHandSword, shield);
            ValidateRejectedItems(gameObjects, stackableEquipment, nonEquippable, miscWeaponLike);

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

    private static void ValidateCompatibleOneHandSequence(List<GameObject> gameObjects, ItemDefinition oneHandSword)
    {
        PlayerInventory inventory = CreateInventory(gameObjects, 5);
        CharacterEquipment equipment = new CharacterEquipment();
        EquipmentService service = new EquipmentService(inventory, equipment);
        ItemInstance swordA = AddAndEquip(inventory, service, oneHandSword);
        ItemInstance swordB = AddAndEquip(inventory, service, oneHandSword);
        string swordAId = swordA.InstanceId;

        Assert(ReferenceEquals(equipment.GetEquipped(EquipmentSlotId.MainHand), swordA), "First compatible one-hand weapon should equip into MainHand.");
        Assert(ReferenceEquals(equipment.GetEquipped(EquipmentSlotId.OffHand), swordB), "Second compatible one-hand weapon should equip into OffHand.");

        ItemInstance swordC = new ItemInstance(oneHandSword);
        Assert(inventory.TryAddInstance(swordC).FullyAdded, "Could not add third compatible one-hand weapon.");
        EquipmentOperationResult result = service.TryEquipFromInventory(ItemCategory.Equipment, 0);

        Assert(result.Success, "Third compatible one-hand weapon equip failed.");
        Assert(ReferenceEquals(equipment.GetEquipped(EquipmentSlotId.MainHand), swordC), "Third compatible one-hand weapon should replace MainHand.");
        Assert(ReferenceEquals(equipment.GetEquipped(EquipmentSlotId.OffHand), swordB), "OffHand compatible weapon should remain equipped.");
        Assert(ContainsInstance(inventory, swordAId), "Replaced MainHand compatible weapon should return to inventory.");
        Assert(!ContainsInstance(inventory, swordC.InstanceId), "Equipped third weapon should not remain in inventory.");
    }

    private static void ValidateCompatibleOneHandSwap(List<GameObject> gameObjects, ItemDefinition oneHandSword)
    {
        PlayerInventory inventory = CreateInventory(gameObjects, 2);
        CharacterEquipment equipment = new CharacterEquipment();
        EquipmentService service = new EquipmentService(inventory, equipment);
        ItemInstance swordA = AddAndEquip(inventory, service, oneHandSword);
        ItemInstance swordB = AddAndEquip(inventory, service, oneHandSword);
        string swordAId = swordA.InstanceId;
        string swordBId = swordB.InstanceId;

        EquipmentOperationResult result = service.TrySwapEquippedSlots(
            EquipmentSlotId.MainHand,
            EquipmentSlotId.OffHand);

        Assert(result.Success, "Compatible one-handed weapon swap failed.");
        Assert(ReferenceEquals(equipment.GetEquipped(EquipmentSlotId.MainHand), swordB), "OffHand sword should move to MainHand.");
        Assert(ReferenceEquals(equipment.GetEquipped(EquipmentSlotId.OffHand), swordA), "MainHand sword should move to OffHand.");
        Assert(equipment.GetEquipped(EquipmentSlotId.MainHand).InstanceId == swordBId, "MainHand swap should preserve ItemInstance identity.");
        Assert(equipment.GetEquipped(EquipmentSlotId.OffHand).InstanceId == swordAId, "OffHand swap should preserve ItemInstance identity.");
    }

    private static void ValidateIncompatibleOneHandSequence(
        List<GameObject> gameObjects,
        ItemDefinition oneHandSword,
        ItemDefinition oneHandAxe)
    {
        PlayerInventory inventory = CreateInventory(gameObjects, 5);
        CharacterEquipment equipment = new CharacterEquipment();
        EquipmentService service = new EquipmentService(inventory, equipment);
        ItemInstance swordA = AddAndEquip(inventory, service, oneHandSword);
        ItemInstance swordB = AddAndEquip(inventory, service, oneHandSword);
        ItemInstance axeC = new ItemInstance(oneHandAxe);
        string swordAId = swordA.InstanceId;
        string swordBId = swordB.InstanceId;

        Assert(inventory.TryAddInstance(axeC).FullyAdded, "Could not add incompatible axe.");
        EquipmentOperationResult result = service.TryEquipFromInventory(ItemCategory.Equipment, 0);

        Assert(result.Success, "Incompatible one-hand weapon equip failed.");
        Assert(ReferenceEquals(equipment.GetEquipped(EquipmentSlotId.MainHand), axeC), "Incompatible axe should equip into MainHand.");
        Assert(equipment.GetEquipped(EquipmentSlotId.OffHand) == null, "OffHand should clear when equipping incompatible one-hand weapon.");
        Assert(ContainsInstance(inventory, swordAId), "Old MainHand sword should return to inventory.");
        Assert(ContainsInstance(inventory, swordBId), "Old OffHand sword should return to inventory.");
        Assert(!ContainsInstance(inventory, axeC.InstanceId), "Equipped incompatible axe should not remain in inventory.");
    }

    private static void ValidateReverseIncompatibleOneHandSequence(
        List<GameObject> gameObjects,
        ItemDefinition oneHandSword,
        ItemDefinition oneHandAxe)
    {
        PlayerInventory inventory = CreateInventory(gameObjects, 5);
        CharacterEquipment equipment = new CharacterEquipment();
        EquipmentService service = new EquipmentService(inventory, equipment);
        ItemInstance axeA = AddAndEquip(inventory, service, oneHandAxe);
        ItemInstance axeB = AddAndEquip(inventory, service, oneHandAxe);
        ItemInstance swordC = new ItemInstance(oneHandSword);
        string axeAId = axeA.InstanceId;
        string axeBId = axeB.InstanceId;

        Assert(inventory.TryAddInstance(swordC).FullyAdded, "Could not add incompatible sword.");
        EquipmentOperationResult result = service.TryEquipFromInventory(ItemCategory.Equipment, 0);

        Assert(result.Success, "Reverse incompatible one-hand weapon equip failed.");
        Assert(ReferenceEquals(equipment.GetEquipped(EquipmentSlotId.MainHand), swordC), "Incompatible sword should equip into MainHand.");
        Assert(equipment.GetEquipped(EquipmentSlotId.OffHand) == null, "OffHand should clear when equipping reverse incompatible one-hand weapon.");
        Assert(ContainsInstance(inventory, axeAId), "Old MainHand axe should return to inventory.");
        Assert(ContainsInstance(inventory, axeBId), "Old OffHand axe should return to inventory.");
    }

    private static void ValidateShieldKeepsMainHand(
        List<GameObject> gameObjects,
        ItemDefinition oneHandSword,
        ItemDefinition shield)
    {
        PlayerInventory inventory = CreateInventory(gameObjects, 5);
        CharacterEquipment equipment = new CharacterEquipment();
        EquipmentService service = new EquipmentService(inventory, equipment);
        ItemInstance swordA = AddAndEquip(inventory, service, oneHandSword);
        ItemInstance swordB = AddAndEquip(inventory, service, oneHandSword);
        ItemInstance shieldInstance = new ItemInstance(shield);
        string swordBId = swordB.InstanceId;

        Assert(inventory.TryAddInstance(shieldInstance).FullyAdded, "Could not add shield for replacement test.");
        EquipmentOperationResult result = service.TryEquipFromInventory(ItemCategory.Equipment, 0);

        Assert(result.Success, "Shield equip over OffHand weapon failed.");
        Assert(ReferenceEquals(equipment.GetEquipped(EquipmentSlotId.MainHand), swordA), "Shield should keep compatible one-hand MainHand weapon.");
        Assert(ReferenceEquals(equipment.GetEquipped(EquipmentSlotId.OffHand), shieldInstance), "Shield should equip into OffHand.");
        Assert(ContainsInstance(inventory, swordBId), "Previous OffHand sword should return to inventory.");
    }

    private static void ValidateTwoHandClearsBothHands(
        List<GameObject> gameObjects,
        ItemDefinition oneHandSword,
        ItemDefinition twoHandWeapon)
    {
        PlayerInventory inventory = CreateInventory(gameObjects, 5);
        CharacterEquipment equipment = new CharacterEquipment();
        EquipmentService service = new EquipmentService(inventory, equipment);
        ItemInstance swordA = AddAndEquip(inventory, service, oneHandSword);
        ItemInstance swordB = AddAndEquip(inventory, service, oneHandSword);
        ItemInstance twoHandInstance = new ItemInstance(twoHandWeapon);
        string swordAId = swordA.InstanceId;
        string swordBId = swordB.InstanceId;

        Assert(inventory.TryAddInstance(twoHandInstance).FullyAdded, "Could not add two-handed weapon for clearing test.");
        EquipmentOperationResult result = service.TryEquipFromInventory(ItemCategory.Equipment, 0);

        Assert(result.Success, "Two-handed weapon equip over dual-wield failed.");
        Assert(ReferenceEquals(equipment.GetEquipped(EquipmentSlotId.MainHand), twoHandInstance), "Two-handed weapon should equip into MainHand.");
        Assert(equipment.GetEquipped(EquipmentSlotId.OffHand) == null, "OffHand should clear after equipping two-handed weapon.");
        Assert(ContainsInstance(inventory, swordAId), "Old MainHand sword should return after two-handed equip.");
        Assert(ContainsInstance(inventory, swordBId), "Old OffHand sword should return after two-handed equip.");
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

    private static void ValidatePreferredOffHandRejectsIncompatibleOneHand(
        List<GameObject> gameObjects,
        ItemDefinition oneHandSword,
        ItemDefinition oneHandAxe)
    {
        PlayerInventory inventory = CreateInventory(gameObjects, 4);
        CharacterEquipment equipment = new CharacterEquipment();
        EquipmentService service = new EquipmentService(inventory, equipment);
        ItemInstance sword = AddAndEquip(inventory, service, oneHandSword);
        ItemInstance axe = new ItemInstance(oneHandAxe);
        string axeId = axe.InstanceId;

        Assert(inventory.TryAddInstance(axe).FullyAdded, "Could not add incompatible axe for preferred OffHand test.");
        EquipmentOperationResult result = service.TryEquipFromInventory(ItemCategory.Equipment, 0, EquipmentSlotId.OffHand);

        Assert(!result.Success, "Preferred OffHand should reject incompatible one-hand weapon.");
        Assert(result.Error == EquipmentOperationError.InvalidTargetSlot, "Preferred OffHand incompatible weapon should return InvalidTargetSlot.");
        Assert(ReferenceEquals(equipment.GetEquipped(EquipmentSlotId.MainHand), sword), "MainHand sword should remain after rejected preferred OffHand.");
        Assert(equipment.GetEquipped(EquipmentSlotId.OffHand) == null, "OffHand should remain empty after rejected preferred OffHand.");
        Assert(ContainsInstance(inventory, axeId), "Rejected incompatible axe should remain in inventory.");
    }

    private static void ValidatePreferredOffHandRejectsEmptyMainHand(
        List<GameObject> gameObjects,
        ItemDefinition oneHandSword)
    {
        PlayerInventory inventory = CreateInventory(gameObjects, 4);
        CharacterEquipment equipment = new CharacterEquipment();
        EquipmentService service = new EquipmentService(inventory, equipment);
        ItemInstance sword = new ItemInstance(oneHandSword);
        string swordId = sword.InstanceId;

        Assert(inventory.TryAddInstance(sword).FullyAdded, "Could not add sword for empty MainHand preferred OffHand test.");
        EquipmentOperationResult result = service.TryEquipFromInventory(ItemCategory.Equipment, 0, EquipmentSlotId.OffHand);

        Assert(!result.Success, "Preferred OffHand should reject one-hand weapon when MainHand is empty.");
        Assert(result.Error == EquipmentOperationError.InvalidTargetSlot, "Empty MainHand preferred OffHand should return InvalidTargetSlot.");
        Assert(equipment.GetEquipped(EquipmentSlotId.MainHand) == null, "MainHand should remain empty after rejected preferred OffHand.");
        Assert(equipment.GetEquipped(EquipmentSlotId.OffHand) == null, "OffHand should remain empty after rejected preferred OffHand.");
        Assert(ContainsInstance(inventory, swordId), "Rejected sword should remain in inventory.");
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

    private static void ValidateUnequipMainHandNormalizesDualWield(
        List<GameObject> gameObjects,
        ItemDefinition oneHandSword)
    {
        PlayerInventory inventory = CreateInventory(gameObjects, 4);
        CharacterEquipment equipment = new CharacterEquipment();
        EquipmentService service = new EquipmentService(inventory, equipment);
        ItemInstance swordA = AddAndEquip(inventory, service, oneHandSword);
        ItemInstance swordB = AddAndEquip(inventory, service, oneHandSword);
        string swordAId = swordA.InstanceId;
        string swordBId = swordB.InstanceId;

        EquipmentOperationResult result = service.TryUnequipToInventory(EquipmentSlotId.MainHand);

        Assert(result.Success, "MainHand unequip should normalize dual-wield state.");
        Assert(ContainsInstance(inventory, swordAId), "Unequipped MainHand sword should return to inventory.");
        Assert(ReferenceEquals(equipment.GetEquipped(EquipmentSlotId.MainHand), swordB), "OffHand sword should move to MainHand.");
        Assert(equipment.GetEquipped(EquipmentSlotId.MainHand).InstanceId == swordBId, "Moved OffHand sword instance id changed.");
        Assert(equipment.GetEquipped(EquipmentSlotId.OffHand) == null, "OffHand should be empty after MainHand normalization.");
        Assert(!ContainsInstance(inventory, swordBId), "Moved OffHand sword should not return to inventory.");
    }

    private static void ValidateUnequipMainHandKeepsShield(
        List<GameObject> gameObjects,
        ItemDefinition oneHandSword,
        ItemDefinition shield)
    {
        PlayerInventory inventory = CreateInventory(gameObjects, 4);
        CharacterEquipment equipment = new CharacterEquipment();
        EquipmentService service = new EquipmentService(inventory, equipment);
        ItemInstance sword = new ItemInstance(oneHandSword);
        ItemInstance shieldInstance = new ItemInstance(shield);
        string swordId = sword.InstanceId;

        Assert(equipment.TrySetSlot(EquipmentSlotId.MainHand, sword), "Could not prepare MainHand sword.");
        Assert(equipment.TrySetSlot(EquipmentSlotId.OffHand, shieldInstance), "Could not prepare OffHand shield.");

        EquipmentOperationResult result = service.TryUnequipToInventory(EquipmentSlotId.MainHand);

        Assert(result.Success, "MainHand sword unequip with shield failed.");
        Assert(ContainsInstance(inventory, swordId), "Unequipped sword should return to inventory.");
        Assert(equipment.GetEquipped(EquipmentSlotId.MainHand) == null, "MainHand should be empty after sword unequip.");
        Assert(ReferenceEquals(equipment.GetEquipped(EquipmentSlotId.OffHand), shieldInstance), "Shield should remain in OffHand.");
    }

    private static void ValidateFullInventoryPreventsDualWieldMainHandUnequip(
        List<GameObject> gameObjects,
        ItemDefinition oneHandSword,
        ItemDefinition shield)
    {
        PlayerInventory inventory = CreateInventory(gameObjects, 1);
        CharacterEquipment equipment = new CharacterEquipment();
        EquipmentService service = new EquipmentService(inventory, equipment);
        ItemInstance swordA = new ItemInstance(oneHandSword);
        ItemInstance swordB = new ItemInstance(oneHandSword);
        ItemInstance inventoryShield = new ItemInstance(shield);

        Assert(equipment.TrySetSlot(EquipmentSlotId.MainHand, swordA), "Could not prepare MainHand sword.");
        Assert(equipment.TrySetSlot(EquipmentSlotId.OffHand, swordB), "Could not prepare OffHand sword.");
        Assert(inventory.TryAddInstance(inventoryShield).FullyAdded, "Could not fill inventory.");

        EquipmentOperationResult result = service.TryUnequipToInventory(EquipmentSlotId.MainHand);

        Assert(!result.Success, "Full inventory should prevent normalized MainHand unequip.");
        Assert(result.Error == EquipmentOperationError.InventoryFull, "Full inventory normalized unequip should return InventoryFull.");
        Assert(ReferenceEquals(equipment.GetEquipped(EquipmentSlotId.MainHand), swordA), "MainHand sword should remain after failed normalized unequip.");
        Assert(ReferenceEquals(equipment.GetEquipped(EquipmentSlotId.OffHand), swordB), "OffHand sword should remain after failed normalized unequip.");
    }

    private static void ValidateUnequipOffHandWeaponKeepsMainHand(
        List<GameObject> gameObjects,
        ItemDefinition oneHandSword)
    {
        PlayerInventory inventory = CreateInventory(gameObjects, 4);
        CharacterEquipment equipment = new CharacterEquipment();
        EquipmentService service = new EquipmentService(inventory, equipment);
        ItemInstance swordA = AddAndEquip(inventory, service, oneHandSword);
        ItemInstance swordB = AddAndEquip(inventory, service, oneHandSword);
        string swordBId = swordB.InstanceId;

        EquipmentOperationResult result = service.TryUnequipToInventory(EquipmentSlotId.OffHand);

        Assert(result.Success, "OffHand sword unequip failed.");
        Assert(ReferenceEquals(equipment.GetEquipped(EquipmentSlotId.MainHand), swordA), "MainHand sword should remain after OffHand unequip.");
        Assert(equipment.GetEquipped(EquipmentSlotId.OffHand) == null, "OffHand should be empty after OffHand unequip.");
        Assert(ContainsInstance(inventory, swordBId), "Unequipped OffHand sword should return to inventory.");
    }

    private static void ValidateUnequipToSpecificInventorySlot(
        List<GameObject> gameObjects,
        ItemDefinition oneHandSword)
    {
        PlayerInventory inventory = CreateInventory(gameObjects, 4);
        CharacterEquipment equipment = new CharacterEquipment();
        EquipmentService service = new EquipmentService(inventory, equipment);
        ItemInstance sword = AddAndEquip(inventory, service, oneHandSword);
        string swordId = sword.InstanceId;

        EquipmentOperationResult result = service.TryUnequipToInventory(EquipmentSlotId.MainHand, ItemCategory.Equipment, 2);

        Assert(result.Success, "Specific-slot unequip failed.");
        Assert(equipment.GetEquipped(EquipmentSlotId.MainHand) == null, "MainHand should be empty after specific-slot unequip.");
        Assert(ReferenceEquals(GetInstanceAt(inventory, ItemCategory.Equipment, 2), sword), "Unequipped sword should land exactly in target slot.");
        Assert(GetInstanceAt(inventory, ItemCategory.Equipment, 2).InstanceId == swordId, "Specific-slot unequip changed instance id.");
    }

    private static void ValidateUnequipMainHandNormalizesDualWieldToSpecificSlot(
        List<GameObject> gameObjects,
        ItemDefinition oneHandSword)
    {
        PlayerInventory inventory = CreateInventory(gameObjects, 5);
        CharacterEquipment equipment = new CharacterEquipment();
        EquipmentService service = new EquipmentService(inventory, equipment);
        ItemInstance swordA = AddAndEquip(inventory, service, oneHandSword);
        ItemInstance swordB = AddAndEquip(inventory, service, oneHandSword);
        string swordAId = swordA.InstanceId;
        string swordBId = swordB.InstanceId;

        EquipmentOperationResult result = service.TryUnequipToInventory(EquipmentSlotId.MainHand, ItemCategory.Equipment, 3);

        Assert(result.Success, "Specific-slot dual-wield normalized unequip failed.");
        Assert(ReferenceEquals(GetInstanceAt(inventory, ItemCategory.Equipment, 3), swordA), "Old MainHand sword should land exactly in target slot.");
        Assert(GetInstanceAt(inventory, ItemCategory.Equipment, 3).InstanceId == swordAId, "Old MainHand sword instance id changed.");
        Assert(ReferenceEquals(equipment.GetEquipped(EquipmentSlotId.MainHand), swordB), "OffHand sword should move to MainHand during specific-slot unequip.");
        Assert(equipment.GetEquipped(EquipmentSlotId.MainHand).InstanceId == swordBId, "Moved OffHand sword instance id changed during specific-slot unequip.");
        Assert(equipment.GetEquipped(EquipmentSlotId.OffHand) == null, "OffHand should clear during specific-slot normalized unequip.");
    }

    private static void ValidateUnequipToOccupiedInventorySlotFails(
        List<GameObject> gameObjects,
        ItemDefinition oneHandSword,
        ItemDefinition shield)
    {
        PlayerInventory inventory = CreateInventory(gameObjects, 4);
        CharacterEquipment equipment = new CharacterEquipment();
        EquipmentService service = new EquipmentService(inventory, equipment);
        ItemInstance sword = new ItemInstance(oneHandSword);
        ItemInstance occupiedShield = new ItemInstance(shield);

        Assert(equipment.TrySetSlot(EquipmentSlotId.MainHand, sword), "Could not prepare MainHand sword.");
        Assert(inventory.SetStack(ItemCategory.Equipment, 1, ItemStack.CreateNonStackable(occupiedShield)), "Could not occupy target inventory slot.");

        EquipmentOperationResult result = service.TryUnequipToInventory(EquipmentSlotId.MainHand, ItemCategory.Equipment, 1);

        Assert(!result.Success, "Unequip to occupied inventory slot should fail.");
        Assert(result.Error == EquipmentOperationError.InvalidInventorySlot, "Occupied target should return InvalidInventorySlot.");
        Assert(ReferenceEquals(equipment.GetEquipped(EquipmentSlotId.MainHand), sword), "MainHand should remain after occupied target failure.");
        Assert(ReferenceEquals(GetInstanceAt(inventory, ItemCategory.Equipment, 1), occupiedShield), "Occupied inventory target should remain unchanged.");
    }

    private static void ValidateUnequipToWrongInventoryCategoryFails(
        List<GameObject> gameObjects,
        ItemDefinition oneHandSword)
    {
        PlayerInventory inventory = CreateInventory(gameObjects, 4);
        CharacterEquipment equipment = new CharacterEquipment();
        EquipmentService service = new EquipmentService(inventory, equipment);
        ItemInstance sword = new ItemInstance(oneHandSword);

        Assert(equipment.TrySetSlot(EquipmentSlotId.MainHand, sword), "Could not prepare MainHand sword.");

        EquipmentOperationResult result = service.TryUnequipToInventory(EquipmentSlotId.MainHand, ItemCategory.Crafting, 0);

        Assert(!result.Success, "Unequip to wrong inventory category should fail.");
        Assert(result.Error == EquipmentOperationError.InvalidInventorySlot, "Wrong category target should return InvalidInventorySlot.");
        Assert(ReferenceEquals(equipment.GetEquipped(EquipmentSlotId.MainHand), sword), "MainHand should remain after wrong category failure.");
        Assert(!ContainsInstance(inventory, ItemCategory.Crafting, sword.InstanceId), "Sword should not move into Crafting section.");
    }

    private static void ValidateReplacementReturnsToSourceSlot(
        List<GameObject> gameObjects,
        ItemDefinition oneHandSword)
    {
        PlayerInventory inventory = CreateInventory(gameObjects, 4);
        CharacterEquipment equipment = new CharacterEquipment();
        EquipmentService service = new EquipmentService(inventory, equipment);
        ItemInstance swordA = new ItemInstance(oneHandSword);
        ItemInstance swordB = new ItemInstance(oneHandSword);
        string swordAId = swordA.InstanceId;

        Assert(equipment.TrySetSlot(EquipmentSlotId.MainHand, swordA), "Could not prepare MainHand sword.");
        Assert(inventory.SetStack(ItemCategory.Equipment, 2, ItemStack.CreateNonStackable(swordB)), "Could not place replacement sword in source slot.");

        EquipmentOperationResult result = service.TryEquipFromInventory(ItemCategory.Equipment, 2, EquipmentSlotId.MainHand);

        Assert(result.Success, "Source-slot replacement equip failed.");
        Assert(ReferenceEquals(equipment.GetEquipped(EquipmentSlotId.MainHand), swordB), "Replacement sword should equip into MainHand.");
        Assert(ReferenceEquals(GetInstanceAt(inventory, ItemCategory.Equipment, 2), swordA), "Old MainHand sword should return exactly to source slot.");
        Assert(GetInstanceAt(inventory, ItemCategory.Equipment, 2).InstanceId == swordAId, "Old MainHand sword instance id changed after source-slot return.");
    }

    private static void ValidateTwoHandReplacementReturnsFirstConflictToSourceSlot(
        List<GameObject> gameObjects,
        ItemDefinition oneHandSword,
        ItemDefinition shield,
        ItemDefinition twoHandWeapon)
    {
        PlayerInventory inventory = CreateInventory(gameObjects, 5);
        CharacterEquipment equipment = new CharacterEquipment();
        EquipmentService service = new EquipmentService(inventory, equipment);
        ItemInstance sword = new ItemInstance(oneHandSword);
        ItemInstance shieldInstance = new ItemInstance(shield);
        ItemInstance twoHand = new ItemInstance(twoHandWeapon);
        string shieldId = shieldInstance.InstanceId;

        Assert(equipment.TrySetSlot(EquipmentSlotId.MainHand, sword), "Could not prepare MainHand sword.");
        Assert(equipment.TrySetSlot(EquipmentSlotId.OffHand, shieldInstance), "Could not prepare OffHand shield.");
        Assert(inventory.SetStack(ItemCategory.Equipment, 2, ItemStack.CreateNonStackable(twoHand)), "Could not place two-hand source item.");

        EquipmentOperationResult result = service.TryEquipFromInventory(ItemCategory.Equipment, 2);

        Assert(result.Success, "Two-hand source-slot replacement failed.");
        Assert(ReferenceEquals(equipment.GetEquipped(EquipmentSlotId.MainHand), twoHand), "Two-hand weapon should equip into MainHand.");
        Assert(equipment.GetEquipped(EquipmentSlotId.OffHand) == null, "OffHand should be empty after two-hand replacement.");
        Assert(ReferenceEquals(GetInstanceAt(inventory, ItemCategory.Equipment, 2), sword), "Old MainHand sword should return exactly to source slot.");
        Assert(ContainsInstance(inventory, shieldId), "Old shield should return to another inventory slot.");
        Assert(!ReferenceEquals(GetInstanceAt(inventory, ItemCategory.Equipment, 2), shieldInstance), "Shield should not occupy the source slot before MainHand conflict.");
    }

    private static void ValidateIncompatibleDualWieldReplacementReturnsFirstConflictToSourceSlot(
        List<GameObject> gameObjects,
        ItemDefinition oneHandSword,
        ItemDefinition oneHandAxe)
    {
        PlayerInventory inventory = CreateInventory(gameObjects, 5);
        CharacterEquipment equipment = new CharacterEquipment();
        EquipmentService service = new EquipmentService(inventory, equipment);
        ItemInstance swordA = new ItemInstance(oneHandSword);
        ItemInstance swordB = new ItemInstance(oneHandSword);
        ItemInstance axe = new ItemInstance(oneHandAxe);
        string swordBId = swordB.InstanceId;

        Assert(equipment.TrySetSlot(EquipmentSlotId.MainHand, swordA), "Could not prepare MainHand sword.");
        Assert(equipment.TrySetSlot(EquipmentSlotId.OffHand, swordB), "Could not prepare OffHand sword.");
        Assert(inventory.SetStack(ItemCategory.Equipment, 2, ItemStack.CreateNonStackable(axe)), "Could not place axe source item.");

        EquipmentOperationResult result = service.TryEquipFromInventory(ItemCategory.Equipment, 2);

        Assert(result.Success, "Incompatible dual-wield source-slot replacement failed.");
        Assert(ReferenceEquals(equipment.GetEquipped(EquipmentSlotId.MainHand), axe), "Axe should equip into MainHand.");
        Assert(equipment.GetEquipped(EquipmentSlotId.OffHand) == null, "OffHand should clear after incompatible replacement.");
        Assert(ReferenceEquals(GetInstanceAt(inventory, ItemCategory.Equipment, 2), swordA), "Old MainHand sword should return exactly to source slot.");
        Assert(ContainsInstance(inventory, swordBId), "Old OffHand sword should return to another inventory slot.");
        Assert(!ReferenceEquals(GetInstanceAt(inventory, ItemCategory.Equipment, 2), swordB), "Old OffHand sword should not take the source slot before MainHand conflict.");
    }

    private static void ValidateCompatibleDualWieldReplacementReturnsMainHandToSourceSlot(
        List<GameObject> gameObjects,
        ItemDefinition oneHandSword)
    {
        PlayerInventory inventory = CreateInventory(gameObjects, 5);
        CharacterEquipment equipment = new CharacterEquipment();
        EquipmentService service = new EquipmentService(inventory, equipment);
        ItemInstance swordA = new ItemInstance(oneHandSword);
        ItemInstance swordB = new ItemInstance(oneHandSword);
        ItemInstance swordC = new ItemInstance(oneHandSword);

        Assert(equipment.TrySetSlot(EquipmentSlotId.MainHand, swordA), "Could not prepare MainHand sword.");
        Assert(equipment.TrySetSlot(EquipmentSlotId.OffHand, swordB), "Could not prepare OffHand sword.");
        Assert(inventory.SetStack(ItemCategory.Equipment, 2, ItemStack.CreateNonStackable(swordC)), "Could not place compatible replacement sword.");

        EquipmentOperationResult result = service.TryEquipFromInventory(ItemCategory.Equipment, 2);

        Assert(result.Success, "Compatible dual-wield source-slot replacement failed.");
        Assert(ReferenceEquals(equipment.GetEquipped(EquipmentSlotId.MainHand), swordC), "Third sword should replace MainHand.");
        Assert(ReferenceEquals(equipment.GetEquipped(EquipmentSlotId.OffHand), swordB), "OffHand sword should remain equipped.");
        Assert(ReferenceEquals(GetInstanceAt(inventory, ItemCategory.Equipment, 2), swordA), "Old MainHand sword should return exactly to source slot.");
    }

    private static void ValidateReplacementFailsBeforeMutationWhenAdditionalConflictCannotReturn(
        List<GameObject> gameObjects,
        ItemDefinition oneHandSword,
        ItemDefinition shield,
        ItemDefinition twoHandWeapon)
    {
        PlayerInventory inventory = CreateInventory(gameObjects, 3);
        CharacterEquipment equipment = new CharacterEquipment();
        EquipmentService service = new EquipmentService(inventory, equipment);
        ItemInstance sword = new ItemInstance(oneHandSword);
        ItemInstance shieldInstance = new ItemInstance(shield);
        ItemInstance fillerA = new ItemInstance(shield);
        ItemInstance fillerB = new ItemInstance(shield);
        ItemInstance twoHand = new ItemInstance(twoHandWeapon);

        Assert(equipment.TrySetSlot(EquipmentSlotId.MainHand, sword), "Could not prepare MainHand sword.");
        Assert(equipment.TrySetSlot(EquipmentSlotId.OffHand, shieldInstance), "Could not prepare OffHand shield.");
        Assert(inventory.SetStack(ItemCategory.Equipment, 0, ItemStack.CreateNonStackable(fillerA)), "Could not fill first inventory slot.");
        Assert(inventory.SetStack(ItemCategory.Equipment, 1, ItemStack.CreateNonStackable(fillerB)), "Could not fill second inventory slot.");
        Assert(inventory.SetStack(ItemCategory.Equipment, 2, ItemStack.CreateNonStackable(twoHand)), "Could not place two-hand source item.");

        EquipmentOperationResult result = service.TryEquipFromInventory(ItemCategory.Equipment, 2);

        Assert(!result.Success, "Replacement should fail when additional conflicts cannot return.");
        Assert(result.Error == EquipmentOperationError.InventoryFull, "Not enough return space should return InventoryFull.");
        Assert(ReferenceEquals(GetInstanceAt(inventory, ItemCategory.Equipment, 2), twoHand), "Source slot should keep original item after failed replacement.");
        Assert(ReferenceEquals(equipment.GetEquipped(EquipmentSlotId.MainHand), sword), "MainHand should remain unchanged after failed replacement.");
        Assert(ReferenceEquals(equipment.GetEquipped(EquipmentSlotId.OffHand), shieldInstance), "OffHand should remain unchanged after failed replacement.");
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
        ItemDefinition nonEquippable,
        ItemDefinition miscWeaponLike)
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

        PlayerInventory miscWeaponLikeInventory = CreateInventory(gameObjects, 4);
        CharacterEquipment miscWeaponLikeEquipment = new CharacterEquipment();
        EquipmentService miscWeaponLikeService = new EquipmentService(miscWeaponLikeInventory, miscWeaponLikeEquipment);
        ItemInstance miscWeaponLikeInstance = new ItemInstance(miscWeaponLike);
        string miscWeaponLikeId = miscWeaponLikeInstance.InstanceId;

        Assert(miscWeaponLikeInventory.TryAddInstance(miscWeaponLikeInstance).FullyAdded, "Could not add weapon-like Misc item.");
        EquipmentOperationResult miscWeaponLikeResult = miscWeaponLikeService.TryEquipFromInventory(ItemCategory.Misc, 0);

        Assert(!miscWeaponLikeResult.Success, "Weapon-like Misc item should not equip.");
        Assert(miscWeaponLikeResult.Error == EquipmentOperationError.ItemNotEquippable, "Weapon-like Misc item should return ItemNotEquippable.");
        Assert(ContainsInstance(miscWeaponLikeInventory, ItemCategory.Misc, miscWeaponLikeId), "Rejected weapon-like Misc item should remain in inventory.");
        Assert(miscWeaponLikeEquipment.GetEquipped(EquipmentSlotId.MainHand) == null, "Equipment should remain unchanged after rejected weapon-like Misc item.");
    }

    private static ItemInstance AddAndEquip(
        PlayerInventory inventory,
        EquipmentService service,
        ItemDefinition definition)
    {
        ItemInstance instance = new ItemInstance(definition);
        string instanceId = instance.InstanceId;

        Assert(inventory.TryAddInstance(instance).FullyAdded, $"Could not add item '{definition.Id}' to inventory.");
        EquipmentOperationResult result = service.TryEquipFromInventory(ItemCategory.Equipment, 0);

        Assert(result.Success, $"Could not equip item '{definition.Id}'. Error: {result.Error}.");
        Assert(ReferenceEquals(result.EquippedInstance, instance), $"Equipped item '{definition.Id}' reference changed.");
        Assert(result.EquippedInstance.InstanceId == instanceId, $"Equipped item '{definition.Id}' instance id changed.");

        return instance;
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
        return ContainsInstance(inventory, ItemCategory.Equipment, instanceId);
    }

    private static bool ContainsInstance(PlayerInventory inventory, ItemCategory category, string instanceId)
    {
        ItemContainerSection section = inventory.GetSection(category);
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

    private static ItemInstance GetInstanceAt(PlayerInventory inventory, ItemCategory category, int index)
    {
        ItemSlot slot = inventory.GetSlot(category, index);
        return slot?.Stack?.Instance;
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
