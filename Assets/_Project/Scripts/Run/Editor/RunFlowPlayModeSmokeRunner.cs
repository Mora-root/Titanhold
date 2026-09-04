using System;
using Titanhold.Combat;
using Titanhold.Session;
using Titanhold.UI.Run;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Titanhold.Run.Editor
{
    public static class RunFlowPlayModeSmokeRunner
    {
        private const string MenuPath = "Tools/Titanhold/Run Run Flow Play Mode Smoke Test";
        private const string SessionKey = "Titanhold.RunFlow.PlayModeSmokePending";

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
#pragma warning disable UDR0001
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
#pragma warning restore UDR0001
        }

        [MenuItem(MenuPath)]
        public static void StartSmokeTest()
        {
            if (EditorApplication.isPlaying)
            {
                SessionState.SetBool(SessionKey, true);
                RunSmokeTestInPlayMode();
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("Run Flow Play Mode smoke test cannot start while Play Mode is changing.");
                return;
            }

            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
            {
                Debug.LogError("Save the active scene before running the Play Mode smoke test.");
                return;
            }

            SessionState.SetBool(SessionKey, true);
            EditorApplication.isPlaying = true;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode ||
                !SessionState.GetBool(SessionKey, false))
            {
                return;
            }

            RunSmokeTestInPlayMode();
        }

        private static void RunSmokeTestInPlayMode()
        {
            GameObject temporaryEnemy = null;
            GameObject assaultEnemyTemplate = null;
            GameObject assaultSpawnPoint = null;
            GameObject spawnedAssaultEnemy = null;
            GameObject completionSmokeRoot = null;
            AssaultWaveDefinition temporaryWaveDefinition = null;

            try
            {
                RunFlowRuntime runtime =
                    UnityEngine.Object.FindAnyObjectByType<RunFlowRuntime>();
                ExplorationCombatExecutionAdapter adapter =
                    UnityEngine.Object.FindAnyObjectByType<ExplorationCombatExecutionAdapter>();

                Assert(runtime != null, "RunFlowRuntime was not found in Play Mode.");
                Assert(adapter != null, "ExplorationCombatExecutionAdapter was not found in Play Mode.");

                Assert(adapter.HasPlayerCombatSource,
                    "PlayerCombat was not bound automatically in Play Mode.");
                Assert(adapter.HasPlayerSkillSource,
                    "PlayerSkillExecutor was not bound automatically in Play Mode.");
                Assert(runtime.State.Phase == RunPhase.Exploration,
                    "Run Flow did not start in Exploration.");
                Assert(runtime.State.RoundScaling.RoundNumber == 1 &&
                       Math.Abs(runtime.State.RoundScaling.HealthMultiplier - 1f) <= 0.0001f &&
                       Math.Abs(runtime.State.RoundScaling.DamageMultiplier - 1f) <= 0.0001f,
                    "Run Flow did not start with round-one enemy scaling.");
                Assert(runtime.State.FinalRoundNumber == 4 &&
                       runtime.State.CurrentEncounterKind == RunEncounterKind.AssaultWave,
                    "Run Flow did not start on the first of three regular rounds.");

                PlayerCombat playerCombat =
                    UnityEngine.Object.FindAnyObjectByType<PlayerCombat>();
                Assert(playerCombat != null,
                    "PlayerCombat was not found for portal smoke validation.");

                RunPortalSpawner portalSpawner =
                    runtime.GetComponent<RunPortalSpawner>();
                Assert(portalSpawner != null,
                    "Serialized RunPortalSpawner wiring was not found in Play Mode.");

                AssaultEnemyRegistry assaultRegistry =
                    runtime.GetComponent<AssaultEnemyRegistry>();
                AssaultTargetRegistry assaultTargetRegistry =
                    runtime.GetComponent<AssaultTargetRegistry>();
                AssaultWaveSpawner assaultSpawner =
                    runtime.GetComponent<AssaultWaveSpawner>();
                LocalAssaultArenaGateway arenaGateway =
                    runtime.GetComponent<LocalAssaultArenaGateway>();
                AssaultArenaTransitionController transitionController =
                    runtime.GetComponent<AssaultArenaTransitionController>();
                AssaultReturnPortalSpawner returnPortalSpawner =
                    runtime.GetComponent<AssaultReturnPortalSpawner>();
                AssaultRewardChestSpawner rewardChestSpawner =
                    runtime.GetComponent<AssaultRewardChestSpawner>();
                Assert(assaultRegistry != null &&
                       assaultTargetRegistry != null &&
                       assaultSpawner != null &&
                       arenaGateway != null &&
                       transitionController != null &&
                       returnPortalSpawner != null &&
                       rewardChestSpawner != null,
                    "Serialized Assault arena wiring was not found in Play Mode.");

                Vector3 explorationPosition = playerCombat.transform.position;
                SerializedObject serializedSpawner =
                    new SerializedObject(assaultSpawner);
                AssaultWaveDefinition installedDefinition =
                    serializedSpawner.FindProperty("waveDefinition")
                        .objectReferenceValue as AssaultWaveDefinition;
                Assert(installedDefinition != null,
                    "Installed Assault wave definition is missing.");
                Assert(installedDefinition.TryCreatePlan(
                        out AssaultWavePlan installedPlan,
                        out _),
                    "Installed Assault wave definition is invalid.");
                assaultEnemyTemplate = installedPlan.Steps[0].EnemyPrefab;
                Health assaultTemplateHealth =
                    assaultEnemyTemplate.GetComponentInChildren<Health>(true);
                EnemyCombat assaultTemplateCombat =
                    assaultEnemyTemplate.GetComponentInChildren<EnemyCombat>(true);
                Assert(assaultTemplateHealth != null && assaultTemplateCombat != null,
                    "Installed Assault enemy has no scalable combat components.");
                float assaultBaseMaxHealth = assaultTemplateHealth.MaxHealth;
                float assaultBaseDamage = assaultTemplateCombat.Damage;
                assaultSpawnPoint = new GameObject("AssaultWave_PlayModeSmokeSpawnPoint");
                assaultSpawnPoint.transform.position =
                    arenaGateway.AssaultDestination.position + Vector3.forward * 4f;
                temporaryWaveDefinition =
                    ScriptableObject.CreateInstance<AssaultWaveDefinition>();
                AssaultWaveDefinitionValidationRunner.ConfigureDefinition(
                    temporaryWaveDefinition,
                    assaultEnemyTemplate,
                    initialDelay: 0f,
                    enemyCount: 1,
                    delayBeforeGroup: 0f,
                    spawnInterval: 0f);

                serializedSpawner.FindProperty("waveDefinition").objectReferenceValue =
                    temporaryWaveDefinition;
                SerializedProperty spawnPoints =
                    serializedSpawner.FindProperty("spawnPoints");
                spawnPoints.arraySize = 1;
                spawnPoints.GetArrayElementAtIndex(0).objectReferenceValue =
                    assaultSpawnPoint.transform;
                serializedSpawner.ApplyModifiedPropertiesWithoutUndo();

                int completedSpawnSequenceCount = 0;
                assaultSpawner.EnemySpawned += (enemyObject, _) =>
                    spawnedAssaultEnemy = enemyObject;
                assaultSpawner.SpawnSequenceCompleted += _ =>
                    completedSpawnSequenceCount++;

                temporaryEnemy = new GameObject("RunFlow_PlayModeSmokeEnemy");
                EnemyRunContributionSource contributionSource =
                    temporaryEnemy.AddComponent<EnemyRunContributionSource>();
                SerializedObject serializedContribution =
                    new SerializedObject(contributionSource);
                serializedContribution.FindProperty("threatAmount").floatValue = 100f;
                serializedContribution.ApplyModifiedPropertiesWithoutUndo();
                Health health = temporaryEnemy.GetComponent<Health>();
                CombatExecutionId executionId = CombatExecutionId.New();
                CombatActorReference player = new CombatActorReference(
                    "player:play-mode-smoke",
                    CombatActorKind.Player);
                DamageRequest request = new DamageRequest(
                    executionId,
                    player,
                    100f,
                    DamageCause.Ability,
                    "ability:play-mode-smoke");
                DeathContext deathContext = new DeathContext(request, 100f);
                DamageResult damageResult = DamageResult.Applied(
                    request,
                    100f,
                    0f,
                    100f,
                    true,
                    deathContext);
                CombatExecutionReport report = CombatExecutionReport.Single(
                    executionId,
                    new DamageTargetResolution(health, damageResult));

                Assert(adapter.TryApplyReport(report, out ExplorationKillApplicationResult result),
                    "Play Mode adapter did not find the temporary eligible kill.");
                Assert(result.Success, $"Play Mode kill batch failed: {result.Error}.");
                Assert(Math.Abs(runtime.State.CurrentThreat - contributionSource.ThreatAmount) <= 0.0001f,
                    "Play Mode Threat did not match the enemy contribution.");
                Assert(runtime.State.Phase == RunPhase.PortalOpen,
                    "Play Mode lethal report did not open the portal phase.");
                Assert(portalSpawner.HasActivePortal,
                    "RunPortalSpawner did not create a portal near the player.");
                ExplorationKillBatchResult instability =
                    runtime.Service.TryRegisterExplorationKill(
                        new ExplorationKillContribution(0f, 20));
                Assert(instability.Success &&
                       runtime.State.RiftInstability.Level == 2,
                    "Play Mode setup did not create Assault instability.");
                int interactableLayer = LayerMask.NameToLayer("Interactable");
                Assert(interactableLayer >= 0 &&
                       portalSpawner.ActivePortal.gameObject.layer == interactableLayer,
                    "Spawned portal does not use the Interactable layer.");

                portalSpawner.ActivePortal.Interact(playerCombat.gameObject);

                Assert(runtime.State.Phase == RunPhase.Assault,
                    "Portal interaction did not enter the Assault phase.");
                Assert(!portalSpawner.HasActivePortal,
                    "RunPortalSpawner retained the portal after entry.");
                Assert(arenaGateway.IsOccupied &&
                       arenaGateway.Occupant == playerCombat.transform &&
                       Vector3.Distance(
                           playerCombat.transform.position,
                           arenaGateway.AssaultDestination.position) <= 2f,
                    "Assault gateway did not move the player to the arena.");
                Assert(spawnedAssaultEnemy != null &&
                       assaultSpawner.SpawnedEnemyCount == 1 &&
                       completedSpawnSequenceCount == 1 &&
                       !assaultSpawner.IsSpawning,
                    "Runtime Assault wave did not finish its spawn sequence.");
                Assert(
                    spawnedAssaultEnemy.GetComponentInChildren<EnemyRewardSource>(true) != null &&
                    spawnedAssaultEnemy.GetComponentInChildren<EnemyLootTableDropper>(true) == null &&
                    spawnedAssaultEnemy.GetComponentInChildren<EnemyRunContributionSource>(true) == null &&
                    spawnedAssaultEnemy.GetComponentInChildren<EnemyThreatSource>(true) == null,
                    "Runtime Assault enemy reward composition is invalid.");
                AssaultAggroTargetProvider assaultTargetProvider =
                    spawnedAssaultEnemy.GetComponentInChildren<
                        AssaultAggroTargetProvider>(true);
                Assert(assaultTargetProvider != null &&
                       assaultTargetProvider.IsBound &&
                       assaultTargetProvider.CurrentTargetActor.IsPlayer &&
                       assaultTargetProvider.GetTarget() != null,
                    "Runtime Assault enemy did not acquire the player target.");

                Health assaultHealth = spawnedAssaultEnemy.GetComponent<Health>();
                EnemyCombat assaultCombat =
                    spawnedAssaultEnemy.GetComponent<EnemyCombat>();
                AssaultScalingSnapshot scaling = runtime.State.AssaultScaling;
                Assert(assaultCombat != null &&
                       Math.Abs(
                           assaultHealth.MaxHealth -
                           assaultBaseMaxHealth * scaling.HealthMultiplier) <=
                       0.0001f &&
                       Math.Abs(
                           assaultCombat.Damage -
                           assaultBaseDamage * scaling.DamageMultiplier) <=
                       0.0001f &&
                       Math.Abs(
                           assaultHealth.CurrentHealth -
                           assaultHealth.MaxHealth) <= 0.0001f,
                    "Runtime Assault enemy did not receive its scaling snapshot.");
                DamageRequest assaultDamageRequest = new DamageRequest(
                    CombatExecutionId.New(),
                    player,
                    assaultHealth.MaxHealth * 2f,
                    DamageCause.Ability,
                    "ability:assault-play-mode-smoke");
                DamageResult assaultDamage =
                    assaultHealth.ApplyDamage(assaultDamageRequest);
                Assert(assaultDamage.Killed &&
                       runtime.AssaultEncounter.State.IsCompleted &&
                       runtime.State.Phase == RunPhase.Intermission &&
                       returnPortalSpawner.HasActivePortal &&
                       runtime.AssaultReward.State.HasReward &&
                       !runtime.AssaultReward.State.IsClaimed &&
                       rewardChestSpawner.HasActiveChest,
                    "Runtime Assault death event did not complete the encounter.");

                AssaultRewardChestResult rewardChestResult = default;
                rewardChestSpawner.ActiveChest.OpenResolved += result =>
                    rewardChestResult = result;
                rewardChestSpawner.ActiveChest.Interact(playerCombat.gameObject);
                Assert(rewardChestResult.Success &&
                       rewardChestResult.EmissionResult.EmittedDropCount ==
                           runtime.AssaultReward.State.Drops.Count &&
                       runtime.AssaultReward.State.IsClaimed &&
                       runtime.AssaultReward.State.ClaimedBy ==
                           playerCombat.ActorReference &&
                       !rewardChestSpawner.ActiveChest.IsInteractable,
                    "Assault reward chest did not emit its fixed reward once.");

                AssaultReturnPortalResult returnResult = default;
                returnPortalSpawner.ActivePortal.ReturnResolved += result =>
                    returnResult = result;
                returnPortalSpawner.ActivePortal.Interact(playerCombat.gameObject);
                Assert(returnResult.Success &&
                       runtime.State.Phase == RunPhase.Exploration &&
                       runtime.State.RoundNumber == 2 &&
                       runtime.State.RoundScaling.RoundNumber == 2 &&
                       Math.Abs(runtime.State.RoundScaling.HealthMultiplier - 1.20f) <= 0.0001f &&
                       Math.Abs(runtime.State.RoundScaling.DamageMultiplier - 1.10f) <= 0.0001f &&
                       !arenaGateway.IsOccupied &&
                       !returnPortalSpawner.HasActivePortal &&
                       !runtime.AssaultReward.State.HasReward &&
                       assaultTargetRegistry.Count == 0,
                    "Assault return portal did not resume the next exploration round.");
                Assert(Vector3.Distance(
                           playerCombat.transform.position,
                           explorationPosition) <= 2f,
                    "Assault gateway did not restore the exploration position.");

                completionSmokeRoot = ValidateRunCompletionUi();

                RunParticipantDefeatController defeatController =
                    UnityEngine.Object.FindAnyObjectByType<
                        RunParticipantDefeatController>();
                RunSessionExitController sessionExit =
                    UnityEngine.Object.FindAnyObjectByType<
                        RunSessionExitController>();
                Assert(defeatController != null &&
                       defeatController.HasRequiredReferences &&
                       sessionExit != null &&
                       sessionExit.HasRequiredReferences,
                    "Run defeat session wiring was not found in Play Mode.");
                Health participantHealth =
                    defeatController.ParticipantHealth[0];
                PlayerBrain participantBrain =
                    participantHealth.GetComponent<PlayerBrain>();
                Assert(participantBrain != null,
                    "Run participant PlayerBrain was not found.");
                participantHealth.TakeDamage(float.MaxValue);
                Assert(!participantHealth.IsAlive &&
                       runtime.State.Phase == RunPhase.Failed &&
                       sessionExit.CompletionView.Mode ==
                           RunCompletionViewMode.Defeat,
                    "Last participant death did not show the defeat state.");
                Assert(participantBrain.IsDead &&
                       participantBrain.StateMachine.CurrentState == null &&
                       participantBrain.Movement.IsStopped &&
                       !participantBrain.Combat.IsAttacking &&
                       !participantBrain.Skills.IsUsingSkill &&
                       !participantBrain.HasQueuedSkillCommand,
                    "Dead participant retained an active gameplay command: " +
                    $"dead={participantBrain.IsDead}, " +
                    $"state={participantBrain.StateMachine.CurrentState?.GetType().Name ?? "null"}, " +
                    $"movementStopped={participantBrain.Movement.IsStopped}, " +
                    $"attacking={participantBrain.Combat.IsAttacking}, " +
                    $"usingSkill={participantBrain.Skills.IsUsingSkill}, " +
                    $"queuedSkill={participantBrain.HasQueuedSkillCommand}.");

                Debug.Log("Run Flow Play Mode smoke test passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"Run Flow Play Mode smoke test failed: {exception}");
            }
            finally
            {
                if (temporaryEnemy != null)
                    UnityEngine.Object.Destroy(temporaryEnemy);

                if (spawnedAssaultEnemy != null)
                    UnityEngine.Object.Destroy(spawnedAssaultEnemy);

                if (assaultSpawnPoint != null)
                    UnityEngine.Object.Destroy(assaultSpawnPoint);

                if (temporaryWaveDefinition != null)
                    UnityEngine.Object.Destroy(temporaryWaveDefinition);

                if (completionSmokeRoot != null)
                    UnityEngine.Object.Destroy(completionSmokeRoot);

                SessionState.SetBool(SessionKey, false);
                EditorApplication.isPlaying = false;
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static GameObject ValidateRunCompletionUi()
        {
            GameObject root = new GameObject("RunCompletion_PlayModeSmoke");
            root.SetActive(false);

            RunFlowRuntime runtime = root.AddComponent<RunFlowRuntime>();
            SerializedObject serializedRuntime = new SerializedObject(runtime);
            serializedRuntime.FindProperty("startingRound").intValue = 4;
            serializedRuntime.ApplyModifiedPropertiesWithoutUndo();

            GameObject uiObject = new GameObject(
                "RunCompletionUI",
                typeof(RectTransform));
            uiObject.transform.SetParent(root.transform, false);
            RunCompletionView view = uiObject.AddComponent<RunCompletionView>();
            RunCompletionController controller =
                uiObject.AddComponent<RunCompletionController>();

            GameObject victoryPanel = CreateSmokePanel(uiObject.transform, "VictoryPanel");
            GameObject collapsedPanel = CreateSmokePanel(uiObject.transform, "CollapsedPanel");
            GameObject confirmationPanel = CreateSmokePanel(uiObject.transform, "ConfirmationPanel");
            GameObject completedPanel = CreateSmokePanel(uiObject.transform, "CompletedPanel");
            GameObject defeatPanel = CreateSmokePanel(uiObject.transform, "DefeatPanel");
            Button continueButton = CreateSmokeButton(uiObject.transform, "Continue");
            Button victoryCompleteButton = CreateSmokeButton(uiObject.transform, "VictoryComplete");
            Button collapsedCompleteButton = CreateSmokeButton(uiObject.transform, "CollapsedComplete");
            Button cancelButton = CreateSmokeButton(uiObject.transform, "Cancel");
            Button confirmButton = CreateSmokeButton(uiObject.transform, "Confirm");
            Button returnToHubButton = CreateSmokeButton(uiObject.transform, "ReturnToHub");
            Button defeatReturnToHubButton = CreateSmokeButton(uiObject.transform, "DefeatReturnToHub");

            SerializedObject serializedView = new SerializedObject(view);
            serializedView.FindProperty("victoryPanel").objectReferenceValue = victoryPanel;
            serializedView.FindProperty("collapsedPanel").objectReferenceValue = collapsedPanel;
            serializedView.FindProperty("confirmationPanel").objectReferenceValue = confirmationPanel;
            serializedView.FindProperty("completedPanel").objectReferenceValue = completedPanel;
            serializedView.FindProperty("defeatPanel").objectReferenceValue = defeatPanel;
            serializedView.FindProperty("continueCollectingButton").objectReferenceValue = continueButton;
            serializedView.FindProperty("victoryCompleteButton").objectReferenceValue = victoryCompleteButton;
            serializedView.FindProperty("collapsedCompleteButton").objectReferenceValue = collapsedCompleteButton;
            serializedView.FindProperty("cancelCompletionButton").objectReferenceValue = cancelButton;
            serializedView.FindProperty("confirmCompletionButton").objectReferenceValue = confirmButton;
            serializedView.FindProperty("returnToHubButton").objectReferenceValue = returnToHubButton;
            serializedView.FindProperty("defeatReturnToHubButton").objectReferenceValue = defeatReturnToHubButton;
            serializedView.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("runFlowRuntime").objectReferenceValue = runtime;
            serializedController.FindProperty("view").objectReferenceValue = view;
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(true);
            Assert(runtime.State.RoundNumber == 4 &&
                   runtime.State.CurrentEncounterKind == RunEncounterKind.Boss,
                "Run Completion smoke did not start on the boss round.");
            Assert(view.Mode == RunCompletionViewMode.Hidden,
                "Run Completion UI was visible before the final intermission.");

            Assert(runtime.Service.TryRegisterExplorationKill(
                    new ExplorationKillContribution(100f, 0)).Success,
                "Run Completion smoke could not fill the boss-round meter.");
            Assert(runtime.Service.TryBeginAssaultTransition().Success &&
                   runtime.Service.TryStartAssault().Success &&
                   runtime.Service.TryCompleteAssault().Success,
                "Run Completion smoke could not enter the final intermission.");
            Assert(view.Mode == RunCompletionViewMode.Victory,
                "Run Completion UI did not show victory after the boss.");

            continueButton.onClick.Invoke();
            Assert(view.Mode == RunCompletionViewMode.Collapsed,
                "Run Completion UI did not collapse for reward collection.");
            collapsedCompleteButton.onClick.Invoke();
            Assert(view.Mode == RunCompletionViewMode.Confirmation,
                "Run Completion UI did not request confirmation.");
            cancelButton.onClick.Invoke();
            Assert(view.Mode == RunCompletionViewMode.Collapsed,
                "Run Completion UI did not restore the collapsed state after cancellation.");
            collapsedCompleteButton.onClick.Invoke();
            confirmButton.onClick.Invoke();
            Assert(runtime.State.Phase == RunPhase.Completed &&
                   view.Mode == RunCompletionViewMode.Completed,
                "Run Completion confirmation did not complete the run.");

            int returnRequestCount = 0;
            view.ReturnToHubRequested += () => returnRequestCount++;
            returnToHubButton.onClick.Invoke();
            Assert(returnRequestCount == 1,
                "Completed Run UI did not emit the Hub return request.");

            return root;
        }

        private static GameObject CreateSmokePanel(Transform parent, string name)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            return panel;
        }

        private static Button CreateSmokeButton(Transform parent, string name)
        {
            GameObject buttonObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            return buttonObject.GetComponent<Button>();
        }
    }
}
