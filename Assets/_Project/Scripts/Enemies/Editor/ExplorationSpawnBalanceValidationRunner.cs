using System;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Enemies.Editor
{
    public static class ExplorationSpawnBalanceValidationRunner
    {
        private const string EnemyCatalogPath =
            "Assets/_Project/ScriptableObjects/Enemies/EnemyDefinitionCatalog.asset";

        [MenuItem("Tools/Titanhold/Validate Exploration Spawn Balance")]
        public static void Validate()
        {
            ExplorationSpawnBalanceDefinition balance = null;
            try
            {
                EnemyDefinitionCatalog enemies =
                    AssetDatabase.LoadAssetAtPath<EnemyDefinitionCatalog>(
                        EnemyCatalogPath);
                Assert(enemies != null && enemies.IsValid,
                    "The active enemy catalog is missing or invalid.");

                balance = ScriptableObject.CreateInstance<
                    ExplorationSpawnBalanceDefinition>();
                balance.ConfigureForEditor(new[]
                {
                    Profile(
                        "spot:easy",
                        Stage(1, 12, 10f,
                            Enemy("enemy:skeleton", 3),
                            Enemy("enemy:skeleton-warrior", 1)),
                        Stage(4, 16, 8f,
                            Enemy("enemy:skeleton", 1),
                            Enemy("enemy:skeleton-warrior", 1)))
                });

                Assert(balance.TryCreateTable(
                        enemies,
                        out ExplorationSpawnBalanceTable table,
                        out string error),
                    error);
                Assert(table.TryResolve(
                        "spot:easy",
                        1,
                        out ExplorationSpawnStage roundOne),
                    "Round-one spawn stage was not resolved.");
                Assert(roundOne.MaximumAlive == 12 &&
                       Mathf.Approximately(roundOne.RespawnDelay, 10f),
                    "Round-one spawn values were not preserved.");
                Assert(roundOne.TrySelectEnemy(0, out string firstEnemy) &&
                       firstEnemy == "enemy:skeleton",
                    "The first weighted enemy interval is invalid.");
                Assert(roundOne.TrySelectEnemy(3, out string secondEnemy) &&
                       secondEnemy == "enemy:skeleton-warrior",
                    "The second weighted enemy interval is invalid.");
                Assert(roundOne.TrySelectEnemy(4, out string wrappedEnemy) &&
                       wrappedEnemy == "enemy:skeleton",
                    "Weighted selection does not wrap deterministically.");
                Assert(table.TryResolve(
                        "spot:easy",
                        3,
                        out ExplorationSpawnStage beforeMilestone) &&
                       ReferenceEquals(roundOne, beforeMilestone),
                    "A spawn stage did not persist until the next milestone.");
                Assert(table.TryResolve(
                        "spot:easy",
                        4,
                        out ExplorationSpawnStage roundFour) &&
                       roundFour.MaximumAlive == 16,
                    "The later spawn milestone was not selected.");
                Assert(!table.TryResolve("spot:missing", 1, out _),
                    "An unknown spawn profile unexpectedly resolved.");

                ValidateDuplicateEnemyRejection();
                ValidateDuplicateProfileRejection(roundOne);
                ValidateStrictIdRejection(roundOne);
                Debug.Log("Exploration spawn balance validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Exploration spawn balance validation failed: {exception}");
            }
            finally
            {
                if (balance != null)
                    UnityEngine.Object.DestroyImmediate(balance);
            }
        }

        private static void ValidateDuplicateEnemyRejection()
        {
            ExplorationEnemyWeight[] duplicateEnemies =
            {
                new("enemy:skeleton", 1),
                new("enemy:skeleton", 2)
            };
            Assert(!ExplorationSpawnStage.TryCreate(
                    1,
                    1,
                    0f,
                    duplicateEnemies,
                    out _,
                    out _),
                "A stage accepted duplicate enemy ids.");
        }

        private static void ValidateDuplicateProfileRejection(
            ExplorationSpawnStage stage)
        {
            Assert(ExplorationSpawnProfile.TryCreate(
                    "spot:duplicate",
                    new[] { stage },
                    out ExplorationSpawnProfile first,
                    out string firstError),
                firstError);
            Assert(ExplorationSpawnProfile.TryCreate(
                    "spot:duplicate",
                    new[] { stage },
                    out ExplorationSpawnProfile second,
                    out string secondError),
                secondError);
            Assert(!ExplorationSpawnBalanceTable.TryCreate(
                    new[] { first, second },
                    out _,
                    out _),
                "The spawn table accepted duplicate profile ids.");
        }

        private static void ValidateStrictIdRejection(
            ExplorationSpawnStage stage)
        {
            Assert(!ExplorationSpawnProfile.TryCreate(
                    " spot:invalid",
                    new[] { stage },
                    out _,
                    out _),
                "A spawn profile accepted surrounding id whitespace.");
            Assert(!ExplorationSpawnStage.TryCreate(
                    1,
                    1,
                    0f,
                    new[] { new ExplorationEnemyWeight("enemy:skeleton ", 1) },
                    out _,
                    out _),
                "A spawn stage accepted surrounding enemy-id whitespace.");
        }

        private static ExplorationSpawnProfileDefinition Profile(
            string profileId,
            params ExplorationSpawnStageDefinition[] stages)
        {
            ExplorationSpawnProfileDefinition profile = new();
            profile.ConfigureForEditor(profileId, stages);
            return profile;
        }

        private static ExplorationSpawnStageDefinition Stage(
            int startRound,
            int maximumAlive,
            float respawnDelay,
            params ExplorationEnemyWeightDefinition[] enemies)
        {
            ExplorationSpawnStageDefinition stage = new();
            stage.ConfigureForEditor(
                startRound,
                maximumAlive,
                respawnDelay,
                enemies);
            return stage;
        }

        private static ExplorationEnemyWeightDefinition Enemy(
            string enemyId,
            int weight)
        {
            ExplorationEnemyWeightDefinition enemy = new();
            enemy.ConfigureForEditor(enemyId, weight);
            return enemy;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
