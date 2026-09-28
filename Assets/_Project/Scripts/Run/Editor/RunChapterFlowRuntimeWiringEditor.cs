using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Titanhold.Run.Editor
{
    public static class RunChapterFlowRuntimeWiringEditor
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/SampleScene.unity";
        private const string DefinitionPath =
            "Assets/_Project/ScriptableObjects/Run/RunChapterFlow_Prototype.asset";
        private const string RuntimeObjectName = "RunFlowRuntime";

        private static readonly float[] EscalationThresholds =
        {
            0f,
            0.30f,
            0.60f,
            0.85f
        };

        [MenuItem("Tools/Titanhold/Install Run Chapter Flow Runtime Wiring")]
        public static void Install()
        {
            try
            {
                RequireEditMode("installation");
                Scene scene = RequireCleanOpenScene();
                RunChapterFlowDefinition definition =
                    CreateOrUpdateDefinition();
                ConfigureRuntime(scene, definition);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                ValidateInternal(scene, definition);
                Debug.Log("Run Chapter Flow Runtime wiring installed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Run Chapter Flow Runtime wiring installation failed: " +
                    $"{exception}");
            }
        }

        [MenuItem("Tools/Titanhold/Validate Run Chapter Flow Runtime Wiring")]
        public static void Validate()
        {
            try
            {
                RequireEditMode("validation");
                Scene scene = EditorSceneManager.GetActiveScene();
                if (scene.path != ScenePath)
                {
                    throw new InvalidOperationException(
                        $"Open {ScenePath} before validation.");
                }

                RunChapterFlowDefinition definition =
                    AssetDatabase.LoadAssetAtPath<RunChapterFlowDefinition>(
                        DefinitionPath);
                ValidateInternal(scene, definition);
                Debug.Log("Run Chapter Flow Runtime wiring validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Run Chapter Flow Runtime wiring validation failed: " +
                    $"{exception}");
            }
        }

        private static RunChapterFlowDefinition CreateOrUpdateDefinition()
        {
            RunChapterFlowDefinition definition =
                AssetDatabase.LoadAssetAtPath<RunChapterFlowDefinition>(
                    DefinitionPath);
            if (definition == null)
            {
                definition =
                    ScriptableObject.CreateInstance<RunChapterFlowDefinition>();
                AssetDatabase.CreateAsset(definition, DefinitionPath);
            }

            definition.ConfigureForEditor(
                configuredChapterNumber: 1,
                configuredMaximumProgress: 100f,
                configuredNormalizedEscalationThresholds:
                    EscalationThresholds,
                configuredCollapseDurationSeconds: 120f,
                configuredInstabilityStackIntervalSeconds: 20f,
                configuredBossHealthBonusPerStack: 0.10f,
                configuredBossDamageBonusPerStack: 0.05f);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static void ConfigureRuntime(
            Scene scene,
            RunChapterFlowDefinition definition)
        {
            GameObject runtimeObject =
                FindRootObject(scene, RuntimeObjectName);
            if (runtimeObject == null)
            {
                throw new InvalidOperationException(
                    $"Scene object '{RuntimeObjectName}' is missing.");
            }

            RunChapterFlowRuntime runtime =
                runtimeObject.GetComponent<RunChapterFlowRuntime>();
            if (runtime == null)
            {
                runtime = Undo.AddComponent<RunChapterFlowRuntime>(
                    runtimeObject);
            }

            SerializedObject serializedRuntime = new SerializedObject(runtime);
            serializedRuntime.FindProperty("definition").objectReferenceValue =
                definition;
            serializedRuntime.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(runtime);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException($"Could not save {ScenePath}.");
        }

        private static void ValidateInternal(
            Scene scene,
            RunChapterFlowDefinition definition)
        {
            ValidateDefinition(definition);

            GameObject runtimeObject =
                FindRootObject(scene, RuntimeObjectName);
            if (runtimeObject == null)
            {
                throw new InvalidOperationException(
                    $"Scene object '{RuntimeObjectName}' is missing.");
            }

            if (runtimeObject.GetComponent<RunFlowRuntime>() == null)
            {
                throw new InvalidOperationException(
                    "Legacy RunFlowRuntime was removed prematurely.");
            }

            RunChapterFlowRuntime runtime =
                runtimeObject.GetComponent<RunChapterFlowRuntime>();
            if (runtime == null || runtime.Definition != definition)
            {
                throw new InvalidOperationException(
                    "SampleScene does not use the authored chapter flow definition.");
            }
        }

        private static void ValidateDefinition(
            RunChapterFlowDefinition definition)
        {
            if (definition == null)
            {
                throw new InvalidOperationException(
                    "Run chapter flow definition is missing.");
            }

            if (!definition.TryCreateConfiguration(
                    out RunChapterFlowConfiguration configuration,
                    out string error))
                throw new InvalidOperationException(error);

            Assert(configuration.ChapterNumber == 1,
                "Prototype chapter number is incorrect.");
            AssertApproximately(
                configuration.MaximumProgress,
                100f,
                "Maximum progress");
            Assert(configuration.EscalationThresholds.Count == 4,
                "Prototype escalation stage count is incorrect.");
            for (int i = 0; i < EscalationThresholds.Length; i++)
            {
                AssertApproximately(
                    configuration.EscalationThresholds[i],
                    EscalationThresholds[i],
                    $"Escalation threshold {i}");
            }

            AssertApproximately(
                (float)configuration.CollapseDurationSeconds,
                120f,
                "Collapse duration");
            AssertApproximately(
                (float)configuration.InstabilityStackIntervalSeconds,
                20f,
                "Instability interval");
            AssertApproximately(
                configuration.BossHealthBonusPerStack,
                0.10f,
                "Boss health per stack");
            AssertApproximately(
                configuration.BossDamageBonusPerStack,
                0.05f,
                "Boss damage per stack");
        }

        private static Scene RequireCleanOpenScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    $"Open {ScenePath} before installation.");
            }

            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "SampleScene has unrelated unsaved changes. Save or revert them first.");
            }

            return scene;
        }

        private static void RequireEditMode(string operation)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    $"Exit Play Mode before {operation}.");
            }
        }

        private static GameObject FindRootObject(
            Scene scene,
            string objectName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == objectName)
                    return roots[i];
            }

            return null;
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
}
