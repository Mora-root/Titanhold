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
                                "character:warrior",
                                "ability:spin")
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
                Assert(runtime.TryGetActiveRunAbilityLoadout(
                           begin.RunSessionId,
                           out RunAbilityLoadoutService abilityLoadout) &&
                       abilityLoadout.ParticipantCount == 1 &&
                       abilityLoadout.TryGetParticipant(
                           "player:local",
                           out RunParticipantAbilityState abilityState) &&
                       abilityState.HasAbility("ability:spin") &&
                       abilityState.TryGetAbilitySlot(
                           0,
                           out string startingAbilityId) &&
                       startingAbilityId == "ability:spin",
                    "Run transition did not seed the participant starting ability.");
                Assert(runtime.TryGetActiveRunAbilityChoices(
                           begin.RunSessionId,
                           out RunAbilityChoiceService abilityChoices) &&
                       abilityChoices.PendingChoiceCount == 0,
                    "Run transition did not create participant ability choices.");
                Assert(runtime.TryGetActiveRunStartReadiness(
                           begin.RunSessionId,
                           out RunStartReadinessService startReadiness) &&
                       startReadiness.ParticipantCount == 1 &&
                       startReadiness.ConfirmedParticipantCount == 1 &&
                       startReadiness.AllParticipantsConfirmed &&
                       startReadiness.IsSealed &&
                       startReadiness.TryGetParticipant(
                           "player:local",
                           out RunParticipantStartReadinessState readinessState) &&
                       readinessState.StartingAbilityId == "ability:spin",
                    "Seeded starting ability did not seal run start readiness.");

                GameSessionCommandResult cancel =
                    runtime.GameSession.TryCancelRunTransition(
                        begin.RunSessionId);
                Assert(cancel.Success &&
                       !runtime.TryGetActiveRunProgression(
                           begin.RunSessionId,
                           out _) &&
                       !runtime.TryGetActiveRunAbilityLoadout(
                           begin.RunSessionId,
                           out _) &&
                       !runtime.TryGetActiveRunAbilityChoices(
                           begin.RunSessionId,
                           out _) &&
                       !runtime.TryGetActiveRunStartReadiness(
                           begin.RunSessionId,
                           out _) &&
                       runtime.AccountCrystals.Amount == 25,
                    "Cancelled run retained temporary state or cleared account currency.");

                GameSessionCommandResult secondBegin =
                    runtime.GameSession.TryBeginRun(
                        new RunLaunchCommand(
                            "difficulty:prototype",
                            456,
                            new[]
                            {
                                new RunParticipantSelection(
                                    "player:local",
                                    "character:warrior",
                                    "ability:spin")
                            }));
                RunProgressionService retainedProgression = null;
                RunAbilityLoadoutService retainedAbilityLoadout = null;
                RunAbilityChoiceService retainedAbilityChoices = null;
                RunStartReadinessService retainedStartReadiness = null;
                Assert(secondBegin.Success &&
                       runtime.TryGetActiveRunProgression(
                           secondBegin.RunSessionId,
                           out retainedProgression) &&
                       runtime.TryGetActiveRunAbilityLoadout(
                           secondBegin.RunSessionId,
                           out retainedAbilityLoadout) &&
                       runtime.TryGetActiveRunAbilityChoices(
                           secondBegin.RunSessionId,
                           out retainedAbilityChoices) &&
                       runtime.TryGetActiveRunStartReadiness(
                           secondBegin.RunSessionId,
                           out retainedStartReadiness) &&
                       retainedStartReadiness.IsSealed &&
                       retainedAbilityLoadout.TryGetParticipant(
                           "player:local",
                           out RunParticipantAbilityState secondAbilityState) &&
                       secondAbilityState.TryGetAbilitySlot(
                           0,
                           out startingAbilityId) &&
                       startingAbilityId == "ability:spin",
                    "Second run did not create fresh participant run state.");
                Assert(runtime.GameSession.TryActivateRun(
                           secondBegin.RunSessionId).Success &&
                       retainedProgression.TryAddGold(
                           "player:local",
                           40).Success,
                    "Second run participant state setup failed.");

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
                           transitionProgression) &&
                       runtime.TryGetActiveRunAbilityLoadout(
                           secondBegin.RunSessionId,
                           out RunAbilityLoadoutService transitionAbilityLoadout) &&
                       ReferenceEquals(
                           retainedAbilityLoadout,
                           transitionAbilityLoadout) &&
                       runtime.TryGetActiveRunAbilityChoices(
                           secondBegin.RunSessionId,
                           out RunAbilityChoiceService transitionAbilityChoices) &&
                       ReferenceEquals(
                           retainedAbilityChoices,
                           transitionAbilityChoices) &&
                       runtime.TryGetActiveRunStartReadiness(
                           secondBegin.RunSessionId,
                           out RunStartReadinessService transitionReadiness) &&
                       ReferenceEquals(
                           retainedStartReadiness,
                           transitionReadiness),
                    "Hub transition cleared temporary run state before rewards could settle.");
                Assert(runtime.GameSession.TryCancelHubTransition(
                           secondBegin.RunSessionId).Success &&
                       runtime.TryGetActiveRunProgression(
                           secondBegin.RunSessionId,
                           out RunProgressionService retriedProgression) &&
                       ReferenceEquals(
                           retainedProgression,
                           retriedProgression) &&
                       runtime.TryGetActiveRunAbilityLoadout(
                           secondBegin.RunSessionId,
                           out RunAbilityLoadoutService retriedAbilityLoadout) &&
                       ReferenceEquals(
                           retainedAbilityLoadout,
                           retriedAbilityLoadout) &&
                       runtime.TryGetActiveRunAbilityChoices(
                           secondBegin.RunSessionId,
                           out RunAbilityChoiceService retriedAbilityChoices) &&
                       ReferenceEquals(
                           retainedAbilityChoices,
                           retriedAbilityChoices) &&
                       runtime.TryGetActiveRunStartReadiness(
                           secondBegin.RunSessionId,
                           out RunStartReadinessService retriedReadiness) &&
                       ReferenceEquals(
                           retainedStartReadiness,
                           retriedReadiness),
                    "Failed Hub loading lost retryable temporary run state.");
                Assert(runtime.GameSession.TryConcludeRun(result).Success &&
                       runtime.GameSession.TryEnterHub(
                           secondBegin.RunSessionId).Success &&
                       !runtime.TryGetActiveRunProgression(
                           secondBegin.RunSessionId,
                           out _) &&
                       !runtime.TryGetActiveRunAbilityLoadout(
                           secondBegin.RunSessionId,
                           out _) &&
                       !runtime.TryGetActiveRunAbilityChoices(
                           secondBegin.RunSessionId,
                           out _) &&
                       !runtime.TryGetActiveRunStartReadiness(
                           secondBegin.RunSessionId,
                           out _) &&
                       runtime.AccountCrystals.Amount == 25,
                    "Hub entry did not clear the temporary run state.");

                GameSessionCommandResult unseededBegin =
                    runtime.GameSession.TryBeginRun(
                        new RunLaunchCommand(
                            "difficulty:prototype",
                            789,
                            new[]
                            {
                                new RunParticipantSelection(
                                    "player:local",
                                    "character:warrior")
                            }));
                Assert(unseededBegin.Success &&
                       runtime.TryGetActiveRunAbilityLoadout(
                           unseededBegin.RunSessionId,
                           out RunAbilityLoadoutService unseededLoadout) &&
                       unseededLoadout.TryGetParticipant(
                           "player:local",
                           out RunParticipantAbilityState unseededAbilityState) &&
                       unseededAbilityState.TryGetAbilitySlot(
                           RunStartReadinessService.StartingAbilitySlotIndex,
                           out string unseededAbilityId) &&
                       unseededAbilityId.Length == 0 &&
                       runtime.TryGetActiveRunStartReadiness(
                           unseededBegin.RunSessionId,
                           out RunStartReadinessService unseededReadiness) &&
                       !unseededReadiness.AllParticipantsConfirmed &&
                       !unseededReadiness.IsSealed,
                    "A run without a seeded starter did not wait for a choice.");
                Assert(runtime.GameSession.TryCancelRunTransition(
                           unseededBegin.RunSessionId).Success &&
                       !runtime.TryGetActiveRunStartReadiness(
                           unseededBegin.RunSessionId,
                           out _),
                    "Cancelling an unready run retained its readiness barrier.");

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
