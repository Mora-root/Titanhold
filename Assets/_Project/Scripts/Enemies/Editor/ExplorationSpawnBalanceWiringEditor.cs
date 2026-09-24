using System;
using System.Collections.Generic;
using Titanhold.Run;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Titanhold.Enemies.Editor
{
    public static class ExplorationSpawnBalanceWiringEditor
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/SampleScene.unity";
        private const string BalancePath =
            "Assets/_Project/ScriptableObjects/Enemies/ExplorationSpawnBalance_Prototype.asset";
        private const string EnemyCatalogPath =
            "Assets/_Project/ScriptableObjects/Enemies/EnemyDefinitionCatalog.asset";
        private const string PrototypeProfileId = "spot:prototype";
        private const string SkeletonId = "enemy:skeleton";

        [MenuItem("Tools/Titanhold/Install Exploration Spawn Balance Wiring")]
        public static void Install()
        {
            try
            {
                EnsureCurrentSceneCanBeRestored();
                EnemyDefinitionCatalog enemies = RequireEnemyCatalog();
                ExplorationSpawnBalanceDefinition balance =
                    CreateOrUpdateBalance(enemies);
                ConfigureScene(balance);
                AssetDatabase.SaveAssets();
                Debug.Log("Exploration spawn balance wiring installed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Exploration spawn balance wiring failed: {exception}");
            }
        }

        [MenuItem("Tools/Titanhold/Validate Exploration Spawn Balance Wiring")]
        public static void Validate()
        {
            try
            {
                EnsureCurrentSceneCanBeRestored();
                EnemyDefinitionCatalog enemies = RequireEnemyCatalog();
                ExplorationSpawnBalanceDefinition balance =
                    AssetDatabase.LoadAssetAtPath<
                        ExplorationSpawnBalanceDefinition>(BalancePath);
                Assert(balance != null, "The exploration spawn balance asset is missing.");
                Assert(balance.TryCreateTable(
                        enemies,
                        out ExplorationSpawnBalanceTable table,
                        out string balanceError),
                    balanceError);
                Assert(table.TryResolve(
                        PrototypeProfileId,
                        1,
                        out ExplorationSpawnStage stage),
                    "The prototype profile does not resolve for round one.");
                Assert(stage.MaximumAlive == 12 &&
                       Mathf.Approximately(stage.RespawnDelay, 10f),
                    "The prototype profile does not preserve current spawn pacing.");
                Assert(stage.Enemies.Count == 1 &&
                       stage.Enemies[0].EnemyId == SkeletonId,
                    "The prototype profile does not preserve the current enemy type.");

                ValidateScene(balance);
                Debug.Log("Exploration spawn balance wiring validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Exploration spawn balance wiring validation failed: {exception}");
            }
        }

        private static ExplorationSpawnBalanceDefinition CreateOrUpdateBalance(
            EnemyDefinitionCatalog enemies)
        {
            Assert(enemies.TryResolve(SkeletonId, out _),
                $"Enemy catalog does not contain '{SkeletonId}'.");

            ExplorationSpawnBalanceDefinition balance =
                AssetDatabase.LoadAssetAtPath<
                    ExplorationSpawnBalanceDefinition>(BalancePath);
            if (balance == null)
            {
                balance = ScriptableObject.CreateInstance<
                    ExplorationSpawnBalanceDefinition>();
                AssetDatabase.CreateAsset(balance, BalancePath);
            }

            ExplorationEnemyWeightDefinition skeleton = new();
            skeleton.ConfigureForEditor(SkeletonId, 1);
            ExplorationSpawnStageDefinition stage = new();
            stage.ConfigureForEditor(
                configuredStartRound: 1,
                configuredMaximumAlive: 12,
                configuredRespawnDelay: 10f,
                configuredEnemies: new[] { skeleton });
            ExplorationSpawnProfileDefinition profile = new();
            profile.ConfigureForEditor(
                PrototypeProfileId,
                new[] { stage });
            balance.ConfigureForEditor(new[] { profile });
            EditorUtility.SetDirty(balance);

            Assert(balance.TryCreateTable(
                    enemies,
                    out _,
                    out string error),
                error);
            return balance;
        }

        private static void ConfigureScene(
            ExplorationSpawnBalanceDefinition balance)
        {
            string originalScenePath = SceneManager.GetActiveScene().path;
            try
            {
                Scene scene = EditorSceneManager.OpenScene(
                    ScenePath,
                    OpenSceneMode.Single);
                RunFlowRuntime[] runtimes = GetSceneComponents<RunFlowRuntime>(scene);
                WorldEnemySpawnZone[] zones =
                    GetSceneComponents<WorldEnemySpawnZone>(scene);
                Assert(runtimes.Length == 1,
                    "SampleScene must contain exactly one RunFlowRuntime.");
                Assert(zones.Length > 0,
                    "SampleScene has no WorldEnemySpawnZone.");

                SerializedObject runtime = new(runtimes[0]);
                SerializedProperty balanceProperty = runtime.FindProperty(
                    "explorationSpawnBalanceDefinition");
                Assert(balanceProperty != null,
                    "RunFlowRuntime spawn-balance field is missing.");
                balanceProperty.objectReferenceValue = balance;
                runtime.ApplyModifiedPropertiesWithoutUndo();

                for (int i = 0; i < zones.Length; i++)
                {
                    SerializedObject zone = new(zones[i]);
                    SerializedProperty profileProperty = zone.FindProperty(
                        "spawnProfileId");
                    Assert(profileProperty != null,
                        "WorldEnemySpawnZone profile field is missing.");
                    profileProperty.stringValue = PrototypeProfileId;
                    zone.ApplyModifiedPropertiesWithoutUndo();
                }

                EditorSceneManager.MarkSceneDirty(scene);
                Assert(EditorSceneManager.SaveScene(scene),
                    "SampleScene could not be saved.");
            }
            finally
            {
                RestoreScene(originalScenePath);
            }
        }

        private static void ValidateScene(
            ExplorationSpawnBalanceDefinition balance)
        {
            string originalScenePath = SceneManager.GetActiveScene().path;
            try
            {
                Scene scene = EditorSceneManager.OpenScene(
                    ScenePath,
                    OpenSceneMode.Single);
                RunFlowRuntime[] runtimes = GetSceneComponents<RunFlowRuntime>(scene);
                WorldEnemySpawnZone[] zones =
                    GetSceneComponents<WorldEnemySpawnZone>(scene);
                Assert(runtimes.Length == 1,
                    "SampleScene must contain exactly one RunFlowRuntime.");
                Assert(zones.Length > 0,
                    "SampleScene has no WorldEnemySpawnZone.");

                SerializedObject runtime = new(runtimes[0]);
                SerializedProperty balanceProperty = runtime.FindProperty(
                    "explorationSpawnBalanceDefinition");
                Assert(balanceProperty != null &&
                       balanceProperty.objectReferenceValue == balance,
                    "RunFlowRuntime does not reference the exploration spawn balance.");

                for (int i = 0; i < zones.Length; i++)
                {
                    SerializedObject zone = new(zones[i]);
                    SerializedProperty profileProperty = zone.FindProperty(
                        "spawnProfileId");
                    Assert(profileProperty != null &&
                           profileProperty.stringValue == PrototypeProfileId,
                        $"Spawn zone '{zones[i].name}' has an invalid profile id.");
                }
            }
            finally
            {
                RestoreScene(originalScenePath);
            }
        }

        private static EnemyDefinitionCatalog RequireEnemyCatalog()
        {
            EnemyDefinitionCatalog catalog =
                AssetDatabase.LoadAssetAtPath<EnemyDefinitionCatalog>(
                    EnemyCatalogPath);
            Assert(catalog != null && catalog.IsValid,
                catalog != null
                    ? catalog.ValidationError
                    : "Enemy definition catalog is missing.");
            return catalog;
        }

        private static T[] GetSceneComponents<T>(Scene scene)
            where T : Component
        {
            List<T> results = new();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
                results.AddRange(roots[i].GetComponentsInChildren<T>(true));
            return results.ToArray();
        }

        private static void EnsureCurrentSceneCanBeRestored()
        {
            Scene scene = SceneManager.GetActiveScene();
            Assert(scene.IsValid() && !string.IsNullOrEmpty(scene.path),
                "Open a saved scene before installing spawn balance wiring.");
            Assert(!scene.isDirty,
                "Save the active scene before installing spawn balance wiring.");
        }

        private static void RestoreScene(string scenePath)
        {
            if (!string.IsNullOrEmpty(scenePath) &&
                SceneManager.GetActiveScene().path != scenePath)
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
