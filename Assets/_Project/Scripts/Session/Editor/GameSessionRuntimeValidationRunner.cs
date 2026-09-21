using System;
using Titanhold.Combat;
using Titanhold.Combat.Abilities;
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

                IRunStartingAbilityPoolResolver startingPools =
                    CreateStartingPools();
                IRunCombatResourceLoadoutResolver combatResourceLoadouts =
                    CreateCombatResourceLoadouts();
                IRunAbilityUnlockScheduleResolver abilityUnlockSchedules =
                    CreateAbilityUnlockSchedules();
                IRunUpgradeDefinitionResolver runUpgrades =
                    CreateRunUpgrades();
                IRunUpgradeUnlockScheduleResolver upgradeUnlockSchedules =
                    CreateUpgradeUnlockSchedules(runUpgrades);
                GameSessionRuntime runtime = new(
                    catalog,
                    CreateRewardPolicy(),
                    runExperienceCurve:
                        new RunExperienceCurve(new[] { 10, 20 }),
                    startingAbilityPools: startingPools,
                    combatResourceLoadouts: combatResourceLoadouts,
                    abilityUnlockSchedules: abilityUnlockSchedules,
                    runUpgrades: runUpgrades,
                    upgradeUnlockSchedules: upgradeUnlockSchedules);
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
                                "archetype:warrior",
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
                Assert(runtime.TryGetActiveRunCombatResources(
                       begin.RunSessionId,
                       out RunCombatResourceService combatResources) &&
                       combatResources.ParticipantCount == 1 &&
                       combatResources.TryGetResource(
                           "player:local",
                           "resource:rage",
                           out CombatResourceSnapshot rage) &&
                       rage.Maximum == 8f &&
                       rage.Current == 0f &&
                       combatResources.TryCreateParticipantGateway(
                           "player:local",
                           out ICombatResourceGateway resourceGateway) &&
                       resourceGateway.TryGain(
                           CombatExecutionId.New(),
                           "resource:rage",
                           2f),
                    "Run transition did not create participant combat resources.");
                RunParticipantAbilityState abilityState = null;
                RunUpgradeChoiceService upgradeChoices = null;
                Assert(runtime.TryGetActiveRunAbilityLoadout(
                           begin.RunSessionId,
                           out RunAbilityLoadoutService abilityLoadout) &&
                       abilityLoadout.ParticipantCount == 1 &&
                       abilityLoadout.TryGetParticipant(
                           "player:local",
                           out abilityState) &&
                       abilityState.HasAbility("ability:spin") &&
                       abilityState.TryGetAbilitySlot(
                           0,
                           out string startingAbilityId) &&
                       startingAbilityId == "ability:spin",
                    "Run transition did not seed the participant starting ability.");
                RunAbilityChoiceState levelChoice = null;
                Assert(runtime.TryGetActiveRunAbilityChoices(
                           begin.RunSessionId,
                           out RunAbilityChoiceService abilityChoices) &&
                       abilityChoices.TryGetPendingChoice(
                           "player:local",
                           out levelChoice) &&
                       levelChoice.TargetSlotIndex == 1 &&
                       levelChoice.OfferedAbilityIds.Count == 2 &&
                       runtime.TryGetActiveRunLevelAbilitySelection(
                           begin.RunSessionId,
                           out RunLevelAbilitySelectionService _) &&
                       runtime.TryGetActiveRunUpgradeChoices(
                           begin.RunSessionId,
                           out upgradeChoices) &&
                       runtime.TryGetActiveRunLevelUpgradeSelection(
                           begin.RunSessionId,
                           out RunLevelUpgradeSelectionService _) &&
                       runtime.TryGetActiveRunLevelRewardSelection(
                           begin.RunSessionId,
                           out RunLevelRewardSelectionService _) &&
                       runtime.TryGetActiveRunUpgradeStatApplication(
                           begin.RunSessionId,
                           out RunUpgradeStatApplicationService _),
                    "Run level did not create its configured ability choice.");
                string unlockedAbilityId =
                    levelChoice.OfferedAbilityIds[0];
                Assert(abilityChoices.TrySelectAbility(
                           "player:local",
                           levelChoice.ChoiceId,
                           unlockedAbilityId).Success &&
                       abilityState.TryGetAbilitySlot(
                           1,
                           out string assignedUnlockId) &&
                       assignedUnlockId == unlockedAbilityId,
                    "Run level ability choice did not update its target slot.");
                Assert(progression.TryGrantExperience(
                           "player:local",
                           15).Success &&
                       upgradeChoices.TryGetPendingChoice(
                           "player:local",
                           out RunUpgradeChoiceState upgradeChoice) &&
                       upgradeChoice.OfferedUpgradeIds.Count == 3 &&
                       upgradeChoices.TrySelectUpgrade(
                           "player:local",
                           upgradeChoice.ChoiceId,
                           upgradeChoice.OfferedUpgradeIds[0]).Success &&
                       upgradeChoices.TryGetParticipant(
                           "player:local",
                           out RunParticipantUpgradeState upgradeState) &&
                       upgradeState.SelectionHistory.Count == 1,
                    "Run level did not create and resolve its configured upgrade choice.");
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
                Assert(runtime.TryGetActiveRunStartingAbilitySelection(
                           begin.RunSessionId,
                           out RunStartingAbilitySelectionService
                               startingSelection) &&
                       startingSelection.TryOfferStartingChoice(
                           new RunStartingAbilityChoiceRequest(
                               "player:local",
                               "choice:starter",
                               new[]
                               {
                                   "ability:strike",
                                   "ability:slash",
                                   "ability:bash"
                               },
                               10)).Error ==
                           RunStartingAbilitySelectionError
                               .ReadinessAlreadySealed,
                    "Runtime did not expose the sealed starting selection service.");

                GameSessionCommandResult cancel =
                    runtime.GameSession.TryCancelRunTransition(
                        begin.RunSessionId);
                Assert(cancel.Success &&
                       !runtime.TryGetActiveRunProgression(
                           begin.RunSessionId,
                           out _) &&
                       !runtime.TryGetActiveRunCombatResources(
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
                       !runtime.TryGetActiveRunStartingAbilitySelection(
                           begin.RunSessionId,
                           out _) &&
                       !runtime.TryGetActiveRunLevelAbilitySelection(
                           begin.RunSessionId,
                           out _) &&
                       !runtime.TryGetActiveRunUpgradeChoices(
                           begin.RunSessionId,
                           out _) &&
                       !runtime.TryGetActiveRunLevelUpgradeSelection(
                           begin.RunSessionId,
                           out _) &&
                       !runtime.TryGetActiveRunLevelRewardSelection(
                           begin.RunSessionId,
                           out _) &&
                       !runtime.TryGetActiveRunUpgradeStatApplication(
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
                                    "archetype:warrior",
                                    "ability:spin")
                            }));
                RunProgressionService retainedProgression = null;
                RunCombatResourceService retainedCombatResources = null;
                RunAbilityLoadoutService retainedAbilityLoadout = null;
                RunAbilityChoiceService retainedAbilityChoices = null;
                RunStartReadinessService retainedStartReadiness = null;
                RunStartingAbilitySelectionService retainedStartingSelection =
                    null;
                RunUpgradeChoiceService retainedUpgradeChoices = null;
                RunLevelUpgradeSelectionService retainedUpgradeSelection =
                    null;
                RunLevelRewardSelectionService retainedRewardSelection = null;
                RunUpgradeStatApplicationService retainedStatApplication =
                    null;
                Assert(secondBegin.Success &&
                       runtime.TryGetActiveRunProgression(
                           secondBegin.RunSessionId,
                           out retainedProgression) &&
                       runtime.TryGetActiveRunCombatResources(
                           secondBegin.RunSessionId,
                           out retainedCombatResources) &&
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
                       runtime.TryGetActiveRunStartingAbilitySelection(
                           secondBegin.RunSessionId,
                           out retainedStartingSelection) &&
                       runtime.TryGetActiveRunUpgradeChoices(
                           secondBegin.RunSessionId,
                           out retainedUpgradeChoices) &&
                       runtime.TryGetActiveRunLevelUpgradeSelection(
                           secondBegin.RunSessionId,
                           out retainedUpgradeSelection) &&
                       runtime.TryGetActiveRunLevelRewardSelection(
                           secondBegin.RunSessionId,
                           out retainedRewardSelection) &&
                       runtime.TryGetActiveRunUpgradeStatApplication(
                           secondBegin.RunSessionId,
                           out retainedStatApplication) &&
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
                       runtime.TryGetActiveRunCombatResources(
                           secondBegin.RunSessionId,
                           out RunCombatResourceService transitionResources) &&
                       ReferenceEquals(
                           retainedCombatResources,
                           transitionResources) &&
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
                           transitionReadiness) &&
                       runtime.TryGetActiveRunStartingAbilitySelection(
                           secondBegin.RunSessionId,
                           out RunStartingAbilitySelectionService
                               transitionStartingSelection) &&
                       ReferenceEquals(
                           retainedStartingSelection,
                           transitionStartingSelection) &&
                       runtime.TryGetActiveRunUpgradeChoices(
                           secondBegin.RunSessionId,
                           out RunUpgradeChoiceService
                               transitionUpgradeChoices) &&
                       ReferenceEquals(
                           retainedUpgradeChoices,
                           transitionUpgradeChoices) &&
                       runtime.TryGetActiveRunLevelUpgradeSelection(
                           secondBegin.RunSessionId,
                           out RunLevelUpgradeSelectionService
                               transitionUpgradeSelection) &&
                       ReferenceEquals(
                           retainedUpgradeSelection,
                           transitionUpgradeSelection) &&
                       runtime.TryGetActiveRunLevelRewardSelection(
                           secondBegin.RunSessionId,
                           out RunLevelRewardSelectionService
                               transitionRewardSelection) &&
                       ReferenceEquals(
                           retainedRewardSelection,
                           transitionRewardSelection) &&
                       runtime.TryGetActiveRunUpgradeStatApplication(
                           secondBegin.RunSessionId,
                           out RunUpgradeStatApplicationService
                               transitionStatApplication) &&
                       ReferenceEquals(
                           retainedStatApplication,
                           transitionStatApplication),
                    "Hub transition cleared temporary run state before rewards could settle.");
                Assert(runtime.GameSession.TryCancelHubTransition(
                           secondBegin.RunSessionId).Success &&
                       runtime.TryGetActiveRunProgression(
                           secondBegin.RunSessionId,
                           out RunProgressionService retriedProgression) &&
                       ReferenceEquals(
                           retainedProgression,
                           retriedProgression) &&
                       runtime.TryGetActiveRunCombatResources(
                           secondBegin.RunSessionId,
                           out RunCombatResourceService retriedResources) &&
                       ReferenceEquals(
                           retainedCombatResources,
                           retriedResources) &&
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
                           retriedReadiness) &&
                       runtime.TryGetActiveRunStartingAbilitySelection(
                           secondBegin.RunSessionId,
                           out RunStartingAbilitySelectionService
                               retriedStartingSelection) &&
                       ReferenceEquals(
                           retainedStartingSelection,
                           retriedStartingSelection) &&
                       runtime.TryGetActiveRunUpgradeChoices(
                           secondBegin.RunSessionId,
                           out RunUpgradeChoiceService
                               retriedUpgradeChoices) &&
                       ReferenceEquals(
                           retainedUpgradeChoices,
                           retriedUpgradeChoices) &&
                       runtime.TryGetActiveRunLevelUpgradeSelection(
                           secondBegin.RunSessionId,
                           out RunLevelUpgradeSelectionService
                               retriedUpgradeSelection) &&
                       ReferenceEquals(
                           retainedUpgradeSelection,
                           retriedUpgradeSelection) &&
                       runtime.TryGetActiveRunLevelRewardSelection(
                           secondBegin.RunSessionId,
                           out RunLevelRewardSelectionService
                               retriedRewardSelection) &&
                       ReferenceEquals(
                           retainedRewardSelection,
                           retriedRewardSelection) &&
                       runtime.TryGetActiveRunUpgradeStatApplication(
                           secondBegin.RunSessionId,
                           out RunUpgradeStatApplicationService
                               retriedStatApplication) &&
                       ReferenceEquals(
                           retainedStatApplication,
                           retriedStatApplication),
                    "Failed Hub loading lost retryable temporary run state.");
                Assert(runtime.GameSession.TryConcludeRun(result).Success &&
                       runtime.GameSession.TryEnterHub(
                           secondBegin.RunSessionId).Success &&
                       !runtime.TryGetActiveRunProgression(
                           secondBegin.RunSessionId,
                           out _) &&
                       !runtime.TryGetActiveRunCombatResources(
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
                       !runtime.TryGetActiveRunStartingAbilitySelection(
                           secondBegin.RunSessionId,
                           out _) &&
                       !runtime.TryGetActiveRunUpgradeChoices(
                           secondBegin.RunSessionId,
                           out _) &&
                       !runtime.TryGetActiveRunLevelUpgradeSelection(
                           secondBegin.RunSessionId,
                           out _) &&
                       !runtime.TryGetActiveRunLevelRewardSelection(
                           secondBegin.RunSessionId,
                           out _) &&
                       !runtime.TryGetActiveRunUpgradeStatApplication(
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
                                    "character:warrior",
                                    "archetype:warrior",
                                    string.Empty)
                            }));
                RunAbilityLoadoutService unseededLoadout = null;
                RunParticipantAbilityState unseededAbilityState = null;
                RunAbilityChoiceService unseededChoices = null;
                RunAbilityChoiceState pendingStartingChoice = null;
                RunStartReadinessService unseededReadiness = null;
                RunStartingAbilitySelectionService unseededSelection = null;
                Assert(unseededBegin.Success &&
                       runtime.TryGetActiveRunAbilityLoadout(
                           unseededBegin.RunSessionId,
                           out unseededLoadout) &&
                       unseededLoadout.TryGetParticipant(
                           "player:local",
                           out unseededAbilityState) &&
                       unseededAbilityState.TryGetAbilitySlot(
                           RunStartReadinessService.StartingAbilitySlotIndex,
                           out string unseededAbilityId) &&
                       unseededAbilityId.Length == 0 &&
                       runtime.TryGetActiveRunStartReadiness(
                           unseededBegin.RunSessionId,
                           out unseededReadiness) &&
                       runtime.TryGetActiveRunAbilityChoices(
                           unseededBegin.RunSessionId,
                           out unseededChoices) &&
                       unseededChoices.TryGetPendingChoice(
                           "player:local",
                           out pendingStartingChoice) &&
                       pendingStartingChoice.ChoiceId ==
                           RunStartingAbilitySelectionService.StartingChoiceId &&
                       pendingStartingChoice.TargetSlotIndex ==
                           RunStartReadinessService.StartingAbilitySlotIndex &&
                       pendingStartingChoice.OfferedAbilityIds.Count == 3 &&
                       runtime.TryGetActiveRunStartingAbilitySelection(
                           unseededBegin.RunSessionId,
                           out unseededSelection) &&
                       !unseededReadiness.AllParticipantsConfirmed &&
                       !unseededReadiness.IsSealed,
                    "A run without a seeded starter did not wait for a choice.");
                string runtimeStarterId =
                    pendingStartingChoice.OfferedAbilityIds[0];
                RunStartingAbilitySelectionResult runtimeSelection =
                    unseededSelection.TrySelectStartingAbility(
                        "player:local",
                        RunStartingAbilitySelectionService.StartingChoiceId,
                        runtimeStarterId);
                Assert(runtimeSelection.Success &&
                       runtimeSelection.RosterSealed &&
                       unseededReadiness.IsSealed &&
                       unseededAbilityState.TryGetAbilitySlot(
                           RunStartReadinessService.StartingAbilitySlotIndex,
                           out unseededAbilityId) &&
                       unseededAbilityId == runtimeStarterId,
                    "Runtime starting selection did not complete readiness.");
                Assert(runtime.GameSession.TryCancelRunTransition(
                           unseededBegin.RunSessionId).Success &&
                       !runtime.TryGetActiveRunStartReadiness(
                           unseededBegin.RunSessionId,
                           out _) &&
                       !runtime.TryGetActiveRunStartingAbilitySelection(
                           unseededBegin.RunSessionId,
                           out _) &&
                       !runtime.TryGetActiveRunLevelAbilitySelection(
                           unseededBegin.RunSessionId,
                           out _) &&
                       !runtime.TryGetActiveRunUpgradeChoices(
                           unseededBegin.RunSessionId,
                           out _) &&
                       !runtime.TryGetActiveRunLevelUpgradeSelection(
                           unseededBegin.RunSessionId,
                           out _) &&
                       !runtime.TryGetActiveRunLevelRewardSelection(
                           unseededBegin.RunSessionId,
                           out _) &&
                       !runtime.TryGetActiveRunUpgradeStatApplication(
                           unseededBegin.RunSessionId,
                           out _),
                    "Cancelling a run retained its starting selection state.");

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

        private static IRunStartingAbilityPoolResolver CreateStartingPools()
        {
            IAbilityDefinition[] definitions =
            {
                new TestAbilityDefinition("ability:strike"),
                new TestAbilityDefinition("ability:slash"),
                new TestAbilityDefinition("ability:bash")
            };
            Assert(AbilityDefinitionRegistry.TryCreate(
                       definitions,
                       out AbilityDefinitionRegistry abilityRegistry,
                       out string abilityError),
                $"Could not prepare starting abilities: {abilityError}");
            Assert(RunStartingAbilityPoolRegistry.TryCreate(
                       new[]
                       {
                           new RunStartingAbilityPool(
                               "starting-pool:warrior",
                               "archetype:warrior",
                               new[]
                               {
                                   "ability:strike",
                                   "ability:slash",
                                   "ability:bash"
                               })
                       },
                       abilityRegistry,
                       out RunStartingAbilityPoolRegistry pools,
                       out string poolError),
                $"Could not prepare starting pools: {poolError}");
            return pools;
        }

        private static IRunCombatResourceLoadoutResolver
            CreateCombatResourceLoadouts()
        {
            Assert(RunCombatResourceLoadoutRegistry.TryCreate(
                       new[]
                       {
                           new RunCombatResourceLoadout(
                               "combat-resources:warrior",
                               "archetype:warrior",
                               new[]
                               {
                                   new RunCombatResourceDefinition(
                                       "resource:rage",
                                       8f,
                                       0f)
                               })
                       },
                       out RunCombatResourceLoadoutRegistry loadouts,
                       out string error),
                $"Could not prepare combat resource loadouts: {error}");
            return loadouts;
        }

        private static IRunAbilityUnlockScheduleResolver
            CreateAbilityUnlockSchedules()
        {
            IAbilityDefinition[] definitions =
            {
                new TestAbilityDefinition("ability:unlock-a"),
                new TestAbilityDefinition("ability:unlock-b"),
                new TestAbilityDefinition("ability:unlock-c")
            };
            Assert(AbilityDefinitionRegistry.TryCreate(
                       definitions,
                       out AbilityDefinitionRegistry abilityRegistry,
                       out string abilityError),
                $"Could not prepare unlock abilities: {abilityError}");
            Assert(RunAbilityUnlockSchedule.TryCreate(
                       "ability-schedule:warrior",
                       "archetype:warrior",
                       new[]
                       {
                           new RunAbilityUnlockMilestone(
                               2,
                               1,
                               2,
                               new[]
                               {
                                   "ability:unlock-a",
                                   "ability:unlock-b",
                                   "ability:unlock-c"
                               })
                       },
                       out RunAbilityUnlockSchedule schedule,
                       out string scheduleError),
                $"Could not prepare unlock schedule: {scheduleError}");
            Assert(RunAbilityUnlockScheduleRegistry.TryCreate(
                       new[] { schedule },
                       abilityRegistry,
                       RunAbilityLoadoutService.DefaultAbilitySlotCount,
                       out RunAbilityUnlockScheduleRegistry schedules,
                       out string registryError),
                $"Could not prepare unlock schedule registry: {registryError}");
            return schedules;
        }

        private static IRunUpgradeDefinitionResolver CreateRunUpgrades()
        {
            IRunUpgradeDefinition[] definitions =
            {
                new TestUpgradeDefinition("upgrade:damage"),
                new TestUpgradeDefinition("upgrade:health"),
                new TestUpgradeDefinition("upgrade:armor")
            };
            Assert(RunUpgradeDefinitionRegistry.TryCreate(
                       definitions,
                       out RunUpgradeDefinitionRegistry upgrades,
                       out string error),
                $"Could not prepare run upgrades: {error}");
            return upgrades;
        }

        private static IRunUpgradeUnlockScheduleResolver
            CreateUpgradeUnlockSchedules(
                IRunUpgradeDefinitionResolver upgrades)
        {
            Assert(RunUpgradeUnlockSchedule.TryCreate(
                       "upgrade-schedule:warrior",
                       "archetype:warrior",
                       new[]
                       {
                           new RunUpgradeUnlockMilestone(
                               3,
                               3,
                               new[]
                               {
                                   "upgrade:damage",
                                   "upgrade:health",
                                   "upgrade:armor"
                               })
                       },
                       out RunUpgradeUnlockSchedule schedule,
                       out string scheduleError),
                $"Could not prepare upgrade schedule: {scheduleError}");
            Assert(RunUpgradeUnlockScheduleRegistry.TryCreate(
                       new[] { schedule },
                       upgrades,
                       out RunUpgradeUnlockScheduleRegistry schedules,
                       out string registryError),
                $"Could not prepare upgrade schedule registry: {registryError}");
            return schedules;
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

        private sealed class TestAbilityDefinition : IAbilityDefinition
        {
            public TestAbilityDefinition(string abilityId)
            {
                AbilityId = abilityId;
            }

            public string AbilityId { get; }
        }

        private sealed class TestUpgradeDefinition : IRunUpgradeDefinition
        {
            public TestUpgradeDefinition(string upgradeId)
            {
                UpgradeId = upgradeId;
            }

            public string UpgradeId { get; }

            public bool TryValidate(out string error)
            {
                error = string.Empty;
                return true;
            }
        }
    }
}
