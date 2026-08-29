using System;
using Titanhold.Combat;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Run.Editor
{
    public static class ExplorationKillApplicationValidationRunner
    {
        private const string MenuPath = "Tools/Titanhold/Validate Exploration Kill Integration";

        [MenuItem(MenuPath)]
        public static void ValidateFromMenu()
        {
            try
            {
                Debug.Log(RunValidation());
            }
            catch (Exception exception)
            {
                Debug.LogError($"Exploration kill integration validation failed: {exception}");
            }
        }

        public static string RunValidation()
        {
            ValidateAtomicThreatBoundary();
            ValidatePostPortalInstability();
            ValidateReplayProtection();
            ValidateRejectedExecutionCannotReplayInLaterRound();
            ValidateRejectedBatchesAreAtomic();
            ValidateEnemyContributionAdapter();

            return "Exploration kill integration validation passed.";
        }

        private static void ValidateAtomicThreatBoundary()
        {
            RunFlowService runFlow = CreateRunFlowService();
            ExplorationKillApplicationService application =
                new ExplorationKillApplicationService(runFlow);
            CombatActorReference player = CreatePlayer("player:atomic-test");

            ExplorationKillApplicationResult firstResult = application.TryApplyBatch(new[]
            {
                CreateRecord(CombatExecutionId.New(), player, "enemy:first", 50f, 1)
            });

            Assert(firstResult.Success, "Initial attributed kill was rejected.");
            AssertApproximately(runFlow.State.CurrentThreat, 50f, "Initial attributed Threat");

            CombatExecutionId finalExecution = CombatExecutionId.New();
            ExplorationKillApplicationResult finalResult = application.TryApplyBatch(new[]
            {
                CreateRecord(finalExecution, player, "enemy:second", 30f, 2),
                CreateRecord(finalExecution, player, "enemy:third", 30f, 5)
            });

            Assert(finalResult.Success, "Final attributed kill batch was rejected.");
            Assert(finalResult.AcceptedKillCount == 2, "Accepted kill count mismatch.");
            Assert(finalResult.ExecutionId == finalExecution, "Applied execution id mismatch.");
            Assert(finalResult.RunFlowResult.PortalOpened, "Atomic batch did not open the portal.");
            Assert(runFlow.State.Phase == RunPhase.PortalOpen, "Run did not enter PortalOpen.");
            AssertApproximately(runFlow.State.CurrentThreat, 100f, "Clamped attributed Threat");
            Assert(runFlow.State.RiftInstability.Points == 0,
                "Final execution leaked kills into Rift Instability.");
            Assert(application.ProcessedExecutionCount == 2,
                "Processed execution count mismatch.");
        }

        private static void ValidatePostPortalInstability()
        {
            RunFlowService runFlow = CreateRunFlowService();
            ExplorationKillApplicationService application =
                new ExplorationKillApplicationService(runFlow);
            CombatActorReference player = CreatePlayer("player:instability-test");

            Assert(application.TryApplyBatch(new[]
            {
                CreateRecord(CombatExecutionId.New(), player, "enemy:portal", 100f, 1)
            }).Success, "Could not prepare PortalOpen through attributed death.");

            CombatExecutionId executionId = CombatExecutionId.New();
            ExplorationKillApplicationResult result = application.TryApplyBatch(new[]
            {
                CreateRecord(executionId, player, "enemy:post-portal-a", 20f, 6),
                CreateRecord(executionId, player, "enemy:post-portal-b", 20f, 5)
            });

            Assert(result.Success, "Post-portal attributed batch was rejected.");
            AssertApproximately(result.RunFlowResult.ThreatAdded, 0f, "Post-portal Threat");
            Assert(result.RunFlowResult.InstabilityPointsAdded == 11,
                "Post-portal instability contribution mismatch.");
            Assert(runFlow.State.RiftInstability.Level == 1,
                "Post-portal instability level mismatch.");
        }

        private static void ValidateReplayProtection()
        {
            RunFlowService runFlow = CreateRunFlowService();
            ExplorationKillApplicationService application =
                new ExplorationKillApplicationService(runFlow);
            CombatActorReference player = CreatePlayer("player:replay-test");
            ExplorationKillRecord[] batch =
            {
                CreateRecord(CombatExecutionId.New(), player, "enemy:replay", 10f, 1)
            };

            Assert(application.TryApplyBatch(batch).Success, "Initial execution was rejected.");
            ExplorationKillApplicationResult replay = application.TryApplyBatch(batch);

            Assert(!replay.Success &&
                   replay.Error == ExplorationKillApplicationError.DuplicateExecution,
                "Repeated execution was not rejected.");
            AssertApproximately(runFlow.State.CurrentThreat, 10f,
                "Repeated execution changed Threat");
        }

        private static void ValidateRejectedExecutionCannotReplayInLaterRound()
        {
            RunFlowService runFlow = CreateRunFlowService();
            Assert(runFlow.TryRegisterExplorationKill(
                new ExplorationKillContribution(100f, 1)).Success,
                "Could not prepare portal phase for rejected-execution test.");
            Assert(runFlow.TryBeginAssaultTransition().Success,
                "Could not enter Assault transition for rejected-execution test.");

            ExplorationKillApplicationService application =
                new ExplorationKillApplicationService(runFlow);
            CombatActorReference player = CreatePlayer("player:phase-replay-test");
            ExplorationKillRecord[] batch =
            {
                CreateRecord(CombatExecutionId.New(), player, "enemy:wrong-phase", 10f, 1)
            };

            ExplorationKillApplicationResult rejected = application.TryApplyBatch(batch);
            AssertRejected(
                rejected,
                ExplorationKillApplicationError.RunFlowRejected,
                "Execution in non-exploration phase");
            Assert(rejected.RunFlowResult.Error == RunFlowError.InvalidPhase,
                "Wrong-phase rejection reason was lost.");

            Assert(runFlow.TryStartAssault().Success, "Could not start Assault.");
            Assert(runFlow.TryCompleteAssault().Success, "Could not complete Assault.");
            Assert(runFlow.TryBeginReturnToExploration().Success,
                "Could not begin exploration return.");
            Assert(runFlow.TryResumeExploration().Success,
                "Could not resume exploration.");

            ExplorationKillApplicationResult replay = application.TryApplyBatch(batch);
            AssertRejected(
                replay,
                ExplorationKillApplicationError.DuplicateExecution,
                "Rejected execution replay in a later round");
            AssertApproximately(runFlow.State.CurrentThreat, 0f,
                "Rejected execution replay changed next-round Threat");
        }

        private static void ValidateRejectedBatchesAreAtomic()
        {
            RunFlowService runFlow = CreateRunFlowService();
            ExplorationKillApplicationService application =
                new ExplorationKillApplicationService(runFlow);
            CombatActorReference player = CreatePlayer("player:rejection-test");
            CombatActorReference otherPlayer = CreatePlayer("player:other");
            CombatActorReference enemySource =
                new CombatActorReference("enemy:attacker", CombatActorKind.Enemy);
            CombatExecutionId executionId = CombatExecutionId.New();

            AssertRejected(
                application.TryApplyBatch(Array.Empty<ExplorationKillRecord>()),
                ExplorationKillApplicationError.EmptyBatch,
                "Empty batch");
            AssertRejected(
                application.TryApplyBatch(new[]
                {
                    new ExplorationKillRecord(
                        default,
                        CreateEnemy("enemy:invalid-context"),
                        new ExplorationKillContribution(10f, 1))
                }),
                ExplorationKillApplicationError.InvalidDeathContext,
                "Invalid death context");
            AssertRejected(
                application.TryApplyBatch(new[]
                {
                    CreateRecord(CombatExecutionId.New(), enemySource, "enemy:non-player", 10f, 1)
                }),
                ExplorationKillApplicationError.NonPlayerSource,
                "Non-player source");
            AssertRejected(
                application.TryApplyBatch(new[]
                {
                    CreateRecord(executionId, player, "enemy:mixed-execution-a", 10f, 1),
                    CreateRecord(CombatExecutionId.New(), player, "enemy:mixed-execution-b", 10f, 1)
                }),
                ExplorationKillApplicationError.MixedExecution,
                "Mixed execution");
            AssertRejected(
                application.TryApplyBatch(new[]
                {
                    CreateRecord(executionId, player, "enemy:mixed-source-a", 10f, 1),
                    CreateRecord(executionId, otherPlayer, "enemy:mixed-source-b", 10f, 1)
                }),
                ExplorationKillApplicationError.MixedSource,
                "Mixed source");
            AssertRejected(
                application.TryApplyBatch(new[]
                {
                    CreateRecord(executionId, player, "enemy:duplicate", 10f, 1),
                    CreateRecord(executionId, player, "enemy:duplicate", 10f, 1)
                }),
                ExplorationKillApplicationError.DuplicateDefeatedActor,
                "Duplicate defeated actor");

            ExplorationKillRecord invalidContribution = CreateRecord(
                executionId,
                player,
                "enemy:invalid-contribution",
                -1f,
                1);
            ExplorationKillApplicationResult invalidContributionResult =
                application.TryApplyBatch(new[] { invalidContribution });

            AssertRejected(
                invalidContributionResult,
                ExplorationKillApplicationError.RunFlowRejected,
                "Invalid contribution");
            Assert(invalidContributionResult.RunFlowResult.Error == RunFlowError.InvalidKillContribution,
                "Run-flow rejection reason was lost.");
            Assert(application.ProcessedExecutionCount == 1,
                "Attempted execution was not marked processed.");
            AssertApproximately(runFlow.State.CurrentThreat, 0f,
                "Rejected batches mutated Threat");

            ExplorationKillApplicationResult correctedRetry = application.TryApplyBatch(new[]
            {
                CreateRecord(executionId, player, "enemy:corrected", 10f, 1)
            });
            AssertRejected(
                correctedRetry,
                ExplorationKillApplicationError.DuplicateExecution,
                "Changed retry of an attempted execution");
        }

        private static void ValidateEnemyContributionAdapter()
        {
            GameObject enemyObject = new GameObject("ExplorationKill_ContributionAdapter");

            try
            {
                EnemyRunContributionSource source =
                    enemyObject.AddComponent<EnemyRunContributionSource>();
                SerializedObject serializedSource = new SerializedObject(source);
                serializedSource.FindProperty("threatAmount").floatValue = 25f;
                serializedSource.FindProperty("instabilityPoints").intValue = 3;
                serializedSource.ApplyModifiedPropertiesWithoutUndo();

                CombatExecutionId executionId = CombatExecutionId.New();
                DamageRequest damageRequest = new DamageRequest(
                    executionId,
                    CreatePlayer("player:adapter-test"),
                    100f,
                    DamageCause.Ability,
                    "ability:adapter-test");
                ExplorationKillRecord record = source.CreateKillRecord(
                    new DeathContext(damageRequest, 100f));

                Assert(record.ExecutionId == executionId,
                    "Enemy adapter changed the execution id.");
                Assert(record.DefeatedActor.IsValid && record.DefeatedActor.IsEnemy,
                    "Enemy adapter did not create a valid defeated actor reference.");
                AssertApproximately(record.Contribution.ThreatAmount, 25f,
                    "Enemy adapter Threat");
                Assert(record.Contribution.InstabilityPoints == 3,
                    "Enemy adapter instability points mismatch.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemyObject);
            }
        }

        private static RunFlowService CreateRunFlowService()
        {
            return new RunFlowService(RunFlowConfiguration.CreateVerticalSliceDefaults());
        }

        private static ExplorationKillRecord CreateRecord(
            CombatExecutionId executionId,
            CombatActorReference source,
            string defeatedActorId,
            float threatAmount,
            int instabilityPoints)
        {
            DamageRequest damageRequest = new DamageRequest(
                executionId,
                source,
                100f,
                DamageCause.BasicAttack);
            DeathContext deathContext = new DeathContext(damageRequest, 100f);

            return new ExplorationKillRecord(
                deathContext,
                CreateEnemy(defeatedActorId),
                new ExplorationKillContribution(threatAmount, instabilityPoints));
        }

        private static CombatActorReference CreatePlayer(string actorId)
        {
            return new CombatActorReference(actorId, CombatActorKind.Player);
        }

        private static CombatActorReference CreateEnemy(string actorId)
        {
            return new CombatActorReference(actorId, CombatActorKind.Enemy);
        }

        private static void AssertRejected(
            ExplorationKillApplicationResult result,
            ExplorationKillApplicationError expectedError,
            string label)
        {
            Assert(!result.Success && result.Error == expectedError,
                $"{label} returned {result.Error} instead of {expectedError}.");
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
