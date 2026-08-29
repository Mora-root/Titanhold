using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Titanhold.Run.Editor
{
    public static class RunFlowVerticalSliceWiringEditor
    {
        private const string ScenePath = "Assets/_Project/Scenes/SampleScene.unity";
        private const string SkeletonPrefabPath = "Assets/_Project/Prefabs/Enemy/Skelet.prefab";
        private const string WarriorPrefabPath = "Assets/_Project/Prefabs/Enemy/Skelet_Warrior.prefab";
        private const string WavePrefabPath = "Assets/_Project/Prefabs/Enemy/Skelet_Wave.prefab";
        private const string RuntimeObjectName = "RunFlowRuntime";

        [MenuItem("Tools/Titanhold/Install Run Flow Vertical Slice Wiring")]
        public static void Install()
        {
            try
            {
                Scene scene = SceneManager.GetActiveScene();
                if (scene.path != ScenePath)
                {
                    throw new InvalidOperationException(
                        $"Open {ScenePath} before installing Run Flow wiring.");
                }

                if (scene.isDirty)
                {
                    throw new InvalidOperationException(
                        "The active scene has unrelated unsaved changes. Save or revert them first.");
                }

                ConfigureEnemyPrefab(SkeletonPrefabPath, 10f, 1);
                ConfigureEnemyPrefab(WarriorPrefabPath, 15f, 1);
                ConfigureRuntimeSceneObject(scene);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("Run Flow vertical-slice wiring installed.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"Run Flow vertical-slice wiring installation failed: {exception}");
            }
        }

        [MenuItem("Tools/Titanhold/Validate Run Flow Vertical Slice Wiring")]
        public static void Validate()
        {
            try
            {
                ValidateEnemyPrefab(SkeletonPrefabPath, 10f, 1);
                ValidateEnemyPrefab(WarriorPrefabPath, 15f, 1);
                ValidateWavePrefabRemainsExcluded();
                ValidateRuntimeSceneObject();
                Debug.Log("Run Flow vertical-slice wiring validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"Run Flow vertical-slice wiring validation failed: {exception}");
            }
        }

        private static void ConfigureEnemyPrefab(
            string prefabPath,
            float threatAmount,
            int instabilityPoints)
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

            try
            {
                EnemyRunContributionSource source =
                    prefabRoot.GetComponent<EnemyRunContributionSource>();
                if (source == null)
                    source = prefabRoot.AddComponent<EnemyRunContributionSource>();

                SerializedObject serializedSource = new SerializedObject(source);
                serializedSource.FindProperty("threatAmount").floatValue = threatAmount;
                serializedSource.FindProperty("instabilityPoints").intValue = instabilityPoints;
                serializedSource.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void ConfigureRuntimeSceneObject(Scene scene)
        {
            GameObject runtimeObject = FindRootObject(scene, RuntimeObjectName);
            if (runtimeObject == null)
            {
                runtimeObject = new GameObject(RuntimeObjectName);
                SceneManager.MoveGameObjectToScene(runtimeObject, scene);
                Undo.RegisterCreatedObjectUndo(runtimeObject, "Create Run Flow Runtime");
            }

            AddComponentIfMissing<RunFlowRuntime>(runtimeObject);
            AddComponentIfMissing<ExplorationCombatExecutionAdapter>(runtimeObject);
            AddComponentIfMissing<RunFlowDebugOverlay>(runtimeObject);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException($"Could not save {ScenePath}.");
        }

        private static void ValidateEnemyPrefab(
            string prefabPath,
            float expectedThreat,
            int expectedInstability)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                throw new InvalidOperationException($"Prefab not found: {prefabPath}");

            EnemyRunContributionSource source = prefab.GetComponent<EnemyRunContributionSource>();
            if (source == null)
                throw new InvalidOperationException($"Run contribution source missing on {prefabPath}");

            AssertApproximately(source.ThreatAmount, expectedThreat, $"{prefabPath} Threat");
            if (source.InstabilityPoints != expectedInstability)
            {
                throw new InvalidOperationException(
                    $"{prefabPath} expected {expectedInstability} instability points, " +
                    $"got {source.InstabilityPoints}.");
            }

            if (prefab.GetComponent<EnemyThreatSource>() == null)
            {
                throw new InvalidOperationException(
                    $"Legacy EnemyThreatSource was removed prematurely from {prefabPath}.");
            }
        }

        private static void ValidateWavePrefabRemainsExcluded()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WavePrefabPath);
            if (prefab == null)
                throw new InvalidOperationException($"Prefab not found: {WavePrefabPath}");

            if (prefab.GetComponent<EnemyRunContributionSource>() != null)
            {
                throw new InvalidOperationException(
                    "Legacy Assault wave enemy must not contribute exploration Threat or Instability.");
            }
        }

        private static void ValidateRuntimeSceneObject()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                throw new InvalidOperationException($"Open {ScenePath} before validation.");

            GameObject runtimeObject = FindRootObject(scene, RuntimeObjectName);
            if (runtimeObject == null)
                throw new InvalidOperationException("RunFlowRuntime scene object is missing.");

            if (runtimeObject.GetComponent<RunFlowRuntime>() == null ||
                runtimeObject.GetComponent<ExplorationCombatExecutionAdapter>() == null ||
                runtimeObject.GetComponent<RunFlowDebugOverlay>() == null)
            {
                throw new InvalidOperationException(
                    "RunFlowRuntime scene object does not contain all required components.");
            }
        }

        private static GameObject FindRootObject(Scene scene, string objectName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == objectName)
                    return roots[i];
            }

            return null;
        }

        private static void AddComponentIfMissing<T>(GameObject gameObject)
            where T : Component
        {
            if (gameObject.GetComponent<T>() == null)
                Undo.AddComponent<T>(gameObject);
        }

        private static void AssertApproximately(float actual, float expected, string label)
        {
            if (Math.Abs(actual - expected) <= 0.0001f)
                return;

            throw new InvalidOperationException(
                $"{label} failed. Expected {expected}, got {actual}.");
        }
    }
}
