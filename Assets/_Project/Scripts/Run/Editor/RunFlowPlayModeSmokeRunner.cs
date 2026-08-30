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
                EditorApplication.delayCall += RunSmokeTestInPlayMode;
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

            EditorApplication.delayCall += RunSmokeTestInPlayMode;
        }

        private static void RunSmokeTestInPlayMode()
        {
            GameObject temporaryEnemy = null;

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

                Assert(runtime.State.Phase == RunPhase.TransitionToAssault,
                    "Portal interaction did not begin the Assault transition.");
                Assert(!portalSpawner.HasActivePortal,
                    "RunPortalSpawner retained the portal after entry.");

                AssaultEncounterId encounterId =
                    new AssaultEncounterId("assault:play-mode-smoke");
                AssaultEncounterResult encounterStart = runtime.AssaultEncounter.TryBegin(
                    new BeginAssaultEncounterCommand(encounterId, 1, 1));
                Assert(encounterStart.Success && runtime.State.Phase == RunPhase.Assault,
                    "Runtime Assault encounter did not start.");
                CombatActorReference assaultEnemy = new CombatActorReference(
                    "enemy:assault-play-mode-smoke",
                    CombatActorKind.Enemy);
                Assert(runtime.AssaultEncounter.TryRegisterSpawn(
                        new AssaultEnemyCommand(encounterId, assaultEnemy)).Success,
                    "Runtime Assault enemy was not registered.");
                AssaultEncounterResult encounterCompletion =
                    runtime.AssaultEncounter.TryRegisterDefeat(
                        new AssaultEnemyCommand(encounterId, assaultEnemy));
                Assert(encounterCompletion.Success &&
                       encounterCompletion.EncounterCompleted &&
                       runtime.State.Phase == RunPhase.Intermission,
                    "Runtime Assault encounter did not complete.");

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
