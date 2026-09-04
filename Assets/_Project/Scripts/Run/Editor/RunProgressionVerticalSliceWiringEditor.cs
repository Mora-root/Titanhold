using System;
using Titanhold.Session;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Titanhold.Run.Editor
{
    public static class RunProgressionVerticalSliceWiringEditor
    {
        private const string HubScenePath =
            "Assets/_Project/Scenes/HubScene.unity";
        private const string RunScenePath =
            "Assets/_Project/Scenes/SampleScene.unity";
        private const string DefinitionPath =
            "Assets/_Project/ScriptableObjects/Run/RunProgression_Prototype.asset";
        private const string AdapterObjectName = "RunProgressionRuntime";
        private static readonly string[] RewardPrefabPaths =
        {
            "Assets/_Project/Prefabs/Enemy/Skelet.prefab",
            "Assets/_Project/Prefabs/Enemy/Skelet_Warrior.prefab",
            "Assets/_Project/Prefabs/Enemy/Skelet_Assault.prefab",
            "Assets/_Project/Prefabs/Enemy/Skelet_Boss_Prototype.prefab"
        };

        [MenuItem("Tools/Titanhold/Install Run Progression Vertical Slice Wiring")]
        public static void Install()
        {
            try
            {
                RequireEditMode("installation");
                RequireCleanOpenScene();
                RunProgressionDefinition definition =
                    GetOrCreateDefinition();
                WireHub(definition);
                WireRunScene(definition);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                ValidateInternal(definition);
                Debug.Log("Run Progression vertical slice wiring installed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Run Progression vertical slice wiring installation failed: {exception}");
            }
        }

        [MenuItem("Tools/Titanhold/Validate Run Progression Vertical Slice Wiring")]
        public static void Validate()
        {
            try
            {
                RequireEditMode("validation");
                RunProgressionDefinition definition =
                    AssetDatabase.LoadAssetAtPath<RunProgressionDefinition>(
                        DefinitionPath);
                ValidateInternal(definition);
                Debug.Log("Run Progression vertical slice wiring validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Run Progression vertical slice wiring validation failed: {exception}");
            }
        }

        private static RunProgressionDefinition GetOrCreateDefinition()
        {
            RunProgressionDefinition definition =
                AssetDatabase.LoadAssetAtPath<RunProgressionDefinition>(
                    DefinitionPath);
            if (definition != null && definition.IsValid)
                return definition;

            if (definition != null)
            {
                throw new InvalidOperationException(
                    "Existing Run Progression Definition is invalid. Fix its values instead of overwriting it.");
            }

            definition =
                ScriptableObject.CreateInstance<RunProgressionDefinition>();
            definition.ConfigureForEditor(
                configuredMaximumLevel: 20,
                configuredBaseExperience: 100,
                configuredExperienceIncrease: 50);
            AssetDatabase.CreateAsset(definition, DefinitionPath);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static void WireHub(RunProgressionDefinition definition)
        {
            Scene scene = EditorSceneManager.OpenScene(
                HubScenePath,
                OpenSceneMode.Single);
            GameSessionRuntimeHost host =
                UnityEngine.Object.FindAnyObjectByType<GameSessionRuntimeHost>(
                    FindObjectsInactive.Include);
            if (host == null || host.ItemDefinitions == null)
            {
                throw new InvalidOperationException(
                    "Hub scene session host or item definition catalog is missing.");
            }

            host.ConfigureForEditor(host.ItemDefinitions, definition);
            EditorUtility.SetDirty(host);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Could not save Hub scene.");
        }

        private static void WireRunScene(RunProgressionDefinition definition)
        {
            Scene scene = EditorSceneManager.OpenScene(
                RunScenePath,
                OpenSceneMode.Single);
            RunSceneSessionEntryPoint entryPoint =
                UnityEngine.Object.FindAnyObjectByType<RunSceneSessionEntryPoint>(
                    FindObjectsInactive.Include);
            if (entryPoint == null)
            {
                throw new InvalidOperationException(
                    "Run scene session entry point is missing.");
            }

            GameObject adapterObject = FindRootObject(
                scene,
                AdapterObjectName);
            if (adapterObject == null)
                adapterObject = new GameObject(AdapterObjectName);

            RunProgressionCombatAdapter adapter =
                adapterObject.GetComponent<RunProgressionCombatAdapter>();
            if (adapter == null)
            {
                adapter = adapterObject.AddComponent<
                    RunProgressionCombatAdapter>();
            }

            adapter.ConfigureForEditor(entryPoint, definition);
            EditorUtility.SetDirty(adapter);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Could not save run scene.");
        }

        private static void ValidateInternal(
            RunProgressionDefinition definition)
        {
            if (definition == null || !definition.IsValid)
            {
                throw new InvalidOperationException(
                    "Run Progression Definition is missing or invalid.");
            }

            ValidateRewardPrefabs();

            EditorSceneManager.OpenScene(
                HubScenePath,
                OpenSceneMode.Single);
            GameSessionRuntimeHost host =
                UnityEngine.Object.FindAnyObjectByType<GameSessionRuntimeHost>(
                    FindObjectsInactive.Include);
            if (host == null || host.RunProgression != definition)
            {
                throw new InvalidOperationException(
                    "Hub session host is not wired to Run Progression Definition.");
            }

            Scene runScene = EditorSceneManager.OpenScene(
                RunScenePath,
                OpenSceneMode.Single);
            GameObject adapterObject = FindRootObject(
                runScene,
                AdapterObjectName);
            RunProgressionCombatAdapter adapter = adapterObject != null
                ? adapterObject.GetComponent<RunProgressionCombatAdapter>()
                : null;
            RunSceneSessionEntryPoint entryPoint =
                UnityEngine.Object.FindAnyObjectByType<RunSceneSessionEntryPoint>(
                    FindObjectsInactive.Include);
            if (adapter == null ||
                adapter.transform.parent != null ||
                adapter.ProgressionDefinition != definition ||
                adapter.SessionEntryPoint != entryPoint)
            {
                throw new InvalidOperationException(
                    "Run scene progression adapter wiring is invalid.");
            }

            if (entryPoint == null || entryPoint.Participants.Count == 0)
            {
                throw new InvalidOperationException(
                    "Run scene has no participant bindings.");
            }

            for (int i = 0; i < entryPoint.Participants.Count; i++)
            {
                RunSceneParticipantBinding binding =
                    entryPoint.Participants[i];
                if (binding == null || !binding.IsValid)
                {
                    throw new InvalidOperationException(
                        $"Run participant binding {i} is invalid.");
                }

                GameObject participant = binding.Inventory.gameObject;
                if (participant.GetComponent<PlayerCombat>() == null &&
                    participant.GetComponent<PlayerSkillExecutor>() == null)
                {
                    throw new InvalidOperationException(
                        $"Run participant '{binding.PlayerId}' has no combat execution source.");
                }
            }
        }

        private static void ValidateRewardPrefabs()
        {
            for (int i = 0; i < RewardPrefabPaths.Length; i++)
            {
                string path = RewardPrefabPaths[i];
                GameObject prefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(path);
                EnemyRewardSource reward = prefab != null
                    ? prefab.GetComponentInChildren<EnemyRewardSource>(true)
                    : null;
                if (reward == null || reward.RunExperienceAmount <= 0)
                {
                    throw new InvalidOperationException(
                        $"Enemy reward source is missing or invalid in '{path}'.");
                }
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

        private static void RequireEditMode(string operation)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    $"Exit Play Mode before Run Progression {operation}.");
            }
        }

        private static void RequireCleanOpenScene()
        {
            Scene current = SceneManager.GetActiveScene();
            if (current.IsValid() && current.isDirty)
            {
                throw new InvalidOperationException(
                    $"Save the currently open scene '{current.path}' first.");
            }
        }
    }
}
