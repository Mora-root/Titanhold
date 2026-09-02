using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Titanhold.Run.Editor
{
    public static class RoundEnemyScalingVerticalSliceWiringEditor
    {
        private const string ScenePath = "Assets/_Project/Scenes/SampleScene.unity";
        private const string RuntimeObjectName = "RunFlowRuntime";
        private const float HealthBonusPerRound = 0.20f;
        private const float DamageBonusPerRound = 0.10f;

        [MenuItem("Tools/Titanhold/Install Round Enemy Scaling Wiring")]
        public static void Install()
        {
            try
            {
                Scene scene = RequireActiveScene(requireClean: true);
                RunFlowRuntime runtime = RequireRuntime(scene);
                ConfigureRuntime(runtime);
                ConfigureSpawners(scene, runtime);

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException($"Could not save {ScenePath}.");

                Debug.Log("Round enemy scaling wiring installed.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"Round enemy scaling wiring installation failed: {exception}");
            }
        }

        [MenuItem("Tools/Titanhold/Validate Round Enemy Scaling Wiring")]
        public static void Validate()
        {
            try
            {
                Scene scene = RequireActiveScene(requireClean: false);
                RunFlowRuntime runtime = RequireRuntime(scene);
                ValidateRuntime(runtime);
                ValidateSpawners(scene, runtime);
                Debug.Log("Round enemy scaling wiring validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"Round enemy scaling wiring validation failed: {exception}");
            }
        }

        private static Scene RequireActiveScene(bool requireClean)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                throw new InvalidOperationException($"Open {ScenePath} first.");

            if (requireClean && scene.isDirty)
            {
                throw new InvalidOperationException(
                    "The active scene has unrelated unsaved changes. Save or revert them first.");
            }

            return scene;
        }

        private static RunFlowRuntime RequireRuntime(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name != RuntimeObjectName)
                    continue;

                RunFlowRuntime runtime = roots[i].GetComponent<RunFlowRuntime>();
                if (runtime != null)
                    return runtime;
            }

            throw new InvalidOperationException("RunFlowRuntime scene object is missing.");
        }

        private static void ConfigureRuntime(RunFlowRuntime runtime)
        {
            SerializedObject serializedRuntime = new SerializedObject(runtime);
            serializedRuntime.FindProperty("enemyHealthBonusPerRound").floatValue =
                HealthBonusPerRound;
            serializedRuntime.FindProperty("enemyDamageBonusPerRound").floatValue =
                DamageBonusPerRound;
            serializedRuntime.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureSpawners(Scene scene, RunFlowRuntime runtime)
        {
            WorldEnemySpawnZone[] zones =
                UnityEngine.Object.FindObjectsByType<WorldEnemySpawnZone>(
                    FindObjectsInactive.Include);
            WorldEnemyRespawnPoint[] points =
                UnityEngine.Object.FindObjectsByType<WorldEnemyRespawnPoint>(
                    FindObjectsInactive.Include);

            int configuredCount = 0;
            for (int i = 0; i < zones.Length; i++)
            {
                if (zones[i].gameObject.scene != scene)
                    continue;

                SetRuntimeReference(zones[i], runtime);
                configuredCount++;
            }

            for (int i = 0; i < points.Length; i++)
            {
                if (points[i].gameObject.scene != scene)
                    continue;

                SetRuntimeReference(points[i], runtime);
                configuredCount++;
            }

            if (configuredCount == 0)
                throw new InvalidOperationException("No exploration enemy spawners were found.");
        }

        private static void SetRuntimeReference(Component spawner, RunFlowRuntime runtime)
        {
            SerializedObject serializedSpawner = new SerializedObject(spawner);
            serializedSpawner.FindProperty("runFlowRuntime").objectReferenceValue = runtime;
            serializedSpawner.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ValidateRuntime(RunFlowRuntime runtime)
        {
            SerializedObject serializedRuntime = new SerializedObject(runtime);
            AssertApproximately(
                serializedRuntime.FindProperty("enemyHealthBonusPerRound").floatValue,
                HealthBonusPerRound,
                "Round health bonus");
            AssertApproximately(
                serializedRuntime.FindProperty("enemyDamageBonusPerRound").floatValue,
                DamageBonusPerRound,
                "Round damage bonus");
        }

        private static void ValidateSpawners(Scene scene, RunFlowRuntime runtime)
        {
            WorldEnemySpawnZone[] zones =
                UnityEngine.Object.FindObjectsByType<WorldEnemySpawnZone>(
                    FindObjectsInactive.Include);
            WorldEnemyRespawnPoint[] points =
                UnityEngine.Object.FindObjectsByType<WorldEnemyRespawnPoint>(
                    FindObjectsInactive.Include);

            int validatedCount = 0;
            for (int i = 0; i < zones.Length; i++)
            {
                if (zones[i].gameObject.scene != scene)
                    continue;

                if (zones[i].RunFlowRuntime != runtime)
                    throw new InvalidOperationException($"{zones[i].name} has stale Run Flow wiring.");

                validatedCount++;
            }

            for (int i = 0; i < points.Length; i++)
            {
                if (points[i].gameObject.scene != scene)
                    continue;

                if (points[i].RunFlowRuntime != runtime)
                    throw new InvalidOperationException($"{points[i].name} has stale Run Flow wiring.");

                validatedCount++;
            }

            if (validatedCount == 0)
                throw new InvalidOperationException("No exploration enemy spawners were found.");
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
