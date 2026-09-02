using System;
using TMPro;
using Titanhold.UI.Run;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Titanhold.Run.Editor
{
    public static class RunCompletionVerticalSliceWiringEditor
    {
        private const string ScenePath = "Assets/_Project/Scenes/SampleScene.unity";
        private const string RuntimeObjectName = "RunFlowRuntime";
        private const string UiObjectName = "RunCompletionUI";
        private const string PrefabPath =
            "Assets/_Project/Prefabs/UI/RunCompletionUI.prefab";

        private static readonly Color PanelColor = new(0.035f, 0.045f, 0.06f, 0.97f);
        private static readonly Color PrimaryColor = new(0.76f, 0.52f, 0.18f, 1f);
        private static readonly Color SecondaryColor = new(0.19f, 0.23f, 0.29f, 1f);

        [MenuItem("Tools/Titanhold/Install Run Completion UI Wiring")]
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
                Debug.Log("Run Completion UI wiring installed.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"Run Completion UI wiring installation failed: {exception}");
            }
        }

        [MenuItem("Tools/Titanhold/Validate Run Completion UI Wiring")]
        public static void Validate()
        {
            try
            {
                RequireEditMode("validation");
                ValidateInternal();
                Debug.Log("Run Completion UI wiring validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"Run Completion UI wiring validation failed: {exception}");
            }
        }

        private static void CreateOrUpdatePrefab()
        {
            GameObject root = CreateUiObject(UiObjectName, null);

            try
            {
                Stretch(root.GetComponent<RectTransform>());
                RunCompletionView view = root.AddComponent<RunCompletionView>();
                RunCompletionController controller =
                    root.AddComponent<RunCompletionController>();

                GameObject victoryPanel = CreateModalPanel(
                    root.transform,
                    "VictoryPanel",
                    new Vector2(620f, 330f));
                Transform victoryCard = victoryPanel.transform.Find("Card");
                CreateText(
                    victoryCard,
                    "Title",
                    "ПОБЕДА",
                    38f,
                    FontStyles.Bold,
                    new Vector2(0f, 94f),
                    new Vector2(520f, 60f));
                CreateText(
                    victoryCard,
                    "Description",
                    "Финальный босс повержен. Можно собрать оставшиеся награды.",
                    22f,
                    FontStyles.Normal,
                    new Vector2(0f, 27f),
                    new Vector2(510f, 72f));
                Button continueButton = CreateButton(
                    victoryCard,
                    "ContinueCollectingButton",
                    "Продолжить сбор",
                    SecondaryColor,
                    new Vector2(-145f, -87f),
                    new Vector2(250f, 58f));
                Button victoryCompleteButton = CreateButton(
                    victoryCard,
                    "CompleteRunButton",
                    "Завершить забег",
                    PrimaryColor,
                    new Vector2(145f, -87f),
                    new Vector2(250f, 58f));

                GameObject collapsedPanel = CreateUiObject(
                    "CollapsedPanel",
                    root.transform);
                RectTransform collapsedRect =
                    collapsedPanel.GetComponent<RectTransform>();
                collapsedRect.anchorMin = Vector2.one;
                collapsedRect.anchorMax = Vector2.one;
                collapsedRect.pivot = Vector2.one;
                collapsedRect.anchoredPosition = new Vector2(-32f, -32f);
                collapsedRect.sizeDelta = new Vector2(270f, 62f);
                Button collapsedCompleteButton = CreateButton(
                    collapsedPanel.transform,
                    "CompleteRunButton",
                    "Завершить забег",
                    PrimaryColor,
                    Vector2.zero,
                    collapsedRect.sizeDelta);

                GameObject confirmationPanel = CreateModalPanel(
                    root.transform,
                    "ConfirmationPanel",
                    new Vector2(620f, 310f));
                Transform confirmationCard = confirmationPanel.transform.Find("Card");
                CreateText(
                    confirmationCard,
                    "Title",
                    "Завершить забег?",
                    34f,
                    FontStyles.Bold,
                    new Vector2(0f, 88f),
                    new Vector2(520f, 54f));
                CreateText(
                    confirmationCard,
                    "Description",
                    "Несобранные предметы на земле будут потеряны.",
                    22f,
                    FontStyles.Normal,
                    new Vector2(0f, 24f),
                    new Vector2(510f, 68f));
                Button cancelButton = CreateButton(
                    confirmationCard,
                    "CancelButton",
                    "Отмена",
                    SecondaryColor,
                    new Vector2(-145f, -80f),
                    new Vector2(250f, 58f));
                Button confirmButton = CreateButton(
                    confirmationCard,
                    "ConfirmButton",
                    "Завершить",
                    PrimaryColor,
                    new Vector2(145f, -80f),
                    new Vector2(250f, 58f));

                GameObject completedPanel = CreateModalPanel(
                    root.transform,
                    "CompletedPanel",
                    new Vector2(570f, 240f));
                Transform completedCard = completedPanel.transform.Find("Card");
                CreateText(
                    completedCard,
                    "Title",
                    "ЗАБЕГ ЗАВЕРШЁН",
                    34f,
                    FontStyles.Bold,
                    new Vector2(0f, 48f),
                    new Vector2(490f, 58f));
                CreateText(
                    completedCard,
                    "Description",
                    "Финальный босс повержен. Забег успешно завершён.",
                    21f,
                    FontStyles.Normal,
                    new Vector2(0f, -28f),
                    new Vector2(480f, 70f));

                ConfigureView(
                    view,
                    victoryPanel,
                    collapsedPanel,
                    confirmationPanel,
                    completedPanel,
                    continueButton,
                    victoryCompleteButton,
                    collapsedCompleteButton,
                    cancelButton,
                    confirmButton);
                SerializedObject serializedController = new(controller);
                serializedController.FindProperty("view").objectReferenceValue = view;
                serializedController.ApplyModifiedPropertiesWithoutUndo();

                victoryPanel.SetActive(false);
                collapsedPanel.SetActive(false);
                confirmationPanel.SetActive(false);
                completedPanel.SetActive(false);

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                if (saved == null)
                    throw new InvalidOperationException($"Could not save {PrefabPath}.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void ConfigureScene(Scene scene)
        {
            GameObject runtimeObject = FindRootObject(scene, RuntimeObjectName);
            RunFlowRuntime runtime = runtimeObject != null
                ? runtimeObject.GetComponent<RunFlowRuntime>()
                : null;
            Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>(
                FindObjectsInactive.Include);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (runtime == null || canvas == null || prefab == null)
            {
                throw new InvalidOperationException(
                    "RunFlowRuntime, scene Canvas, or Run Completion prefab is missing.");
            }

            Transform existing = canvas.transform.Find(UiObjectName);
            GameObject instance;
            if (existing == null)
            {
                instance = PrefabUtility.InstantiatePrefab(
                    prefab,
                    canvas.transform) as GameObject;
                if (instance == null)
                    throw new InvalidOperationException("Could not instantiate Run Completion UI.");

                Undo.RegisterCreatedObjectUndo(instance, "Create Run Completion UI");
                instance.name = UiObjectName;
            }
            else
            {
                instance = existing.gameObject;
            }

            RunCompletionView view = instance.GetComponent<RunCompletionView>();
            RunCompletionController controller =
                instance.GetComponent<RunCompletionController>();
            if (view == null || controller == null)
                throw new InvalidOperationException("Run Completion UI components are missing.");

            SerializedObject serializedController = new(controller);
            serializedController.FindProperty("runFlowRuntime").objectReferenceValue = runtime;
            serializedController.FindProperty("view").objectReferenceValue = view;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException($"Could not save {ScenePath}.");
        }

        private static void ValidateInternal()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                throw new InvalidOperationException($"Open {ScenePath} before validation.");

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                throw new InvalidOperationException($"Prefab not found: {PrefabPath}");

            ValidateComponents(prefab, false);

            GameObject runtimeObject = FindRootObject(scene, RuntimeObjectName);
            RunFlowRuntime runtime = runtimeObject != null
                ? runtimeObject.GetComponent<RunFlowRuntime>()
                : null;
            Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>(
                FindObjectsInactive.Include);
            Transform uiTransform = canvas != null
                ? canvas.transform.Find(UiObjectName)
                : null;
            if (runtime == null || uiTransform == null)
                throw new InvalidOperationException("Run Completion scene wiring is missing.");

            RunCompletionController controller =
                uiTransform.GetComponent<RunCompletionController>();
            ValidateComponents(uiTransform.gameObject, true);

            SerializedObject serializedController = new(controller);
            if (serializedController.FindProperty("runFlowRuntime")
                    .objectReferenceValue != runtime)
            {
                throw new InvalidOperationException(
                    "RunCompletionController is not bound to RunFlowRuntime.");
            }
        }

        private static void ValidateComponents(GameObject root, bool requireRuntime)
        {
            RunCompletionView view = root.GetComponent<RunCompletionView>();
            RunCompletionController controller = root.GetComponent<RunCompletionController>();
            if (view == null || controller == null)
                throw new InvalidOperationException($"{root.name} lacks completion UI components.");

            SerializedObject serializedView = new(view);
            string[] viewReferences =
            {
                "victoryPanel",
                "collapsedPanel",
                "confirmationPanel",
                "completedPanel",
                "continueCollectingButton",
                "victoryCompleteButton",
                "collapsedCompleteButton",
                "cancelCompletionButton",
                "confirmCompletionButton"
            };
            for (int i = 0; i < viewReferences.Length; i++)
            {
                if (serializedView.FindProperty(viewReferences[i]).objectReferenceValue == null)
                {
                    throw new InvalidOperationException(
                        $"{root.name} has no {viewReferences[i]} reference.");
                }
            }

            SerializedObject serializedController = new(controller);
            if (serializedController.FindProperty("view").objectReferenceValue != view)
                throw new InvalidOperationException($"{root.name} controller view is not wired.");

            if (requireRuntime &&
                serializedController.FindProperty("runFlowRuntime").objectReferenceValue == null)
            {
                throw new InvalidOperationException($"{root.name} controller runtime is not wired.");
            }
        }

        private static void ConfigureView(
            RunCompletionView view,
            GameObject victoryPanel,
            GameObject collapsedPanel,
            GameObject confirmationPanel,
            GameObject completedPanel,
            Button continueButton,
            Button victoryCompleteButton,
            Button collapsedCompleteButton,
            Button cancelButton,
            Button confirmButton)
        {
            SerializedObject serializedView = new(view);
            serializedView.FindProperty("victoryPanel").objectReferenceValue = victoryPanel;
            serializedView.FindProperty("collapsedPanel").objectReferenceValue = collapsedPanel;
            serializedView.FindProperty("confirmationPanel").objectReferenceValue = confirmationPanel;
            serializedView.FindProperty("completedPanel").objectReferenceValue = completedPanel;
            serializedView.FindProperty("continueCollectingButton").objectReferenceValue = continueButton;
            serializedView.FindProperty("victoryCompleteButton").objectReferenceValue = victoryCompleteButton;
            serializedView.FindProperty("collapsedCompleteButton").objectReferenceValue = collapsedCompleteButton;
            serializedView.FindProperty("cancelCompletionButton").objectReferenceValue = cancelButton;
            serializedView.FindProperty("confirmCompletionButton").objectReferenceValue = confirmButton;
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

        private static GameObject CreateUiObject(string name, Transform parent)
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
                    $"Run Completion UI {operation} is available only in Edit Mode.");
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
    }
}
