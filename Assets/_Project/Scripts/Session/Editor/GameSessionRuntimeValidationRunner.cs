using System;
using Titanhold.Run;
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

                GameSessionRuntime runtime = new(
                    catalog,
                    CreateRewardPolicy(),
                    runExperienceCurve:
                        new RunExperienceCurve(new[] { 10, 20 }));
                int snapshotChangeCount = 0;
                runtime.CharacterSnapshotChanged += (_, _) =>
                    snapshotChangeCount++;
                Assert(runtime.AccountCrystals.TryAdd(25).Success,
                    "Could not prepare account crystals.");

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

                Assert(runtime.TryGetActiveRunProgression(
                           begin.RunSessionId,
                           out RunProgressionService progression) &&
                       progression.ParticipantCount == 1 &&
                       progression.TryGrantExperience(
                           "player:local",
                           15).Success &&
                       progression.TryGetParticipant(
                           "player:local",
                           out RunParticipantProgressionState runState) &&
                       runState.Level == 2 &&
                       runState.Experience == 5,
                    "Run transition did not create participant progression.");

                GameSessionCommandResult cancel =
                    runtime.GameSession.TryCancelRunTransition(
                        begin.RunSessionId);
                Assert(cancel.Success &&
                       !runtime.TryGetActiveRunProgression(
                           begin.RunSessionId,
                           out _) &&
                       runtime.AccountCrystals.Amount == 25,
                    "Cancelled run retained run progression or cleared account currency.");

                GameSessionCommandResult secondBegin =
                    runtime.GameSession.TryBeginRun(
                        new RunLaunchCommand(
                            "difficulty:prototype",
                            456,
                            new[]
                            {
                                new RunParticipantSelection(
                                    "player:local",
                                    "character:warrior")
                            }));
                RunProgressionService retainedProgression = null;
                Assert(secondBegin.Success &&
                       runtime.TryGetActiveRunProgression(
                           secondBegin.RunSessionId,
                           out retainedProgression),
                    "Second run did not create fresh progression.");
                Assert(runtime.GameSession.TryActivateRun(
                           secondBegin.RunSessionId).Success &&
                       retainedProgression.TryAddGold(
                           "player:local",
                           40).Success,
                    "Second run progression setup failed.");

                RunResultSummary result = new(
                    secondBegin.RunSessionId,
                    RunOutcome.Defeat,
                    completedRoundCount: 1);
                Assert(runtime.GameSession.TryConcludeRun(result).Success &&
                       runtime.TryGetActiveRunProgression(
                           secondBegin.RunSessionId,
                           out RunProgressionService transitionProgression) &&
                       ReferenceEquals(
                           retainedProgression,
                           transitionProgression),
                    "Hub transition cleared run progression before rewards could settle.");
                Assert(runtime.GameSession.TryCancelHubTransition(
                           secondBegin.RunSessionId).Success &&
                       runtime.TryGetActiveRunProgression(
                           secondBegin.RunSessionId,
                           out RunProgressionService retriedProgression) &&
                       ReferenceEquals(
                           retainedProgression,
                           retriedProgression),
                    "Failed Hub loading lost retryable run progression.");
                Assert(runtime.GameSession.TryConcludeRun(result).Success &&
                       runtime.GameSession.TryEnterHub(
                           secondBegin.RunSessionId).Success &&
                       !runtime.TryGetActiveRunProgression(
                           secondBegin.RunSessionId,
                           out _) &&
                       runtime.AccountCrystals.Amount == 25,
                    "Hub entry did not clear only the temporary run progression.");

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

        private static RunConclusionRewardPolicy CreateRewardPolicy()
        {
            return new RunConclusionRewardPolicy(
                new RunConclusionRewardConfiguration(100, 5, 200, 10),
                new System.Collections.Generic.Dictionary<string, int>
                {
                    { "difficulty:prototype", 100 }
                });
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
