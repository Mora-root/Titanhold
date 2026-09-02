using System;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Session.Editor
{
    public static class GameSessionRuntimeValidationRunner
    {
        [MenuItem("Tools/Titanhold/Validate Persistent Game Session Runtime")]
        public static void Validate()
        {
            ItemDefinitionCatalog catalog = null;
            ItemDefinition potion = null;
            GameObject source = null;
            GameObject target = null;

            try
            {
                potion = CreateDefinition("item:potion", maxStack: 10);
                catalog = CreateCatalog(potion);
                source = CreatePlayerRuntime("SessionRuntime_Source");
                target = CreatePlayerRuntime("SessionRuntime_Target");

                PlayerInventory sourceInventory =
                    source.GetComponent<PlayerInventory>();
                Assert(sourceInventory.SetStack(
                        ItemCategory.Consumable,
                        2,
                        ItemStack.CreateStackable(potion, 6)),
                    "Could not prepare source inventory.");
                source.GetComponent<PlayerExperience>().AddExperience(125);
                source.GetComponent<PlayerGold>().Add(17);

                GameSessionRuntime runtime = new(catalog);
                int snapshotChangeCount = 0;
                runtime.CharacterSnapshotChanged += (_, _) =>
                    snapshotChangeCount++;

                CharacterSnapshotCaptureResult capture =
                    runtime.TryCaptureCharacter(
                        "character:warrior",
                        sourceInventory,
                        source.GetComponent<PlayerEquipmentRuntime>(),
                        source.GetComponent<PlayerExperience>(),
                        source.GetComponent<PlayerGold>());
                Assert(capture.Success &&
                       runtime.StoredCharacterCount == 1 &&
                       snapshotChangeCount == 1,
                    $"Runtime capture failed: {capture.Error} {capture.Detail}");

                CharacterSnapshotRestoreResult restore =
                    runtime.TryRestoreCharacter(
                        " character:warrior ",
                        target.GetComponent<PlayerInventory>(),
                        target.GetComponent<PlayerEquipmentRuntime>(),
                        target.GetComponent<PlayerExperience>(),
                        target.GetComponent<PlayerGold>());
                Assert(restore.Success,
                    $"Runtime restore failed: {restore.Error} {restore.Detail}");
                ValidateRestoredCharacter(target);

                CharacterSnapshotRestoreResult missing =
                    runtime.TryRestoreCharacter(
                        "character:missing",
                        target.GetComponent<PlayerInventory>(),
                        target.GetComponent<PlayerEquipmentRuntime>(),
                        target.GetComponent<PlayerExperience>(),
                        target.GetComponent<PlayerGold>());
                Assert(!missing.Success &&
                       missing.Error == CharacterSnapshotError.SnapshotNotFound,
                    "Runtime accepted a missing character snapshot.");

                CharacterSnapshot originalSnapshot = capture.Snapshot;
                CharacterSnapshotCaptureResult rejectedCapture =
                    runtime.TryCaptureCharacter(
                        "character:warrior",
                        null,
                        source.GetComponent<PlayerEquipmentRuntime>(),
                        source.GetComponent<PlayerExperience>(),
                        source.GetComponent<PlayerGold>());
                Assert(!rejectedCapture.Success &&
                       runtime.TryGetCharacterSnapshot(
                           "character:warrior",
                           out CharacterSnapshot preservedSnapshot) &&
                       ReferenceEquals(originalSnapshot, preservedSnapshot) &&
                       snapshotChangeCount == 1,
                    "Rejected capture replaced the last valid snapshot.");

                GameSessionCommandResult begin = runtime.GameSession.TryBeginRun(
                    new RunLaunchCommand(
                        "difficulty:prototype",
                        123,
                        new[]
                        {
                            new RunParticipantSelection(
                                "player:local",
                                "character:warrior")
                        }));
                Assert(begin.Success &&
                       runtime.GameSession.State.Phase ==
                           GameSessionPhase.TransitionToRun &&
                       runtime.TryGetCharacterSnapshot(
                           "character:warrior",
                           out preservedSnapshot) &&
                       ReferenceEquals(originalSnapshot, preservedSnapshot),
                    "Run transition did not preserve runtime character state.");

                Debug.Log("Persistent Game Session Runtime validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Persistent Game Session Runtime validation failed: {exception}");
            }
            finally
            {
                Destroy(source);
                Destroy(target);
                Destroy(catalog);
                Destroy(potion);
            }
        }

        private static void ValidateRestoredCharacter(GameObject target)
        {
            ItemStack potion = target.GetComponent<PlayerInventory>()
                .GetSlot(ItemCategory.Consumable, 2).Stack;
            Assert(potion != null &&
                   potion.Definition.Id == "item:potion" &&
                   potion.Amount == 6,
                "Runtime did not restore inventory state.");
            Assert(target.GetComponent<PlayerExperience>().CurrentLevel == 2 &&
                   target.GetComponent<PlayerExperience>().CurrentExperience == 25,
                "Runtime did not restore progression state.");
            Assert(target.GetComponent<PlayerGold>().Amount == 17,
                "Runtime did not restore gold state.");
        }

        private static GameObject CreatePlayerRuntime(string name)
        {
            GameObject result = new(name);
            PlayerInventory inventory = result.AddComponent<PlayerInventory>();
            PlayerEquipmentRuntime equipment =
                result.AddComponent<PlayerEquipmentRuntime>();
            result.AddComponent<PlayerExperience>();
            result.AddComponent<PlayerGold>();
            result.AddComponent<PlayerInfo>();
            inventory.EnsureInitialized();
            equipment.SetPlayerInventory(inventory);
            return result;
        }

        private static ItemDefinitionCatalog CreateCatalog(
            params ItemDefinition[] definitions)
        {
            ItemDefinitionCatalog catalog =
                ScriptableObject.CreateInstance<ItemDefinitionCatalog>();
            SerializedObject serialized = new(catalog);
            SerializedProperty property = serialized.FindProperty("definitions");
            property.arraySize = definitions.Length;
            for (int i = 0; i < definitions.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = definitions[i];

            serialized.ApplyModifiedPropertiesWithoutUndo();
            catalog.RebuildIndex();
            return catalog;
        }

        private static ItemDefinition CreateDefinition(string id, int maxStack)
        {
            ItemDefinition definition =
                ScriptableObject.CreateInstance<ItemDefinition>();
            definition.name = id;
            SerializedObject serialized = new(definition);
            serialized.FindProperty("id").stringValue = id;
            serialized.FindProperty("category").enumValueIndex =
                (int)ItemCategory.Consumable;
            serialized.FindProperty("maxStack").intValue = maxStack;
            serialized.FindProperty("consumableSubtype").enumValueIndex =
                (int)ConsumableSubtype.Potion;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private static void Destroy(UnityEngine.Object instance)
        {
            if (instance != null)
                UnityEngine.Object.DestroyImmediate(instance);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
