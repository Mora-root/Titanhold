using System;
using Titanhold.Combat;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Run.Editor
{
    public static class RunChapterCombatProgressAdapterValidationRunner
    {
        [MenuItem("Tools/Titanhold/Validate Run Chapter Combat Progress Adapter")]
        public static void Validate()
        {
            GameObject adapterObject = new("ChapterCombatProgress_Adapter");
            GameObject firstEnemy = null;
            GameObject secondEnemy = null;

            try
            {
                RunChapterFlowService flow = new(
                    RunChapterFlowConfiguration.CreatePrototypeDefaults());
                RunChapterProgressApplicationService application = new(flow);
                RunChapterCombatProgressAdapter adapter =
                    adapterObject.AddComponent<RunChapterCombatProgressAdapter>();
                Assert(adapter.TryInitialize(application),
                    "Could not inject the chapter progress application.");

                firstEnemy = CreateEnemy(
                    "ChapterCombatProgress_FirstEnemy",
                    15f,
                    out RunChapterProgressValidationDamageable firstTarget);
                secondEnemy = CreateEnemy(
                    "ChapterCombatProgress_SecondEnemy",
                    25f,
                    out RunChapterProgressValidationDamageable secondTarget);

                CombatExecutionId executionId = CombatExecutionId.New();
                CombatActorReference player = new(
                    "actor:player-one",
                    CombatActorKind.Player);
                CombatExecutionReport report = new(
                    executionId,
                    new[]
                    {
                        CreateKilledResolution(firstTarget, executionId, player),
                        CreateKilledResolution(secondTarget, executionId, player)
                    });

                Assert(adapter.TryApplyReport(
                           "player:one",
                           player,
                           report,
                           10d,
                           out RunChapterProgressApplicationResult result),
                    "Valid combat report did not produce a progress command.");
                Assert(result.Success,
                    $"Valid combat progress was rejected: {result.Error}.");
                Assert(result.AcceptedContributionCount == 2,
                    "Multi-target execution did not remain one atomic batch.");
                AssertApproximately(
                    flow.State.CurrentProgress,
                    40f,
                    "Combat progress");

                Assert(adapter.TryApplyReport(
                           "player:one",
                           player,
                           report,
                           11d,
                           out RunChapterProgressApplicationResult replay),
                    "Replay was not forwarded to replay protection.");
                Assert(!replay.Success &&
                       replay.Error ==
                       RunChapterProgressApplicationError.DuplicateEvent,
                    "Repeated combat execution was not rejected.");
                AssertApproximately(
                    flow.State.CurrentProgress,
                    40f,
                    "Combat progress after replay");

                CombatActorReference otherPlayer = new(
                    "actor:player-two",
                    CombatActorKind.Player);
                Assert(!adapter.TryApplyReport(
                           "player:two",
                           otherPlayer,
                           report,
                           12d,
                           out _),
                    "A report attributed to another actor was accepted.");

                Debug.Log(
                    "Run Chapter Combat Progress Adapter validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Run Chapter Combat Progress Adapter validation failed: " +
                    $"{exception}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(adapterObject);
                if (firstEnemy != null)
                    UnityEngine.Object.DestroyImmediate(firstEnemy);
                if (secondEnemy != null)
                    UnityEngine.Object.DestroyImmediate(secondEnemy);
            }
        }

        private static GameObject CreateEnemy(
            string objectName,
            float progressAmount,
            out RunChapterProgressValidationDamageable damageable)
        {
            GameObject enemy = new(objectName);
            EnemyRunContributionSource source =
                enemy.AddComponent<EnemyRunContributionSource>();
            SerializedObject serializedSource = new(source);
            serializedSource.FindProperty("threatAmount").floatValue =
                progressAmount;
            serializedSource.ApplyModifiedPropertiesWithoutUndo();
            damageable =
                enemy.AddComponent<RunChapterProgressValidationDamageable>();
            return enemy;
        }

        private static DamageTargetResolution CreateKilledResolution(
            RunChapterProgressValidationDamageable target,
            CombatExecutionId executionId,
            CombatActorReference source)
        {
            DamageRequest request = new(
                executionId,
                source,
                rawDamage: 10f,
                DamageCause.BasicAttack);
            DeathContext death = new(request, appliedDamage: 10f);
            DamageResult result = DamageResult.Applied(
                request,
                healthBefore: 10f,
                healthAfter: 0f,
                appliedDamage: 10f,
                killed: true,
                death);
            return new DamageTargetResolution(target, result);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static void AssertApproximately(
            float actual,
            float expected,
            string label)
        {
            if (Math.Abs(actual - expected) <= 0.0001f)
                return;

            throw new InvalidOperationException(
                $"{label} failed. Expected {expected}, got {actual}.");
        }
    }

    public sealed class RunChapterProgressValidationDamageable :
        MonoBehaviour,
        IDamageable
    {
        public void TakeDamage(float damage)
        {
        }
    }
}
