using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Titanhold.UI.Hub.Editor
{
    public static class HubStartingAbilitySelectionWiringEditor
    {
        private const string HubScenePath =
            "Assets/_Project/Scenes/HubScene.unity";

        private static readonly Color OverlayColor =
            new(0.008f, 0.012f, 0.02f, 0.94f);
        private static readonly Color PanelColor =
            new(0.035f, 0.047f, 0.067f, 1f);
        private static readonly Color CardColor =
            new(0.065f, 0.082f, 0.108f, 1f);
        private static readonly Color AccentColor =
            new(0.76f, 0.52f, 0.18f, 1f);
        private static readonly Color MutedTextColor =
            new(0.62f, 0.67f, 0.74f, 1f);

        [MenuItem("Tools/Titanhold/Install Starting Ability Selection UI")]
        public static void Install()
        {
            try
            {
                RequireEditMode("installation");
                RequireCleanOpenScene();
                Scene scene = EditorSceneManager.OpenScene(
                    HubScenePath,
                    OpenSceneMode.Single);
                if (UnityEngine.Object.FindAnyObjectByType<
                        HubStartingAbilitySelectionView>(
                        FindObjectsInactive.Include) != null)
                {
                    ValidateInternal();
                    Debug.Log("Starting Ability Selection UI is already installed.");
                    return;
                }

                Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>(
                    FindObjectsInactive.Include);
                HubRunLaunchController launchController =
                    UnityEngine.Object.FindAnyObjectByType<
                        HubRunLaunchController>(FindObjectsInactive.Include);
                if (canvas == null || launchController == null)
                {
                    throw new InvalidOperationException(
                        "Hub Canvas or run launch controller is missing.");
                }

                GameObject uiRoot = CreateUiObject(
                    "StartingAbilitySelectionUI",
                    canvas.transform);
                Stretch(uiRoot.GetComponent<RectTransform>());
                HubStartingAbilitySelectionView view =
                    uiRoot.AddComponent<HubStartingAbilitySelectionView>();
                HubStartingAbilitySelectionController controller =
                    uiRoot.AddComponent<HubStartingAbilitySelectionController>();

                GameObject overlay = CreateUiObject(
                    "SelectionOverlay",
                    uiRoot.transform);
                Stretch(overlay.GetComponent<RectTransform>());
                overlay.AddComponent<Image>().color = OverlayColor;

                GameObject panel = CreateUiObject(
                    "SelectionPanel",
                    overlay.transform);
                SetCenteredRect(
                    panel.GetComponent<RectTransform>(),
                    new Vector2(1160f, 610f),
                    Vector2.zero);
                panel.AddComponent<Image>().color = PanelColor;
                CreateText(
                    panel.transform,
                    "Title",
                    "CHOOSE YOUR STARTING ABILITY",
                    30f,
                    FontStyles.Bold,
                    new Vector2(0f, 246f),
                    new Vector2(960f, 54f),
                    Color.white);
                CreateText(
                    panel.transform,
                    "Subtitle",
                    "This ability occupies your first run slot.",
                    17f,
                    FontStyles.Normal,
                    new Vector2(0f, 205f),
                    new Vector2(860f, 34f),
                    MutedTextColor);

                Button[] buttons = new Button[3];
                TMP_Text[] names = new TMP_Text[3];
                TMP_Text[] descriptions = new TMP_Text[3];
                Image[] icons = new Image[3];
                for (int i = 0; i < buttons.Length; i++)
                {
                    float x = (i - 1) * 350f;
                    CreateOptionCard(
                        panel.transform,
                        i,
                        new Vector2(x, -15f),
                        out buttons[i],
                        out names[i],
                        out descriptions[i],
                        out icons[i]);
                }

                TMP_Text status = CreateText(
                    panel.transform,
                    "Status",
                    string.Empty,
                    16f,
                    FontStyles.Normal,
                    new Vector2(0f, -267f),
                    new Vector2(900f, 30f),
                    MutedTextColor);
                view.ConfigureForEditor(
                    overlay,
                    status,
                    buttons,
                    names,
                    descriptions,
                    icons);
                controller.ConfigureForEditor(view, launchController);
                overlay.SetActive(false);

                EditorUtility.SetDirty(view);
                EditorUtility.SetDirty(controller);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("Could not save Hub scene.");

                ValidateInternal();
                Debug.Log("Starting Ability Selection UI installed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Starting Ability Selection UI installation failed: {exception}");
            }
        }

        [MenuItem("Tools/Titanhold/Validate Starting Ability Selection UI Wiring")]
        public static void Validate()
        {
            try
            {
                RequireEditMode("validation");
                ValidateInternal();
                Debug.Log(
                    "Starting Ability Selection UI wiring validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Starting Ability Selection UI wiring validation failed: {exception}");
            }
        }

        private static void CreateOptionCard(
            Transform parent,
            int index,
            Vector2 position,
            out Button button,
            out TMP_Text nameText,
            out TMP_Text descriptionText,
            out Image icon)
        {
            GameObject card = CreateUiObject($"AbilityOption{index + 1}", parent);
            SetCenteredRect(
                card.GetComponent<RectTransform>(),
                new Vector2(310f, 390f),
                position);
            Image background = card.AddComponent<Image>();
            background.color = CardColor;
            button = card.AddComponent<Button>();
            button.targetGraphic = background;
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.88f, 0.67f, 1f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.65f);
            button.colors = colors;

            GameObject accent = CreateUiObject("Accent", card.transform);
            RectTransform accentRect = accent.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 1f);
            accentRect.anchorMax = new Vector2(1f, 1f);
            accentRect.pivot = new Vector2(0.5f, 1f);
            accentRect.anchoredPosition = Vector2.zero;
            accentRect.sizeDelta = new Vector2(0f, 5f);
            Image accentImage = accent.AddComponent<Image>();
            accentImage.color = AccentColor;
            accentImage.raycastTarget = false;

            GameObject iconObject = CreateUiObject("Icon", card.transform);
            SetCenteredRect(
                iconObject.GetComponent<RectTransform>(),
                new Vector2(104f, 104f),
                new Vector2(0f, 100f));
            icon = iconObject.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.enabled = false;

            nameText = CreateText(
                card.transform,
                "Name",
                $"ABILITY {index + 1}",
                23f,
                FontStyles.Bold,
                new Vector2(0f, 22f),
                new Vector2(260f, 58f),
                Color.white);
            descriptionText = CreateText(
                card.transform,
                "Description",
                string.Empty,
                16f,
                FontStyles.Normal,
                new Vector2(0f, -89f),
                new Vector2(250f, 132f),
                MutedTextColor);
        }

        private static void ValidateInternal()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != HubScenePath)
                throw new InvalidOperationException($"Open '{HubScenePath}'.");

            HubStartingAbilitySelectionView[] views =
                UnityEngine.Object.FindObjectsByType<
                    HubStartingAbilitySelectionView>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            HubStartingAbilitySelectionController[] controllers =
                UnityEngine.Object.FindObjectsByType<
                    HubStartingAbilitySelectionController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            HubRunLaunchController launchController =
                UnityEngine.Object.FindAnyObjectByType<
                    HubRunLaunchController>(FindObjectsInactive.Include);
            Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>(
                FindObjectsInactive.Include);
            if (views.Length != 1 || controllers.Length != 1 ||
                launchController == null || canvas == null)
            {
                throw new InvalidOperationException(
                    "Starting selection UI components are missing or duplicated.");
            }

            HubStartingAbilitySelectionView view = views[0];
            HubStartingAbilitySelectionController controller = controllers[0];
            if (!view.HasRequiredReferences ||
                view.SelectionRoot == null ||
                view.SelectionRoot.activeSelf ||
                !view.transform.IsChildOf(canvas.transform) ||
                !controller.HasRequiredReferences ||
                controller.View != view ||
                controller.LaunchController != launchController)
            {
                throw new InvalidOperationException(
                    "Starting selection UI references are invalid.");
            }
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
            SetCenteredRect(
                textObject.GetComponent<RectTransform>(),
                size,
                position);
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

        private static void RequireEditMode(string operation)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    $"Exit Play Mode before starting ability UI {operation}.");
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
