using System;
using Titanhold.Combat;
using UnityEditor;
using UnityEngine;

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
                Assert(assaultRegistry != null &&
                       assaultTargetRegistry != null &&
                       assaultSpawner != null &&
                       arenaGateway != null &&
                       transitionController != null,
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
                       runtime.State.Phase == RunPhase.Intermission,
                    "Runtime Assault death event did not complete the encounter.");

                AssaultArenaTransitionResult returned =
                    transitionController.TryReturnToExploration();
                Assert(returned.Success &&
                       runtime.State.Phase == RunPhase.Exploration &&
                       runtime.State.RoundNumber == 2 &&
                       !arenaGateway.IsOccupied &&
                       assaultTargetRegistry.Count == 0,
                    "Assault gateway did not resume the next exploration round.");
                Assert(Vector3.Distance(
                           playerCombat.transform.position,
                           explorationPosition) <= 2f,
                    "Assault gateway did not restore the exploration position.");

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

                SessionState.SetBool(SessionKey, false);
                EditorApplication.isPlaying = false;
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
