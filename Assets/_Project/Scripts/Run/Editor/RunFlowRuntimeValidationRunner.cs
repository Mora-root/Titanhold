using System;
using System.Collections.Generic;
using Titanhold.Combat;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Run.Editor
{
    public static class RunFlowRuntimeValidationRunner
    {
        private const string MenuPath = "Tools/Titanhold/Validate Run Flow Runtime";

        [MenuItem(MenuPath)]
        public static void ValidateFromMenu()
        {
            try
            {
                Debug.Log(RunValidation());
            }
            catch (Exception exception)
            {
                Debug.LogError($"Run Flow runtime validation failed: {exception}");
            }
        }

        public static string RunValidation()
        {
            ValidateExecutionReportCopiesResolutions();
            ValidateAuthoredRoundBalanceRuntime();
            ValidateRuntimeAdapterAppliesAtomicBatch();

            return "Run Flow runtime validation passed.";
        }

        private static void ValidateAuthoredRoundBalanceRuntime()
        {
            GameObject runtimeObject = null;
            RunRoundBalanceDefinition definition = null;
            try
            {
                RunRoundBalanceEntryDefinition first = new();
                first.ConfigureForEditor(1, 120f, 1f, 1f, 1f);
                RunRoundBalanceEntryDefinition second = new();
                second.ConfigureForEditor(2, 180f, 1.2f, 1.1f, 1.25f);
                definition =
                    ScriptableObject.CreateInstance<RunRoundBalanceDefinition>();
                definition.ConfigureForEditor(new[] { first, second });

                runtimeObject = new GameObject(
                    "RunFlowRuntime_AuthoredBalanceValidation");
                RunFlowRuntime runtime =
                    runtimeObject.AddComponent<RunFlowRuntime>();
                SerializedObject serializedRuntime = new(runtime);
                serializedRuntime.FindProperty("regularRoundCount").intValue = 1;
                serializedRuntime.FindProperty("roundBalanceDefinition")
                    .objectReferenceValue = definition;
                serializedRuntime.ApplyModifiedPropertiesWithoutUndo();

                runtime.EnsureInitialized();
                Assert(ReferenceEquals(
                           runtime.RoundBalanceDefinition,
                           definition),
                    "Run runtime lost its authored round balance reference.");
                AssertApproximately(runtime.State.MaxThreat, 120f,
                    "Runtime authored round-one Threat");
                AssertApproximately(runtime.State.ExperienceMultiplier, 1f,
                    "Runtime authored round-one XP multiplier");

                Assert(runtime.Service.TryRegisterExplorationKill(
                           new ExplorationKillContribution(120f, 0)).Success,
                    "Runtime authored round could not fill its meter.");
                Assert(runtime.Service.TryBeginAssaultTransition().Success &&
                       runtime.Service.TryStartAssault().Success &&
                       runtime.Service.TryCompleteAssault().Success &&
                       runtime.Service.TryBeginReturnToExploration().Success &&
                       runtime.Service.TryResumeExploration().Success,
                    "Runtime authored balance could not advance rounds.");
                AssertApproximately(runtime.State.MaxThreat, 180f,
                    "Runtime authored round-two Threat");
                AssertApproximately(runtime.State.ExperienceMultiplier, 1.25f,
                    "Runtime authored round-two XP multiplier");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runtimeObject);
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        private static void ValidateExecutionReportCopiesResolutions()
        {
            CombatExecutionId executionId = CombatExecutionId.New();
            List<DamageTargetResolution> mutableResolutions =
                new List<DamageTargetResolution> { default };
            CombatExecutionReport report =
                new CombatExecutionReport(executionId, mutableResolutions);

            mutableResolutions.Clear();

            Assert(report.ExecutionId == executionId, "Execution report changed its id.");
            Assert(report.ResolutionCount == 1,
                "Execution report retained a mutable resolution collection.");
        }

        private static void ValidateRuntimeAdapterAppliesAtomicBatch()
        {
            GameObject runtimeObject = new GameObject("RunFlowRuntime_Validation");
            GameObject firstEnemy = new GameObject("RunFlowRuntime_EnemyA");
            GameObject secondEnemy = new GameObject("RunFlowRuntime_EnemyB");
            GameObject thirdEnemy = new GameObject("RunFlowRuntime_EnemyC");

            try
            {
                RunFlowRuntime runtime = runtimeObject.AddComponent<RunFlowRuntime>();
                ExplorationCombatExecutionAdapter adapter =
                    runtimeObject.AddComponent<ExplorationCombatExecutionAdapter>();
                Assert(ReferenceEquals(runtime.AssaultEncounter, runtime.AssaultEncounter),
                    "Runtime recreated its Assault encounter application service.");
                Assert(!runtime.AssaultEncounter.State.IsStarted,
                    "Runtime Assault encounter started without a command.");
                EnemyRunContributionSource firstSource =
                    ConfigureEnemySource(firstEnemy, 60f, 2);
                EnemyRunContributionSource secondSource =
                    ConfigureEnemySource(secondEnemy, 60f, 5);

                CombatActorReference player = new CombatActorReference(
                    "player:runtime-validation",
                    CombatActorKind.Player);
                CombatExecutionId threatExecution = CombatExecutionId.New();
                CombatExecutionReport threatReport = new CombatExecutionReport(
                    threatExecution,
                    new[]
                    {
                        CreateLethalResolution(firstSource, threatExecution, player),
                        CreateLethalResolution(secondSource, threatExecution, player)
                    });

                bool hadEligibleKills = adapter.TryApplyReport(
                    threatReport,
                    out ExplorationKillApplicationResult threatResult);

                Assert(hadEligibleKills, "Runtime adapter did not find eligible exploration kills.");
                Assert(threatResult.Success && threatResult.AcceptedKillCount == 2,
                    "Runtime adapter rejected the atomic Threat batch.");
                Assert(runtime.State.Phase == RunPhase.PortalOpen,
                    "Runtime adapter did not open the portal phase.");
                AssertApproximately(runtime.State.CurrentThreat, 100f,
                    "Runtime adapter Threat");
                Assert(runtime.State.RiftInstability.Points == 0,
                    "Runtime adapter leaked threshold kills into Instability.");

                EnemyRunContributionSource thirdSource =
                    ConfigureEnemySource(thirdEnemy, 10f, 3);
                CombatExecutionId instabilityExecution = CombatExecutionId.New();
                CombatExecutionReport instabilityReport = new CombatExecutionReport(
                    instabilityExecution,
                    new[]
                    {
                        CreateLethalResolution(thirdSource, instabilityExecution, player)
                    });

                Assert(adapter.TryApplyReport(instabilityReport, out ExplorationKillApplicationResult instabilityResult),
                    "Runtime adapter did not find a post-portal kill.");
                Assert(instabilityResult.Success, "Runtime adapter rejected post-portal kill.");
                Assert(runtime.State.RiftInstability.Points == 3,
                    "Runtime adapter did not add Rift Instability.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(thirdEnemy);
                UnityEngine.Object.DestroyImmediate(secondEnemy);
                UnityEngine.Object.DestroyImmediate(firstEnemy);
                UnityEngine.Object.DestroyImmediate(runtimeObject);
            }
        }

        private static EnemyRunContributionSource ConfigureEnemySource(
            GameObject enemy,
            float threatAmount,
            int instabilityPoints)
        {
            EnemyRunContributionSource source = enemy.AddComponent<EnemyRunContributionSource>();
            SerializedObject serializedSource = new SerializedObject(source);
            serializedSource.FindProperty("threatAmount").floatValue = threatAmount;
            serializedSource.FindProperty("instabilityPoints").intValue = instabilityPoints;
            serializedSource.ApplyModifiedPropertiesWithoutUndo();
            return source;
        }

        private static DamageTargetResolution CreateLethalResolution(
            EnemyRunContributionSource contributionSource,
            CombatExecutionId executionId,
            CombatActorReference player)
        {
            DamageRequest request = new DamageRequest(
                executionId,
                player,
                100f,
                DamageCause.Ability,
                "ability:runtime-validation");
            DeathContext deathContext = new DeathContext(request, 100f);
            DamageResult result = DamageResult.Applied(
                request,
                100f,
                0f,
                100f,
                true,
                deathContext);
            Health health = contributionSource.GetComponent<Health>();

            return new DamageTargetResolution(health, result);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static void AssertApproximately(float actual, float expected, string label)
        {
            if (Math.Abs(actual - expected) <= 0.0001f)
                return;

            throw new InvalidOperationException($"{label} failed. Expected {expected}, got {actual}.");
        }
    }
}
