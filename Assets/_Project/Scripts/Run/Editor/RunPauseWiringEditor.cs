using System;
using System.Collections.Generic;
using TMPro;
using Titanhold.Session;
using Titanhold.UI.Common;
using Titanhold.UI.Run;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Titanhold.Run.Editor
{
    public static class RunPauseWiringEditor
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/SampleScene.unity";
        private const string PrefabPath =
            "Assets/_Project/Prefabs/UI/RunPauseUI.prefab";
        private const string UiObjectName = "RunPauseUI";
        private const string RuntimeObjectName = "RunFlowRuntime";
        private const string SessionEntryObjectName = "RunSessionEntryPoint";

        private static readonly Color PanelColor =
            new(0.035f, 0.045f, 0.06f, 0.97f);
        private static readonly Color PrimaryColor =
            new(0.76f, 0.52f, 0.18f, 1f);
        private static readonly Color SecondaryColor =
            new(0.19f, 0.23f, 0.29f, 1f);

        [MenuItem("Tools/Titanhold/Install Run Pause Wiring")]
        public static void Install()
        {
            try
            {
                RequireEditMode("installation");
                Scene scene = RequireCleanSampleScene();
                CreateOrUpdatePrefab();
                ConfigureScene(scene);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                ValidateInternal();
                Debug.Log("Run Pause wiring installed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Run Pause wiring installation failed: {exception}");
            }
        }

        [MenuItem("Tools/Titanhold/Validate Run Pause Wiring")]
        public static void Validate()
        {
            try
            {
                RequireEditMode("validation");
                ValidateInternal();
                Debug.Log("Run Pause wiring validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Run Pause wiring validation failed: {exception}");
            }
        }

        private static void CreateOrUpdatePrefab()
        {
            GameObject root = CreateUiObject(UiObjectName, null);
            try
            {
                Stretch(root.GetComponent<RectTransform>());
                RunPauseView view = root.AddComponent<RunPauseView>();
                RunPauseController controller =
                    root.AddComponent<RunPauseController>();

                GameObject pausePanel = CreateModalPanel(
                    root.transform,
                    "PausePanel",
                    new Vector2(520f, 360f));
                Transform pauseCard = pausePanel.transform.Find("Card");
                CreateText(
                    pauseCard,
                    "Title",
                    "ПАУЗА",
                    38f,
                    FontStyles.Bold,
                    new Vector2(0f, 105f),
                    new Vector2(440f, 60f));
                Button resumeButton = CreateButton(
                    pauseCard,
                    "ResumeButton",
                    "Продолжить",
                    PrimaryColor,
                    new Vector2(0f, 22f),
                    new Vector2(270f, 58f));
                Button exitButton = CreateButton(
                    pauseCard,
                    "ExitButton",
                    "Покинуть забег",
                    SecondaryColor,
                    new Vector2(0f, -62f),
                    new Vector2(270f, 58f));

                GameObject confirmationPanel = CreateModalPanel(
                    root.transform,
                    "ExitConfirmationPanel",
                    new Vector2(620f, 320f));
                Transform confirmationCard =
                    confirmationPanel.transform.Find("Card");
                CreateText(
                    confirmationCard,
                    "Title",
                    "Покинуть забег?",
                    34f,
                    FontStyles.Bold,
                    new Vector2(0f, 88f),
                    new Vector2(520f, 58f));
                CreateText(
                    confirmationCard,
                    "Description",
                    "Будут засчитаны только полностью завершённые раунды.",
                    21f,
                    FontStyles.Normal,
                    new Vector2(0f, 22f),
                    new Vector2(510f, 72f));
                Button cancelButton = CreateButton(
                    confirmationCard,
                    "CancelButton",
                    "Отмена",
                    SecondaryColor,
                    new Vector2(-145f, -88f),
                    new Vector2(250f, 58f));
                Button confirmButton = CreateButton(
                    confirmationCard,
                    "ConfirmButton",
                    "Покинуть",
                    PrimaryColor,
                    new Vector2(145f, -88f),
                    new Vector2(250f, 58f));

                ConfigureView(
                    view,
                    pausePanel,
                    confirmationPanel,
                    resumeButton,
                    exitButton,
                    cancelButton,
                    confirmButton);
                SerializedObject serializedController = new(controller);
                serializedController.FindProperty("view")
                    .objectReferenceValue = view;
                serializedController.ApplyModifiedPropertiesWithoutUndo();

                pausePanel.SetActive(false);
                confirmationPanel.SetActive(false);

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(
                    root,
                    PrefabPath);
                if (saved == null)
                {
                    throw new InvalidOperationException(
                        $"Could not save {PrefabPath}.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void ConfigureScene(Scene scene)
        {
            RunFlowRuntime runtime = FindRootObject(
                    scene,
                    RuntimeObjectName)
                ?.GetComponent<RunFlowRuntime>();
            GameObject sessionEntryObject = FindRootObject(
                scene,
                SessionEntryObjectName);
            RunSceneSessionEntryPoint sessionEntry = sessionEntryObject != null
                ? sessionEntryObject.GetComponent<RunSceneSessionEntryPoint>()
                : null;
            RunSessionExitController sessionExit = sessionEntryObject != null
                ? sessionEntryObject.GetComponent<RunSessionExitController>()
                : null;
            RunCompletionView completionView =
                UnityEngine.Object.FindAnyObjectByType<RunCompletionView>(
                    FindObjectsInactive.Include);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PrefabPath);
            if (runtime == null || sessionEntry == null || sessionExit == null ||
                completionView == null || prefab == null)
            {
                throw new InvalidOperationException(
                    "Run runtime, session entry/exit, completion UI, or Pause prefab is missing.");
            }

            Transform uiParent = completionView.transform.parent;
            Transform existing = uiParent.Find(UiObjectName);
            GameObject instance = existing != null
                ? existing.gameObject
                : PrefabUtility.InstantiatePrefab(
                    prefab,
                    uiParent) as GameObject;
            if (instance == null)
                throw new InvalidOperationException("Could not instantiate Run Pause UI.");

            if (existing == null)
            {
                Undo.RegisterCreatedObjectUndo(instance, "Create Run Pause UI");
                instance.name = UiObjectName;
            }

            instance.transform.SetAsLastSibling();
            RunPauseView view = instance.GetComponent<RunPauseView>();
            RunPauseController controller =
                instance.GetComponent<RunPauseController>();
            if (view == null || controller == null)
                throw new InvalidOperationException("Run Pause UI components are missing.");

            PlayerInput[] inputs = ResolveParticipantInputs(sessionEntry);
            MonoBehaviour[] priorityWindows = ResolveEscapePriorityWindows(
                instance);
            controller.ConfigureForEditor(
                runtime,
                view,
                sessionExit,
                inputs,
                priorityWindows,
                true);
            EditorUtility.SetDirty(controller);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException($"Could not save {ScenePath}.");
        }

        private static PlayerInput[] ResolveParticipantInputs(
            RunSceneSessionEntryPoint sessionEntry)
        {
            PlayerInput[] inputs =
                new PlayerInput[sessionEntry.Participants.Count];
            for (int i = 0; i < inputs.Length; i++)
            {
                RunSceneParticipantBinding binding =
                    sessionEntry.Participants[i];
                inputs[i] = binding?.Inventory != null
                    ? binding.Inventory.GetComponent<PlayerInput>()
                    : null;
                if (inputs[i] == null)
                {
                    throw new InvalidOperationException(
                        $"Run participant {i} has no PlayerInput component.");
                }
            }

            return inputs;
        }

        private static MonoBehaviour[] ResolveEscapePriorityWindows(
            GameObject pauseUi)
        {
            MonoBehaviour[] behaviours =
                UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                    FindObjectsInactive.Include);
            List<MonoBehaviour> windows = new();
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null ||
                    behaviour.transform.IsChildOf(pauseUi.transform) ||
                    behaviour is not IEscapePriorityWindow)
                {
                    continue;
                }

                windows.Add(behaviour);
            }

            return windows.ToArray();
        }

        private static void ValidateInternal()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                throw new InvalidOperationException($"Open {ScenePath} before validation.");

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PrefabPath);
            if (prefab == null)
                throw new InvalidOperationException($"Prefab not found: {PrefabPath}.");

            ValidatePrefab(prefab);

            RunCompletionView completionView =
                UnityEngine.Object.FindAnyObjectByType<RunCompletionView>(
                    FindObjectsInactive.Include);
            Transform instanceTransform = completionView != null
                ? completionView.transform.parent.Find(UiObjectName)
                : null;
            RunPauseController controller = instanceTransform != null
                ? instanceTransform.GetComponent<RunPauseController>()
                : null;
            if (controller == null || !controller.HasRequiredReferences ||
                !controller.PauseWorld)
            {
                throw new InvalidOperationException(
                    "Run Pause scene wiring is missing or invalid.");
            }
        }

        private static void ValidatePrefab(GameObject prefab)
        {
            RunPauseView view = prefab.GetComponent<RunPauseView>();
            RunPauseController controller =
                prefab.GetComponent<RunPauseController>();
            if (view == null || controller == null)
                throw new InvalidOperationException("Run Pause prefab components are missing.");

            SerializedObject serializedView = new(view);
            string[] references =
            {
                "pausePanel",
                "exitConfirmationPanel",
                "resumeButton",
                "exitButton",
                "cancelExitButton",
                "confirmExitButton"
            };
            for (int i = 0; i < references.Length; i++)
            {
                if (serializedView.FindProperty(references[i])
                        .objectReferenceValue == null)
                {
                    throw new InvalidOperationException(
                        $"Run Pause prefab has no {references[i]} reference.");
                }
            }

            SerializedObject serializedController = new(controller);
            if (serializedController.FindProperty("view")
                    .objectReferenceValue != view)
            {
                throw new InvalidOperationException(
                    "Run Pause prefab controller view is not wired.");
            }
        }

        private static void ConfigureView(
            RunPauseView view,
            GameObject pausePanel,
            GameObject confirmationPanel,
            Button resumeButton,
            Button exitButton,
            Button cancelButton,
            Button confirmButton)
        {
            SerializedObject serializedView = new(view);
            serializedView.FindProperty("pausePanel")
                .objectReferenceValue = pausePanel;
            serializedView.FindProperty("exitConfirmationPanel")
                .objectReferenceValue = confirmationPanel;
            serializedView.FindProperty("resumeButton")
                .objectReferenceValue = resumeButton;
            serializedView.FindProperty("exitButton")
                .objectReferenceValue = exitButton;
            serializedView.FindProperty("cancelExitButton")
                .objectReferenceValue = cancelButton;
            serializedView.FindProperty("confirmExitButton")
                .objectReferenceValue = confirmButton;
            serializedView.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateModalPanel(
            Transform parent,
            string name,
            Vector2 cardSize)
        {
            GameObject overlay = CreateUiObject(name, parent);
            Stretch(overlay.GetComponent<RectTransform>());
            Image dim = overlay.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.58f);
            dim.raycastTarget = true;

            GameObject card = CreateUiObject("Card", overlay.transform);
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.sizeDelta = cardSize;
            Image cardImage = card.AddComponent<Image>();
            cardImage.color = PanelColor;
            cardImage.raycastTarget = true;
            return overlay;
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            string label,
            Color color,
            Vector2 position,
            Vector2 size)
        {
            GameObject buttonObject = CreateUiObject(name, parent);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = buttonObject.AddComponent<Image>();
            image.color = color;
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            TMP_Text text = CreateText(
                buttonObject.transform,
                "Label",
                label,
                22f,
                FontStyles.Bold,
                Vector2.zero,
                size - new Vector2(20f, 8f));
            text.raycastTarget = false;
            return button;
        }

        private static TMP_Text CreateText(
            Transform parent,
            string name,
            string value,
            float fontSize,
            FontStyles fontStyle,
            Vector2 position,
            Vector2 size)
        {
            GameObject textObject = CreateUiObject(name, parent);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject CreateUiObject(
            string name,
            Transform parent)
        {
            GameObject result = new(name, typeof(RectTransform));
            result.layer = LayerMask.NameToLayer("UI");
            result.transform.SetParent(parent, false);
            return result;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        private static void RequireEditMode(string operation)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    $"Run Pause {operation} is available only in Edit Mode.");
            }
        }

        private static Scene RequireCleanSampleScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                throw new InvalidOperationException($"Open {ScenePath} before installation.");

            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "The active scene has unrelated unsaved changes. Save or revert them first.");
            }

            return scene;
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
    }
}
