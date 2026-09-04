using System;
using TMPro;
using Titanhold.Session;
using Titanhold.UI.Run;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Titanhold.Run.Editor
{
    public static class RunDefeatWiringEditor
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/SampleScene.unity";
        private const string CompletionPrefabPath =
            "Assets/_Project/Prefabs/UI/RunCompletionUI.prefab";
        private const string SessionEntryObjectName = "RunSessionEntryPoint";

        [MenuItem("Tools/Titanhold/Install Run Defeat Wiring")]
        public static void Install()
        {
            try
            {
                RequireEditMode("installation");
                RequireCleanOpenScene();
                ConfigureCompletionPrefab();

                Scene scene = EditorSceneManager.OpenScene(
                    ScenePath,
                    OpenSceneMode.Single);
                ConfigureScene(scene);
                ValidatePrefab();
                ValidateScene(scene);
                AssetDatabase.SaveAssets();
                Debug.Log("Run Defeat wiring installed.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"Run Defeat wiring installation failed: {exception}");
            }
        }

        [MenuItem("Tools/Titanhold/Validate Run Defeat Wiring")]
        public static void Validate()
        {
            try
            {
                RequireEditMode("validation");
                ValidatePrefab();
                Scene scene = SceneManager.GetActiveScene();
                if (scene.path != ScenePath)
                    throw new InvalidOperationException($"Open '{ScenePath}'.");

                ValidateScene(scene);
                Debug.Log("Run Defeat wiring validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"Run Defeat wiring validation failed: {exception}");
            }
        }

        private static void ConfigureCompletionPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(
                CompletionPrefabPath);
            try
            {
                RunCompletionView view = root != null
                    ? root.GetComponent<RunCompletionView>()
                    : null;
                Transform completedPanel = root != null
                    ? root.transform.Find("CompletedPanel")
                    : null;
                if (view == null || completedPanel == null)
                {
                    throw new InvalidOperationException(
                        "Completion prefab or its completed panel is missing.");
                }

                Transform defeatPanel = root.transform.Find("DefeatPanel");
                if (defeatPanel == null)
                {
                    GameObject clone = UnityEngine.Object.Instantiate(
                        completedPanel.gameObject,
                        root.transform,
                        false);
                    clone.name = "DefeatPanel";
                    defeatPanel = clone.transform;
                }

                TMP_Text title = defeatPanel.Find("Card/Title")
                    ?.GetComponent<TMP_Text>();
                TMP_Text description = defeatPanel.Find("Card/Description")
                    ?.GetComponent<TMP_Text>();
                Button returnButton = defeatPanel.Find(
                    "Card/ReturnToHubButton")?.GetComponent<Button>();
                TMP_Text buttonLabel = returnButton != null
                    ? returnButton.GetComponentInChildren<TMP_Text>(true)
                    : null;
                if (title == null || description == null ||
                    returnButton == null || buttonLabel == null)
                {
                    throw new InvalidOperationException(
                        "Defeat panel structure is incomplete.");
                }

                title.text = "ПОРАЖЕНИЕ";
                description.text =
                    "Награда рассчитана по полностью завершённым раундам.";
                buttonLabel.text = "Вернуться в Hub";
                defeatPanel.gameObject.SetActive(false);

                SerializedObject serializedView = new(view);
                serializedView.FindProperty("defeatPanel")
                    .objectReferenceValue = defeatPanel.gameObject;
                serializedView.FindProperty("defeatReturnToHubButton")
                    .objectReferenceValue = returnButton;
                serializedView.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(view);

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(
                    root,
                    CompletionPrefabPath);
                if (saved == null)
                    throw new InvalidOperationException("Could not save completion prefab.");
            }
            finally
            {
                if (root != null)
                    PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureScene(Scene scene)
        {
            GameObject entryObject = FindRootObject(
                scene,
                SessionEntryObjectName);
            RunSceneSessionEntryPoint sessionEntry = entryObject != null
                ? entryObject.GetComponent<RunSceneSessionEntryPoint>()
                : null;
            RunFlowRuntime runFlow =
                UnityEngine.Object.FindAnyObjectByType<RunFlowRuntime>(
                    FindObjectsInactive.Include);
            if (sessionEntry == null || runFlow == null)
            {
                throw new InvalidOperationException(
                    "Run session entry point or flow runtime is missing.");
            }

            Health[] participants = new Health[sessionEntry.Participants.Count];
            for (int i = 0; i < participants.Length; i++)
            {
                RunSceneParticipantBinding binding =
                    sessionEntry.Participants[i];
                participants[i] = binding?.Inventory != null
                    ? binding.Inventory.GetComponent<Health>()
                    : null;
                if (participants[i] == null)
                {
                    throw new InvalidOperationException(
                        $"Participant binding {i} has no Health component.");
                }
            }

            RunParticipantDefeatController controller =
                entryObject.GetComponent<RunParticipantDefeatController>();
            if (controller == null)
            {
                controller = entryObject.AddComponent<
                    RunParticipantDefeatController>();
            }

            controller.ConfigureForEditor(runFlow, participants);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Could not save SampleScene.");
        }

        private static void ValidatePrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                CompletionPrefabPath);
            RunCompletionView view = prefab != null
                ? prefab.GetComponent<RunCompletionView>()
                : null;
            Transform defeatPanel = prefab != null
                ? prefab.transform.Find("DefeatPanel")
                : null;
            Button returnButton = defeatPanel != null
                ? defeatPanel.Find("Card/ReturnToHubButton")
                    ?.GetComponent<Button>()
                : null;
            if (view == null || defeatPanel == null || returnButton == null)
                throw new InvalidOperationException("Defeat prefab UI is missing.");

            SerializedObject serializedView = new(view);
            if (serializedView.FindProperty("defeatPanel")
                    .objectReferenceValue != defeatPanel.gameObject ||
                serializedView.FindProperty("defeatReturnToHubButton")
                    .objectReferenceValue != returnButton)
            {
                throw new InvalidOperationException(
                    "Defeat prefab UI references are invalid.");
            }
        }

        private static void ValidateScene(Scene scene)
        {
            GameObject entryObject = FindRootObject(
                scene,
                SessionEntryObjectName);
            RunParticipantDefeatController controller = entryObject != null
                ? entryObject.GetComponent<RunParticipantDefeatController>()
                : null;
            if (controller == null || !controller.HasRequiredReferences ||
                controller.RunFlowRuntime == null ||
                controller.ParticipantHealth.Count != 1)
            {
                throw new InvalidOperationException(
                    "Run participant defeat controller wiring is invalid.");
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

        private static void RequireEditMode(string operation)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    $"Exit Play Mode before Run Defeat {operation}.");
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
