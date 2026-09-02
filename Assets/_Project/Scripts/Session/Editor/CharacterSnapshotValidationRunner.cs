using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Session.Editor
{
    public static class CharacterSnapshotValidationRunner
    {
        [MenuItem("Tools/Titanhold/Validate Character Snapshot")]
        public static void Validate()
        {
            GameObject source = null;
            GameObject target = null;
            ItemDefinition potion = null;
            ItemDefinition sword = null;
            ItemDefinition ring = null;

            try
            {
                potion = CreateDefinition(
                    "item:potion",
                    ItemCategory.Consumable,
                    maxStack: 10);
                sword = CreateDefinition(
                    "item:sword",
                    ItemCategory.Equipment,
                    maxStack: 1,
                    EquipmentSlotType.Weapon,
                    WeaponType.OneHandSword);
                ring = CreateDefinition(
                    "item:ring",
                    ItemCategory.Equipment,
                    maxStack: 1,
                    EquipmentSlotType.Ring);

                DictionaryResolver resolver = new(potion, sword, ring);
                source = CreatePlayerRuntime("CharacterSnapshot_Source");
                target = CreatePlayerRuntime("CharacterSnapshot_Target");

                PopulateSource(source, potion, sword, ring);
                CharacterSnapshotService service = new();
                CharacterSnapshotCaptureResult capture = service.TryCapture(
                    "character:warrior",
                    source.GetComponent<PlayerInventory>(),
                    source.GetComponent<PlayerEquipmentRuntime>(),
                    source.GetComponent<PlayerExperience>(),
                    source.GetComponent<PlayerGold>());
                Assert(capture.Success && capture.Snapshot != null,
                    $"Capture failed: {capture.Error} {capture.Detail}");

                string json = JsonUtility.ToJson(capture.Snapshot);
                CharacterSnapshot serializedSnapshot =
                    JsonUtility.FromJson<CharacterSnapshot>(json);
                Assert(serializedSnapshot != null &&
                       serializedSnapshot.SchemaVersion ==
                           CharacterSnapshot.CurrentSchemaVersion,
                    "Character snapshot did not survive JSON serialization.");

                CharacterSnapshotRestoreResult restore = service.TryRestore(
                    serializedSnapshot,
                    resolver,
                    target.GetComponent<PlayerInventory>(),
                    target.GetComponent<PlayerEquipmentRuntime>(),
                    target.GetComponent<PlayerExperience>(),
                    target.GetComponent<PlayerGold>());
                Assert(restore.Success,
                    $"Restore failed: {restore.Error} {restore.Detail}");
                ValidateRestoredState(target);
                ValidateRejectedRestoreIsAtomic(service, resolver, target);
                ValidateInvalidHandLoadout(service, resolver, target, sword);

                Debug.Log("Character Snapshot validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"Character Snapshot validation failed: {exception}");
            }
            finally
            {
                if (source != null)
                    UnityEngine.Object.DestroyImmediate(source);

                if (target != null)
                    UnityEngine.Object.DestroyImmediate(target);

                if (potion != null)
                    UnityEngine.Object.DestroyImmediate(potion);

                if (sword != null)
                    UnityEngine.Object.DestroyImmediate(sword);

                if (ring != null)
                    UnityEngine.Object.DestroyImmediate(ring);
            }
        }

        private static GameObject CreatePlayerRuntime(string name)
        {
            GameObject result = new(name);
            PlayerInventory inventory = result.AddComponent<PlayerInventory>();
            PlayerEquipmentRuntime equipment =
                result.AddComponent<PlayerEquipmentRuntime>();
            result.AddComponent<PlayerExperience>();
            result.AddComponent<PlayerGold>();
            PlayerInfo playerInfo = result.AddComponent<PlayerInfo>();

            inventory.EnsureInitialized();
            equipment.SetPlayerInventory(inventory);
            SerializedObject serializedInfo = new(playerInfo);
            serializedInfo.FindProperty("playerExperience").objectReferenceValue =
                result.GetComponent<PlayerExperience>();
            serializedInfo.ApplyModifiedPropertiesWithoutUndo();
            return result;
        }

        private static void PopulateSource(
            GameObject source,
            ItemDefinition potion,
            ItemDefinition sword,
            ItemDefinition ring)
        {
            PlayerInventory inventory = source.GetComponent<PlayerInventory>();
            PlayerEquipmentRuntime equipment =
                source.GetComponent<PlayerEquipmentRuntime>();
            PlayerExperience experience = source.GetComponent<PlayerExperience>();
            PlayerGold gold = source.GetComponent<PlayerGold>();

            Assert(inventory.SetStack(
                    ItemCategory.Consumable,
                    3,
                    ItemStack.CreateStackable(potion, 7)),
                "Could not prepare the stackable inventory item.");

            ItemInstance ringInstance = new(
                ring,
                "instance:ring",
                new[]
                {
                    new StatModifierData(
                        StatType.Armor,
                        StatModifierType.Flat,
                        4f)
                });
            Assert(inventory.SetStack(
                    ItemCategory.Equipment,
                    5,
                    ItemStack.CreateNonStackable(ringInstance)),
                "Could not prepare the generated inventory item.");

            ItemInstance swordInstance = new(
                sword,
                "instance:sword",
                new[]
                {
                    new StatModifierData(
                        StatType.Damage,
                        StatModifierType.Increased,
                        12f)
                });
            Assert(equipment.Equipment.TrySetSlot(
                    EquipmentSlotId.MainHand,
                    swordInstance),
                "Could not prepare equipped weapon.");

            experience.AddExperience(275);
            gold.Add(42);
        }

        private static void ValidateRestoredState(GameObject target)
        {
            PlayerInventory inventory = target.GetComponent<PlayerInventory>();
            ItemStack potionStack = inventory.GetSlot(
                ItemCategory.Consumable,
                3).Stack;
            Assert(potionStack != null &&
                   potionStack.Definition.Id == "item:potion" &&
                   potionStack.Amount == 7 &&
                   potionStack.Instance == null,
                "Stackable inventory state was not restored exactly.");

            ItemInstance ring = inventory.GetSlot(
                ItemCategory.Equipment,
                5).Stack?.Instance;
            Assert(ring != null &&
                   ring.Definition.Id == "item:ring" &&
                   ring.InstanceId == "instance:ring" &&
                   ring.GeneratedModifiers.Count == 1 &&
                   ring.GeneratedModifiers[0].Type == StatType.Armor &&
                   Math.Abs(ring.GeneratedModifiers[0].Value - 4f) <= 0.0001f,
                "Generated inventory item was not restored exactly.");

            ItemInstance sword = target.GetComponent<PlayerEquipmentRuntime>()
                .Equipment.GetEquipped(EquipmentSlotId.MainHand);
            Assert(sword != null &&
                   sword.Definition.Id == "item:sword" &&
                   sword.InstanceId == "instance:sword" &&
                   sword.GeneratedModifiers.Count == 1,
                "Equipment state was not restored exactly.");

            PlayerExperience experience = target.GetComponent<PlayerExperience>();
            Assert(experience.CurrentLevel == 3 &&
                   experience.CurrentExperience == 25 &&
                   target.GetComponent<PlayerInfo>().Level == 3,
                "Character progression or displayed level was not restored.");
            Assert(target.GetComponent<PlayerGold>().Amount == 42,
                "Character gold was not restored.");
        }

        private static void ValidateRejectedRestoreIsAtomic(
            CharacterSnapshotService service,
            IItemDefinitionResolver resolver,
            GameObject target)
        {
            PlayerInventory inventory = target.GetComponent<PlayerInventory>();
            PlayerEquipmentRuntime equipment =
                target.GetComponent<PlayerEquipmentRuntime>();
            PlayerExperience experience = target.GetComponent<PlayerExperience>();
            PlayerGold gold = target.GetComponent<PlayerGold>();
            ItemStack originalPotion = inventory.GetSlot(
                ItemCategory.Consumable,
                3).Stack;
            ItemInstance originalSword = equipment.Equipment.GetEquipped(
                EquipmentSlotId.MainHand);

            CharacterSnapshot invalid = new(
                "character:warrior",
                2,
                10,
                1,
                new[]
                {
                    new InventorySlotSnapshot(
                        ItemCategory.Misc,
                        0,
                        new ItemStackSnapshot(
                            "item:missing",
                            1,
                            new ItemInstanceSnapshot(
                                "item:missing",
                                "instance:missing",
                                Array.Empty<StatModifierData>())))
                },
                Array.Empty<EquipmentSlotSnapshot>());
            CharacterSnapshotRestoreResult result = service.TryRestore(
                invalid,
                resolver,
                inventory,
                equipment,
                experience,
                gold);

            Assert(!result.Success &&
                   result.Error == CharacterSnapshotError.UnresolvedItemDefinition,
                "Snapshot with an unresolved item definition was accepted.");
            Assert(ReferenceEquals(
                       inventory.GetSlot(ItemCategory.Consumable, 3).Stack,
                       originalPotion) &&
                   ReferenceEquals(
                       equipment.Equipment.GetEquipped(EquipmentSlotId.MainHand),
                       originalSword) &&
                   experience.CurrentLevel == 3 &&
                   experience.CurrentExperience == 25 &&
                   gold.Amount == 42,
                "Rejected snapshot partially changed runtime state.");
        }

        private static void ValidateInvalidHandLoadout(
            CharacterSnapshotService service,
            IItemDefinitionResolver resolver,
            GameObject target,
            ItemDefinition sword)
        {
            CharacterSnapshot invalid = new(
                "character:warrior",
                1,
                0,
                0,
                Array.Empty<InventorySlotSnapshot>(),
                new[]
                {
                    new EquipmentSlotSnapshot(
                        EquipmentSlotId.OffHand,
                        new ItemInstanceSnapshot(
                            sword.Id,
                            "instance:orphan-offhand",
                            Array.Empty<StatModifierData>()))
                });

            CharacterSnapshotRestoreResult result = service.TryRestore(
                invalid,
                resolver,
                target.GetComponent<PlayerInventory>(),
                target.GetComponent<PlayerEquipmentRuntime>(),
                target.GetComponent<PlayerExperience>(),
                target.GetComponent<PlayerGold>());
            Assert(!result.Success &&
                   result.Error == CharacterSnapshotError.InvalidEquipmentLoadout,
                "Orphan OffHand weapon snapshot was accepted.");
        }

        private static ItemDefinition CreateDefinition(
            string id,
            ItemCategory category,
            int maxStack,
            EquipmentSlotType equipmentSlotType = EquipmentSlotType.None,
            WeaponType weaponType = WeaponType.None)
        {
            ItemDefinition definition =
                ScriptableObject.CreateInstance<ItemDefinition>();
            definition.name = id;
            SerializedObject serialized = new(definition);
            serialized.FindProperty("id").stringValue = id;
            serialized.FindProperty("category").enumValueIndex = (int)category;
            serialized.FindProperty("maxStack").intValue = maxStack;
            serialized.FindProperty("equipmentSlotType").enumValueIndex =
                (int)equipmentSlotType;
            serialized.FindProperty("weaponType").enumValueIndex = (int)weaponType;
            if (category == ItemCategory.Consumable)
            {
                serialized.FindProperty("consumableSubtype").enumValueIndex =
                    (int)ConsumableSubtype.Potion;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private sealed class DictionaryResolver : IItemDefinitionResolver
        {
            private readonly Dictionary<string, ItemDefinition> definitions =
                new(StringComparer.Ordinal);

            public DictionaryResolver(params ItemDefinition[] definitions)
            {
                for (int i = 0; i < definitions.Length; i++)
                {
                    ItemDefinition definition = definitions[i];
                    this.definitions.Add(definition.Id, definition);
                }
            }

            public bool TryResolve(
                string definitionId,
                out ItemDefinition definition)
            {
                return definitions.TryGetValue(definitionId, out definition);
            }
        }
    }
}
