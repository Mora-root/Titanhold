using System;
using System.Collections.Generic;
using TMPro;
using Titanhold.UI.Hub;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Titanhold.Session.Editor
{
    public static class HubSceneWiringEditor
    {
        private const string HubScenePath =
            "Assets/_Project/Scenes/HubScene.unity";
        private const string RunScenePath =
            "Assets/_Project/Scenes/SampleScene.unity";
        private const string CatalogPath =
            "Assets/_Project/ScriptableObjects/Items/ItemDefinitionCatalog.asset";

        private static readonly Color BackgroundColor =
            new(0.018f, 0.025f, 0.038f, 1f);
        private static readonly Color PanelColor =
            new(0.035f, 0.047f, 0.067f, 0.98f);
        private static readonly Color CardColor =
            new(0.065f, 0.082f, 0.108f, 1f);
        private static readonly Color AccentColor =
            new(0.76f, 0.52f, 0.18f, 1f);
        private static readonly Color MutedTextColor =
            new(0.62f, 0.67f, 0.74f, 1f);

        [MenuItem("Tools/Titanhold/Build Run Preparation Hub Scene")]
        public static void Build()
        {
            try
            {
                RequireEditMode("build");
                RequireCleanOpenScene();

                ItemDefinitionCatalog catalog =
                    AssetDatabase.LoadAssetAtPath<ItemDefinitionCatalog>(
                        CatalogPath);
                if (catalog == null || !catalog.IsValid)
                {
                    throw new InvalidOperationException(
                        "A valid runtime Item Definition Catalog is required.");
                }

                Scene scene = EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Single);
                CreateSessionRoot(catalog);
                CreateHubCamera();
                CreateHubUi();
                CreateEventSystem();

                if (!EditorSceneManager.SaveScene(scene, HubScenePath))
                    throw new InvalidOperationException("Could not save Hub scene.");

                ConfigureBuildSettings();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                ValidateInternal();
                Debug.Log("Run Preparation Hub scene built.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"Run Preparation Hub scene build failed: {exception}");
            }
        }

        [MenuItem("Tools/Titanhold/Validate Run Preparation Hub Scene")]
        public static void Validate()
        {
            try
            {
                RequireEditMode("validation");
                ValidateInternal();
                Debug.Log("Run Preparation Hub scene validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Run Preparation Hub scene validation failed: {exception}");
            }
        }

        private static void CreateSessionRoot(ItemDefinitionCatalog catalog)
        {
            GameObject root = new("GameSessionRoot");
            GameSessionRuntimeHost host =
                root.AddComponent<GameSessionRuntimeHost>();
            host.ConfigureForEditor(catalog);
            EditorUtility.SetDirty(host);
            EditorSceneManager.MarkSceneDirty(root.scene);
        }

        private static void CreateHubUi()
        {
            GameObject canvasObject = new(
                "HubCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.layer = LayerMask.NameToLayer("UI");

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            GameObject background = CreateUiObject(
                "Background",
                canvasObject.transform);
            Stretch(background.GetComponent<RectTransform>());
            background.AddComponent<Image>().color = BackgroundColor;

            GameObject topAccent = CreateUiObject(
                "TopAccent",
                background.transform);
            RectTransform accentRect = topAccent.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 1f);
            accentRect.anchorMax = new Vector2(1f, 1f);
            accentRect.pivot = new Vector2(0.5f, 1f);
            accentRect.anchoredPosition = Vector2.zero;
            accentRect.sizeDelta = new Vector2(0f, 6f);
            topAccent.AddComponent<Image>().color = AccentColor;

            CreateText(
                background.transform,
                "GameTitle",
                "TITANHOLD",
                52f,
                FontStyles.Bold,
                new Vector2(0f, 425f),
                new Vector2(1000f, 80f),
                Color.white);
            CreateText(
                background.transform,
                "ScreenTitle",
                "RUN PREPARATION",
                20f,
                FontStyles.Normal,
                new Vector2(0f, 370f),
                new Vector2(800f, 40f),
                AccentColor);

            GameObject panel = CreateUiObject("PreparationPanel", background.transform);
            SetCenteredRect(
                panel.GetComponent<RectTransform>(),
                new Vector2(1180f, 700f),
                new Vector2(0f, -30f));
            panel.AddComponent<Image>().color = PanelColor;
            HubRunPreparationView view =
                panel.AddComponent<HubRunPreparationView>();

            CreateText(
                panel.transform,
                "SelectionTitle",
                "EXPEDITION LOADOUT",
                25f,
                FontStyles.Bold,
                new Vector2(-280f, 292f),
                new Vector2(520f, 48f),
                Color.white);

            GameObject characterCard = CreateCard(
                panel.transform,
                "CharacterCard",
                new Vector2(-280f, 158f),
                new Vector2(520f, 170f));
            CreateCardLabel(characterCard.transform, "CHARACTER");
            TMP_Text characterName = CreateText(
                characterCard.transform,
                "CharacterName",
                "WARRIOR",
                34f,
                FontStyles.Bold,
                new Vector2(0f, 10f),
                new Vector2(440f, 54f),
                Color.white);
            CreateText(
                characterCard.transform,
                "CharacterDescription",
                "Energy and Rage",
                18f,
                FontStyles.Normal,
                new Vector2(0f, -48f),
                new Vector2(440f, 34f),
                MutedTextColor);

            GameObject difficultyCard = CreateCard(
                panel.transform,
                "DifficultyCard",
                new Vector2(-280f, -48f),
                new Vector2(520f, 190f));
            CreateCardLabel(difficultyCard.transform, "DIFFICULTY");
            TMP_Text difficultyName = CreateText(
                difficultyCard.transform,
                "DifficultyName",
                "PROTOTYPE",
                31f,
                FontStyles.Bold,
                new Vector2(0f, 17f),
                new Vector2(440f, 50f),
                Color.white);
            CreateText(
                difficultyCard.transform,
                "DifficultyDescription",
                "3 rounds + final boss",
                18f,
                FontStyles.Normal,
                new Vector2(0f, -42f),
                new Vector2(440f, 34f),
                MutedTextColor);

            Button startButton = CreateButton(
                panel.transform,
                "StartRunButton",
                "START RUN",
                new Vector2(-280f, -264f),
                new Vector2(520f, 76f));
            TMP_Text status = CreateText(
                panel.transform,
                "Status",
                "READY",
                16f,
                FontStyles.Normal,
                new Vector2(-280f, -319f),
                new Vector2(520f, 28f),
                MutedTextColor);

            GameObject futurePanel = CreateCard(
                panel.transform,
                "FutureSystemsPanel",
                new Vector2(315f, 0f),
                new Vector2(430f, 580f));
            CreateText(
                futurePanel.transform,
                "Title",
                "CHARACTER SETUP",
                23f,
                FontStyles.Bold,
                new Vector2(0f, 235f),
                new Vector2(360f, 44f),
                Color.white);
            CreateFeatureRow(futurePanel.transform, "Equipment", 132f);
            CreateFeatureRow(futurePanel.transform, "Talents", 22f);
            CreateFeatureRow(futurePanel.transform, "Abilities", -88f);
            CreateText(
                futurePanel.transform,
                "FutureNote",
                "These systems will be connected here as the vertical slice grows.",
                17f,
                FontStyles.Normal,
                new Vector2(0f, -207f),
                new Vector2(340f, 82f),
                MutedTextColor);

            SerializedObject serializedView = new(view);
            serializedView.FindProperty("characterNameText").objectReferenceValue =
                characterName;
            serializedView.FindProperty("difficultyNameText").objectReferenceValue =
                difficultyName;
            serializedView.FindProperty("statusText").objectReferenceValue = status;
            serializedView.FindProperty("startRunButton").objectReferenceValue =
                startButton;
            serializedView.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateHubCamera()
        {
            GameObject cameraObject = new(
                "HubCamera",
                typeof(Camera),
                typeof(AudioListener));
            cameraObject.tag = "MainCamera";

            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = BackgroundColor;
            camera.cullingMask = 0;
            camera.orthographic = true;
            camera.depth = -100f;
        }

        private static void CreateEventSystem()
        {
            GameObject eventSystem = new(
                "EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));
            eventSystem.transform.SetAsLastSibling();
        }

        private static GameObject CreateCard(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size)
        {
            GameObject card = CreateUiObject(name, parent);
            SetCenteredRect(card.GetComponent<RectTransform>(), size, position);
            card.AddComponent<Image>().color = CardColor;
            return card;
        }

        private static void CreateCardLabel(Transform parent, string text)
        {
            TMP_Text label = CreateText(
                parent,
                "Label",
                text,
                15f,
                FontStyles.Bold,
                new Vector2(0f, 60f),
                new Vector2(440f, 28f),
                AccentColor);
            label.alignment = TextAlignmentOptions.MidlineLeft;
        }

        private static void CreateFeatureRow(
            Transform parent,
            string label,
            float y)
        {
            GameObject row = CreateUiObject(label + "Row", parent);
            SetCenteredRect(
                row.GetComponent<RectTransform>(),
                new Vector2(340f, 82f),
                new Vector2(0f, y));
            row.AddComponent<Image>().color = PanelColor;
            TMP_Text title = CreateText(
                row.transform,
                "Label",
                label.ToUpperInvariant(),
                19f,
                FontStyles.Bold,
                new Vector2(-8f, 10f),
                new Vector2(280f, 32f),
                Color.white);
            title.alignment = TextAlignmentOptions.MidlineLeft;
            TMP_Text state = CreateText(
                row.transform,
                "State",
                "COMING LATER",
                13f,
                FontStyles.Normal,
                new Vector2(-8f, -20f),
                new Vector2(280f, 24f),
                MutedTextColor);
            state.alignment = TextAlignmentOptions.MidlineLeft;
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            string label,
            Vector2 position,
            Vector2 size)
        {
            GameObject buttonObject = CreateUiObject(name, parent);
            SetCenteredRect(buttonObject.GetComponent<RectTransform>(), size, position);
            Image image = buttonObject.AddComponent<Image>();
            image.color = AccentColor;
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.92f, 0.78f, 1f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            button.colors = colors;

            CreateText(
                buttonObject.transform,
                "Label",
                label,
                23f,
                FontStyles.Bold,
                Vector2.zero,
                size - new Vector2(24f, 14f),
                Color.white);
            return button;
        }

        private static TMP_Text CreateText(
            Transform parent,
            string name,
            string content,
            float fontSize,
            FontStyles fontStyle,
            Vector2 position,
            Vector2 size,
            Color color)
        {
            GameObject textObject = CreateUiObject(name, parent);
            SetCenteredRect(textObject.GetComponent<RectTransform>(), size, position);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject result = new(name, typeof(RectTransform));
            result.layer = LayerMask.NameToLayer("UI");
            if (parent != null)
                result.transform.SetParent(parent, false);
            return result;
        }

        private static void SetCenteredRect(
            RectTransform rect,
            Vector2 size,
            Vector2 position)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void ConfigureBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new()
            {
                new EditorBuildSettingsScene(HubScenePath, true)
            };

            EditorBuildSettingsScene[] existing = EditorBuildSettings.scenes;
            bool runSceneAdded = false;
            for (int i = 0; i < existing.Length; i++)
            {
                EditorBuildSettingsScene entry = existing[i];
                if (entry.path == HubScenePath)
                    continue;

                scenes.Add(entry);
                if (entry.path == RunScenePath)
                    runSceneAdded = true;
            }

            if (!runSceneAdded)
                scenes.Add(new EditorBuildSettingsScene(RunScenePath, true));

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void ValidateInternal()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != HubScenePath)
                throw new InvalidOperationException($"Open '{HubScenePath}'.");

            GameSessionRuntimeHost host =
                UnityEngine.Object.FindAnyObjectByType<GameSessionRuntimeHost>(
                    FindObjectsInactive.Include);
            if (host == null || host.transform.parent != null)
                throw new InvalidOperationException("Session root wiring is missing.");

            if (host.ItemDefinitions == null)
            {
                throw new InvalidOperationException(
                    "Session root has no item catalog reference.");
            }

            Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>(
                FindObjectsInactive.Include);
            Camera camera = UnityEngine.Object.FindAnyObjectByType<Camera>(
                FindObjectsInactive.Include);
            HubRunPreparationView view =
                UnityEngine.Object.FindAnyObjectByType<HubRunPreparationView>(
                    FindObjectsInactive.Include);
            EventSystem eventSystem =
                UnityEngine.Object.FindAnyObjectByType<EventSystem>(
                    FindObjectsInactive.Include);
            if (camera == null ||
                !camera.CompareTag("MainCamera") ||
                camera.clearFlags != CameraClearFlags.SolidColor ||
                camera.cullingMask != 0 ||
                camera.GetComponent<AudioListener>() == null ||
                canvas == null || view == null || eventSystem == null ||
                eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                throw new InvalidOperationException(
                    "Hub camera, Canvas, view, or Input System EventSystem is missing.");
            }

            SerializedObject serializedView = new(view);
            string[] references =
            {
                "characterNameText",
                "difficultyNameText",
                "statusText",
                "startRunButton"
            };
            for (int i = 0; i < references.Length; i++)
            {
                if (serializedView.FindProperty(references[i])
                        .objectReferenceValue == null)
                {
                    throw new InvalidOperationException(
                        $"Hub view has no '{references[i]}' reference.");
                }
            }

            EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
            if (buildScenes.Length < 2 ||
                buildScenes[0].path != HubScenePath ||
                !buildScenes[0].enabled ||
                buildScenes[1].path != RunScenePath ||
                !buildScenes[1].enabled)
            {
                throw new InvalidOperationException(
                    "Hub and run scenes are not first and second in Build Settings.");
            }
        }

        private static void RequireEditMode(string operation)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    $"Exit Play Mode before Hub scene {operation}.");
            }
        }

        private static void RequireCleanOpenScene()
        {
            Scene current = SceneManager.GetActiveScene();
            if (current.IsValid() && current.isDirty)
            {
                throw new InvalidOperationException(
                    $"Save the currently open scene '{current.path}' before building the Hub.");
            }
        }
    }
}
