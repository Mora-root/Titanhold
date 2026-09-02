using System;
using System.Collections.Generic;

namespace Titanhold.Session
{
    public sealed class CharacterSnapshotService
    {
        public CharacterSnapshotCaptureResult TryCapture(
            string characterId,
            PlayerInventory inventory,
            PlayerEquipmentRuntime equipmentRuntime,
            PlayerExperience experience,
            PlayerGold gold)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                return CharacterSnapshotCaptureResult.Failed(
                    CharacterSnapshotError.MissingCharacterId);
            }

            if (inventory == null || equipmentRuntime == null ||
                experience == null || gold == null)
            {
                return CharacterSnapshotCaptureResult.Failed(
                    CharacterSnapshotError.MissingRuntimeSource);
            }

            inventory.EnsureInitialized();
            equipmentRuntime.EnsureInitialized();
            if (!experience.CanRestoreState(
                    experience.CurrentLevel,
                    experience.CurrentExperience) ||
                !gold.CanRestoreState(gold.Amount))
            {
                return CharacterSnapshotCaptureResult.Failed(
                    CharacterSnapshotError.InvalidProgression);
            }

            HashSet<string> instanceIds = new(StringComparer.Ordinal);
            List<InventorySlotSnapshot> inventorySlots = new();
            IReadOnlyList<ItemContainerSection> sections =
                inventory.Container.Sections;
            for (int sectionIndex = 0; sectionIndex < sections.Count; sectionIndex++)
            {
                ItemContainerSection section = sections[sectionIndex];
                if (section == null)
                    continue;

                for (int slotIndex = 0; slotIndex < section.Capacity; slotIndex++)
                {
                    ItemSlot slot = section.GetSlot(slotIndex);
                    if (slot == null || slot.IsEmpty)
                        continue;

                    CharacterSnapshotCaptureResult stackResult =
                        TryCaptureStack(slot.Stack, instanceIds, out ItemStackSnapshot stack);
                    if (!stackResult.Success)
                        return stackResult;

                    inventorySlots.Add(
                        new InventorySlotSnapshot(section.Category, slotIndex, stack));
                }
            }

            List<EquipmentSlotSnapshot> equipmentSlots = new();
            CharacterEquipment equipment = equipmentRuntime.Equipment;
            foreach (EquipmentSlotId slotId in Enum.GetValues(typeof(EquipmentSlotId)))
            {
                ItemInstance item = equipment.GetEquipped(slotId);
                if (item == null)
                    continue;

                CharacterSnapshotCaptureResult itemResult =
                    TryCaptureInstance(item, instanceIds, out ItemInstanceSnapshot itemSnapshot);
                if (!itemResult.Success)
                    return itemResult;

                equipmentSlots.Add(new EquipmentSlotSnapshot(slotId, itemSnapshot));
            }

            CharacterSnapshot snapshot = new(
                characterId,
                experience.CurrentLevel,
                experience.CurrentExperience,
                gold.Amount,
                inventorySlots,
                equipmentSlots);
            return CharacterSnapshotCaptureResult.Succeeded(snapshot);
        }

        public CharacterSnapshotRestoreResult TryRestore(
            CharacterSnapshot snapshot,
            IItemDefinitionResolver definitionResolver,
            PlayerInventory inventory,
            PlayerEquipmentRuntime equipmentRuntime,
            PlayerExperience experience,
            PlayerGold gold)
        {
            if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.CharacterId))
            {
                return CharacterSnapshotRestoreResult.Failed(
                    CharacterSnapshotError.MissingCharacterId);
            }

            if (definitionResolver == null)
            {
                return CharacterSnapshotRestoreResult.Failed(
                    CharacterSnapshotError.MissingDefinitionResolver);
            }

            if (inventory == null || equipmentRuntime == null ||
                experience == null || gold == null)
            {
                return CharacterSnapshotRestoreResult.Failed(
                    CharacterSnapshotError.MissingRuntimeSource);
            }

            if (snapshot.SchemaVersion != CharacterSnapshot.CurrentSchemaVersion)
            {
                return CharacterSnapshotRestoreResult.Failed(
                    CharacterSnapshotError.UnsupportedSchemaVersion,
                    $"Schema {snapshot.SchemaVersion} is not supported.");
            }

            if (!experience.CanRestoreState(snapshot.Level, snapshot.Experience) ||
                !gold.CanRestoreState(snapshot.Gold))
            {
                return CharacterSnapshotRestoreResult.Failed(
                    CharacterSnapshotError.InvalidProgression);
            }

            inventory.EnsureInitialized();
            equipmentRuntime.EnsureInitialized();
            ItemContainer restoredInventory = CreateEmptyMatchingContainer(
                inventory.Container);
            CharacterEquipment restoredEquipment = new();
            HashSet<string> instanceIds = new(StringComparer.Ordinal);

            CharacterSnapshotRestoreResult inventoryResult = RestoreInventory(
                snapshot,
                definitionResolver,
                restoredInventory,
                instanceIds);
            if (!inventoryResult.Success)
                return inventoryResult;

            CharacterSnapshotRestoreResult equipmentResult = RestoreEquipment(
                snapshot,
                definitionResolver,
                restoredEquipment,
                instanceIds);
            if (!equipmentResult.Success)
                return equipmentResult;

            CharacterSnapshotRestoreResult loadoutResult =
                ValidateEquipmentLoadout(restoredEquipment);
            if (!loadoutResult.Success)
                return loadoutResult;

            inventory.ReplaceContainerState(restoredInventory);
            equipmentRuntime.Equipment.ReplaceState(restoredEquipment);
            experience.RestoreState(snapshot.Level, snapshot.Experience);
            gold.RestoreState(snapshot.Gold);
            return CharacterSnapshotRestoreResult.Succeeded();
        }

        private static CharacterSnapshotCaptureResult TryCaptureStack(
            ItemStack stack,
            HashSet<string> instanceIds,
            out ItemStackSnapshot snapshot)
        {
            snapshot = null;
            if (stack == null || stack.Definition == null || stack.Amount <= 0)
            {
                return CharacterSnapshotCaptureResult.Failed(
                    CharacterSnapshotError.InvalidItemStack);
            }

            ItemDefinition definition = stack.Definition;
            if (string.IsNullOrWhiteSpace(definition.Id))
            {
                return CharacterSnapshotCaptureResult.Failed(
                    CharacterSnapshotError.MissingItemDefinitionId);
            }

            if (definition.MaxStack > 1)
            {
                if (stack.Instance != null || stack.Amount > definition.MaxStack)
                {
                    return CharacterSnapshotCaptureResult.Failed(
                        CharacterSnapshotError.InvalidItemStack);
                }

                snapshot = new ItemStackSnapshot(definition.Id, stack.Amount);
                return CharacterSnapshotCaptureResult.Succeeded(null);
            }

            if (stack.Amount != 1 || stack.Instance == null ||
                !ReferenceEquals(stack.Instance.Definition, definition))
            {
                return CharacterSnapshotCaptureResult.Failed(
                    CharacterSnapshotError.InvalidItemStack);
            }

            CharacterSnapshotCaptureResult instanceResult = TryCaptureInstance(
                stack.Instance,
                instanceIds,
                out ItemInstanceSnapshot instanceSnapshot);
            if (!instanceResult.Success)
                return instanceResult;

            snapshot = new ItemStackSnapshot(
                definition.Id,
                stack.Amount,
                instanceSnapshot);
            return CharacterSnapshotCaptureResult.Succeeded(null);
        }

        private static CharacterSnapshotCaptureResult TryCaptureInstance(
            ItemInstance item,
            HashSet<string> instanceIds,
            out ItemInstanceSnapshot snapshot)
        {
            snapshot = null;
            if (item == null || item.Definition == null ||
                item.Definition.MaxStack > 1 ||
                string.IsNullOrWhiteSpace(item.InstanceId))
            {
                return CharacterSnapshotCaptureResult.Failed(
                    CharacterSnapshotError.InvalidItemInstance);
            }

            if (string.IsNullOrWhiteSpace(item.Definition.Id))
            {
                return CharacterSnapshotCaptureResult.Failed(
                    CharacterSnapshotError.MissingItemDefinitionId);
            }

            if (!instanceIds.Add(item.InstanceId))
            {
                return CharacterSnapshotCaptureResult.Failed(
                    CharacterSnapshotError.DuplicateItemInstanceId,
                    item.InstanceId);
            }

            for (int i = 0; i < item.GeneratedModifiers.Count; i++)
            {
                if (!IsModifierValid(item.GeneratedModifiers[i]))
                {
                    return CharacterSnapshotCaptureResult.Failed(
                        CharacterSnapshotError.InvalidGeneratedModifier,
                        item.InstanceId);
                }
            }

            snapshot = new ItemInstanceSnapshot(
                item.Definition.Id,
                item.InstanceId,
                item.GeneratedModifiers);
            return CharacterSnapshotCaptureResult.Succeeded(null);
        }

        private static CharacterSnapshotRestoreResult RestoreInventory(
            CharacterSnapshot snapshot,
            IItemDefinitionResolver resolver,
            ItemContainer inventory,
            HashSet<string> instanceIds)
        {
            HashSet<string> occupiedSlots = new(StringComparer.Ordinal);
            for (int i = 0; i < snapshot.InventorySlots.Count; i++)
            {
                InventorySlotSnapshot slotSnapshot = snapshot.InventorySlots[i];
                if (slotSnapshot == null ||
                    !Enum.IsDefined(typeof(ItemCategory), slotSnapshot.Category))
                {
                    return CharacterSnapshotRestoreResult.Failed(
                        CharacterSnapshotError.InvalidInventorySlot,
                        $"Inventory entry {i}.");
                }

                ItemSlot slot = inventory.GetSlot(
                    slotSnapshot.Category,
                    slotSnapshot.SlotIndex);
                if (slot == null)
                {
                    return CharacterSnapshotRestoreResult.Failed(
                        CharacterSnapshotError.InvalidInventorySlot,
                        $"{slotSnapshot.Category}:{slotSnapshot.SlotIndex}.");
                }

                string address =
                    $"{(int)slotSnapshot.Category}:{slotSnapshot.SlotIndex}";
                if (!occupiedSlots.Add(address))
                {
                    return CharacterSnapshotRestoreResult.Failed(
                        CharacterSnapshotError.DuplicateInventorySlot,
                        address);
                }

                CharacterSnapshotRestoreResult stackResult = TryRestoreStack(
                    slotSnapshot.Stack,
                    slotSnapshot.Category,
                    resolver,
                    instanceIds,
                    out ItemStack stack);
                if (!stackResult.Success)
                    return stackResult;

                slot.Set(stack);
            }

            return CharacterSnapshotRestoreResult.Succeeded();
        }

        private static CharacterSnapshotRestoreResult RestoreEquipment(
            CharacterSnapshot snapshot,
            IItemDefinitionResolver resolver,
            CharacterEquipment equipment,
            HashSet<string> instanceIds)
        {
            HashSet<EquipmentSlotId> occupiedSlots = new();
            for (int i = 0; i < snapshot.EquipmentSlots.Count; i++)
            {
                EquipmentSlotSnapshot slotSnapshot = snapshot.EquipmentSlots[i];
                if (slotSnapshot == null ||
                    !Enum.IsDefined(typeof(EquipmentSlotId), slotSnapshot.SlotId))
                {
                    return CharacterSnapshotRestoreResult.Failed(
                        CharacterSnapshotError.InvalidEquipmentSlot,
                        $"Equipment entry {i}.");
                }

                if (!occupiedSlots.Add(slotSnapshot.SlotId))
                {
                    return CharacterSnapshotRestoreResult.Failed(
                        CharacterSnapshotError.DuplicateEquipmentSlot,
                        slotSnapshot.SlotId.ToString());
                }

                CharacterSnapshotRestoreResult itemResult = TryRestoreInstance(
                    slotSnapshot.Item,
                    resolver,
                    instanceIds,
                    out ItemInstance item);
                if (!itemResult.Success)
                    return itemResult;

                if (!equipment.TrySetSlot(slotSnapshot.SlotId, item))
                {
                    return CharacterSnapshotRestoreResult.Failed(
                        CharacterSnapshotError.InvalidEquipmentSlot,
                        slotSnapshot.SlotId.ToString());
                }
            }

            return CharacterSnapshotRestoreResult.Succeeded();
        }

        private static CharacterSnapshotRestoreResult TryRestoreStack(
            ItemStackSnapshot snapshot,
            ItemCategory expectedCategory,
            IItemDefinitionResolver resolver,
            HashSet<string> instanceIds,
            out ItemStack stack)
        {
            stack = null;
            CharacterSnapshotRestoreResult definitionResult = TryResolveDefinition(
                snapshot?.DefinitionId,
                resolver,
                out ItemDefinition definition);
            if (!definitionResult.Success)
                return definitionResult;

            if (definition.Category != expectedCategory)
            {
                return CharacterSnapshotRestoreResult.Failed(
                    CharacterSnapshotError.ItemDefinitionMismatch,
                    snapshot.DefinitionId);
            }

            if (definition.MaxStack > 1)
            {
                if (snapshot.HasInstance ||
                    snapshot.Amount <= 0 ||
                    snapshot.Amount > definition.MaxStack)
                {
                    return CharacterSnapshotRestoreResult.Failed(
                        CharacterSnapshotError.InvalidItemStack,
                        $"{snapshot.DefinitionId}: amount={snapshot.Amount}, " +
                        $"maxStack={definition.MaxStack}, hasInstance={snapshot.HasInstance}.");
                }

                stack = ItemStack.CreateStackable(definition, snapshot.Amount);
                return CharacterSnapshotRestoreResult.Succeeded();
            }

            if (snapshot.Amount != 1 || !snapshot.HasInstance ||
                snapshot.Instance == null ||
                !string.Equals(
                    snapshot.DefinitionId,
                    snapshot.Instance.DefinitionId,
                    StringComparison.Ordinal))
            {
                return CharacterSnapshotRestoreResult.Failed(
                    CharacterSnapshotError.InvalidItemStack,
                    snapshot.DefinitionId);
            }

            CharacterSnapshotRestoreResult instanceResult = TryRestoreInstance(
                snapshot.Instance,
                resolver,
                instanceIds,
                out ItemInstance instance);
            if (!instanceResult.Success)
                return instanceResult;

            stack = ItemStack.CreateNonStackable(instance);
            return CharacterSnapshotRestoreResult.Succeeded();
        }

        private static CharacterSnapshotRestoreResult TryRestoreInstance(
            ItemInstanceSnapshot snapshot,
            IItemDefinitionResolver resolver,
            HashSet<string> instanceIds,
            out ItemInstance item)
        {
            item = null;
            if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.InstanceId))
            {
                return CharacterSnapshotRestoreResult.Failed(
                    CharacterSnapshotError.InvalidItemInstance);
            }

            CharacterSnapshotRestoreResult definitionResult = TryResolveDefinition(
                snapshot.DefinitionId,
                resolver,
                out ItemDefinition definition);
            if (!definitionResult.Success)
                return definitionResult;

            if (definition.MaxStack > 1)
            {
                return CharacterSnapshotRestoreResult.Failed(
                    CharacterSnapshotError.InvalidItemInstance,
                    snapshot.InstanceId);
            }

            if (!instanceIds.Add(snapshot.InstanceId))
            {
                return CharacterSnapshotRestoreResult.Failed(
                    CharacterSnapshotError.DuplicateItemInstanceId,
                    snapshot.InstanceId);
            }

            for (int i = 0; i < snapshot.GeneratedModifiers.Count; i++)
            {
                if (!IsModifierValid(snapshot.GeneratedModifiers[i]))
                {
                    return CharacterSnapshotRestoreResult.Failed(
                        CharacterSnapshotError.InvalidGeneratedModifier,
                        snapshot.InstanceId);
                }
            }

            item = new ItemInstance(
                definition,
                snapshot.InstanceId,
                snapshot.GeneratedModifiers);
            return CharacterSnapshotRestoreResult.Succeeded();
        }

        private static CharacterSnapshotRestoreResult TryResolveDefinition(
            string definitionId,
            IItemDefinitionResolver resolver,
            out ItemDefinition definition)
        {
            definition = null;
            if (string.IsNullOrWhiteSpace(definitionId))
            {
                return CharacterSnapshotRestoreResult.Failed(
                    CharacterSnapshotError.MissingItemDefinitionId);
            }

            if (!resolver.TryResolve(definitionId, out definition) ||
                definition == null)
            {
                return CharacterSnapshotRestoreResult.Failed(
                    CharacterSnapshotError.UnresolvedItemDefinition,
                    definitionId);
            }

            if (!string.Equals(definition.Id, definitionId, StringComparison.Ordinal))
            {
                return CharacterSnapshotRestoreResult.Failed(
                    CharacterSnapshotError.ItemDefinitionMismatch,
                    definitionId);
            }

            return CharacterSnapshotRestoreResult.Succeeded();
        }

        private static CharacterSnapshotRestoreResult ValidateEquipmentLoadout(
            CharacterEquipment equipment)
        {
            ItemDefinition mainHand =
                equipment.GetDefinition(EquipmentSlotId.MainHand);
            ItemDefinition offHand =
                equipment.GetDefinition(EquipmentSlotId.OffHand);

            if (offHand != null && offHand.IsWeapon)
            {
                if (offHand.OccupiesBothHands || mainHand == null ||
                    !mainHand.IsWeapon || mainHand.OccupiesBothHands ||
                    mainHand.WeaponFamily != offHand.WeaponFamily)
                {
                    return CharacterSnapshotRestoreResult.Failed(
                        CharacterSnapshotError.InvalidEquipmentLoadout,
                        "Invalid dual-wield hand state.");
                }
            }

            if (mainHand != null && mainHand.OccupiesBothHands && offHand != null)
            {
                return CharacterSnapshotRestoreResult.Failed(
                    CharacterSnapshotError.InvalidEquipmentLoadout,
                    "A two-handed weapon conflicts with OffHand.");
            }

            return CharacterSnapshotRestoreResult.Succeeded();
        }

        private static ItemContainer CreateEmptyMatchingContainer(ItemContainer source)
        {
            Dictionary<ItemCategory, int> capacities = new();
            IReadOnlyList<ItemContainerSection> sections = source.Sections;
            for (int i = 0; i < sections.Count; i++)
            {
                ItemContainerSection section = sections[i];
                if (section != null)
                    capacities[section.Category] = section.Capacity;
            }

            return new ItemContainer(capacities, 0);
        }

        private static bool IsModifierValid(StatModifierData modifier)
        {
            return Enum.IsDefined(typeof(StatType), modifier.Type) &&
                   Enum.IsDefined(typeof(StatModifierType), modifier.ModifierType) &&
                   !float.IsNaN(modifier.Value) &&
                   !float.IsInfinity(modifier.Value);
        }
    }
}
