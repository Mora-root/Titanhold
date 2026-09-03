using System;
using TMPro;
using Titanhold.Run;
using Titanhold.UI.Hub;
using Titanhold.UI.Run;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Titanhold.Session.Editor
{
    public static class RunSessionTransitionWiringEditor
    {
        private const string HubScenePath =
            "Assets/_Project/Scenes/HubScene.unity";
        private const string RunScenePath =
            "Assets/_Project/Scenes/SampleScene.unity";
        private const string CompletionPrefabPath =
            "Assets/_Project/Prefabs/UI/RunCompletionUI.prefab";
        private const string RunEntryObjectName = "RunSessionEntryPoint";
        private const string HubEntryObjectName = "HubSessionEntryPoint";

        [MenuItem("Tools/Titanhold/Install Run Session Return Wiring")]
        public static void Install()
        {
            try
            {
                RequireEditMode("installation");
                RequireCleanOpenScene();
                ConfigureCompletionPrefab();

                Scene runScene = EditorSceneManager.OpenScene(
                    RunScenePath,
                    OpenSceneMode.Single);
                ConfigureRunScene(runScene);
                ValidateRunScene(runScene);

                Scene hubScene = EditorSceneManager.OpenScene(
                    HubScenePath,
                    OpenSceneMode.Single);
                ConfigureHubScene(hubScene);
                ValidateHubScene(hubScene);
                ValidateCompletionPrefab();

                AssetDatabase.SaveAssets();
                Debug.Log("Run Session return wiring installed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Run Session return wiring installation failed: {exception}");
            }
        }

        [MenuItem("Tools/Titanhold/Validate Run Session Return Wiring")]
        public static void Validate()
        {
            try
            {
                RequireEditMode("validation");
                RequireCleanOpenScene();
                ValidateCompletionPrefab();

                Scene runScene = EditorSceneManager.OpenScene(
                    RunScenePath,
                    OpenSceneMode.Single);
                ValidateRunScene(runScene);

                Scene hubScene = EditorSceneManager.OpenScene(
                    HubScenePath,
                    OpenSceneMode.Single);
                ValidateHubScene(hubScene);
                Debug.Log("Run Session return wiring validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Run Session return wiring validation failed: {exception}");
            }
        }

        private static void ConfigureCompletionPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(
                CompletionPrefabPath);
            if (root == null)
            {
                throw new InvalidOperationException(
                    $"Completion prefab not found: {CompletionPrefabPath}");
            }

            try
            {
                RunCompletionView view = root.GetComponent<RunCompletionView>();
                Transform completedCard = root.transform.Find(
                    "CompletedPanel/Card");
                Transform sourceButton = root.transform.Find(
                    "VictoryPanel/Card/CompleteRunButton");
                if (view == null || completedCard == null || sourceButton == null)
                {
                    throw new InvalidOperationException(
                        "Completion prefab structure is incomplete.");
                }

                RectTransform cardRect =
                    completedCard.GetComponent<RectTransform>();
                cardRect.sizeDelta = new Vector2(570f, 300f);
                SetAnchoredPosition(completedCard.Find("Title"), 0f, 70f);
                SetAnchoredPosition(completedCard.Find("Description"), 0f, -5f);

                Transform buttonTransform = completedCard.Find(
                    "ReturnToHubButton");
                if (buttonTransform == null)
                {
                    GameObject clone = UnityEngine.Object.Instantiate(
                        sourceButton.gameObject,
                        completedCard,
                        false);
                    clone.name = "ReturnToHubButton";
                    buttonTransform = clone.transform;
                }

                RectTransform buttonRect =
                    buttonTransform.GetComponent<RectTransform>();
                buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
                buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
                buttonRect.pivot = new Vector2(0.5f, 0.5f);
                buttonRect.anchoredPosition = new Vector2(0f, -95f);
                buttonRect.sizeDelta = new Vector2(250f, 58f);

                Button button = buttonTransform.GetComponent<Button>();
                TMP_Text label = buttonTransform.GetComponentInChildren<TMP_Text>(
                    true);
                if (button == null || label == null)
                {
                    throw new InvalidOperationException(
                        "Return-to-Hub button components are missing.");
                }

                label.text = "Вернуться в Hub";
                SerializedObject serializedView = new(view);
                serializedView.FindProperty("returnToHubButton")
                    .objectReferenceValue = button;
                serializedView.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(view);

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(
                    root,
                    CompletionPrefabPath);
                if (saved == null)
                {
                    throw new InvalidOperationException(
                        "Could not save the completion prefab.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureRunScene(Scene scene)
        {
            GameObject entryObject = FindRootObject(scene, RunEntryObjectName);
            RunSceneSessionEntryPoint entry = entryObject != null
                ? entryObject.GetComponent<RunSceneSessionEntryPoint>()
                : null;
            RunFlowRuntime runFlow =
                UnityEngine.Object.FindAnyObjectByType<RunFlowRuntime>(
                    FindObjectsInactive.Include);
            RunCompletionView view =
                UnityEngine.Object.FindAnyObjectByType<RunCompletionView>(
                    FindObjectsInactive.Include);
            if (entry == null || runFlow == null || view == null)
            {
                throw new InvalidOperationException(
                    "Run session entry, flow runtime, or completion view is missing.");
            }

            RunSessionExitController controller =
                entryObject.GetComponent<RunSessionExitController>();
            if (controller == null)
                controller = entryObject.AddComponent<RunSessionExitController>();

            controller.ConfigureForEditor(
                runFlow,
                view,
                entry,
                "HubScene");
            EditorUtility.SetDirty(controller);
            SaveScene(scene, RunScenePath);
        }

        private static void ConfigureHubScene(Scene scene)
        {
            HubRunPreparationView view =
                UnityEngine.Object.FindAnyObjectByType<HubRunPreparationView>(
                    FindObjectsInactive.Include);
            if (view == null)
                throw new InvalidOperationException("Hub preparation view is missing.");

            GameObject entryObject = FindRootObject(scene, HubEntryObjectName);
            if (entryObject == null)
                entryObject = new GameObject(HubEntryObjectName);

            HubSceneSessionEntryPoint entry =
                entryObject.GetComponent<HubSceneSessionEntryPoint>();
            if (entry == null)
                entry = entryObject.AddComponent<HubSceneSessionEntryPoint>();

            entry.ConfigureForEditor(view);
            EditorUtility.SetDirty(entry);
            SaveScene(scene, HubScenePath);
        }

        private static void ValidateCompletionPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                CompletionPrefabPath);
            RunCompletionView view = prefab != null
                ? prefab.GetComponent<RunCompletionView>()
                : null;
            Transform button = prefab != null
                ? prefab.transform.Find("CompletedPanel/Card/ReturnToHubButton")
                : null;
            if (view == null || button == null ||
                button.GetComponent<Button>() == null)
            {
                throw new InvalidOperationException(
                    "Completion prefab has no return-to-Hub button.");
            }

            SerializedObject serializedView = new(view);
            if (serializedView.FindProperty("returnToHubButton")
                    .objectReferenceValue != button.GetComponent<Button>())
            {
                throw new InvalidOperationException(
                    "Completion view return-to-Hub button is not wired.");
            }
        }

        private static void ValidateRunScene(Scene scene)
        {
            GameObject entryObject = FindRootObject(scene, RunEntryObjectName);
            RunSessionExitController controller = entryObject != null
                ? entryObject.GetComponent<RunSessionExitController>()
                : null;
            if (controller == null || !controller.HasRequiredReferences ||
                controller.RunFlowRuntime == null ||
                controller.CompletionView == null ||
                controller.SessionEntryPoint == null ||
                controller.HubSceneName != "HubScene")
            {
                throw new InvalidOperationException(
                    "Run session exit controller wiring is invalid.");
            }
        }

        private static void ValidateHubScene(Scene scene)
        {
            GameObject entryObject = FindRootObject(scene, HubEntryObjectName);
            HubSceneSessionEntryPoint entry = entryObject != null
                ? entryObject.GetComponent<HubSceneSessionEntryPoint>()
                : null;
            HubRunPreparationView view =
                UnityEngine.Object.FindAnyObjectByType<HubRunPreparationView>(
                    FindObjectsInactive.Include);
            if (entry == null || entry.transform.parent != null ||
                view == null || entry.View != view)
            {
                throw new InvalidOperationException(
                    "Hub session entry point wiring is invalid.");
            }
        }

        private static void SetAnchoredPosition(
            Transform target,
            float x,
            float y)
        {
            RectTransform rect = target != null
                ? target.GetComponent<RectTransform>()
                : null;
            if (rect == null)
                throw new InvalidOperationException("Completion text is missing.");

            rect.anchoredPosition = new Vector2(x, y);
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

        private static void SaveScene(Scene scene, string scenePath)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    $"Could not save scene '{scenePath}'.");
            }
        }

        private static void RequireEditMode(string operation)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    $"Exit Play Mode before Run Session return {operation}.");
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
