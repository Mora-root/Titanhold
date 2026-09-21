using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Titanhold.UI.Run.Editor
{
    public static class RunUpgradeHudWiringEditor
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/SampleScene.unity";
        private const string RootName = "RunUpgradeHUD";
        private const string ContentName = "Content";
        private const string RowsName = "Rows";
        private const string RowTemplateName = "RowTemplate";
        private const string PlayerHudName = "PlayerHUD";
        private const string LegacyWaveHudName = "WaveHUD";
        private const string PlayerId = "player:local";

        [MenuItem("Tools/Titanhold/Install Run Upgrade HUD Wiring")]
        public static void Install()
        {
            try
            {
                RequireEditMode("installation");
                RequireCleanOpenScene();
                Scene scene = EditorSceneManager.OpenScene(
                    ScenePath,
                    OpenSceneMode.Single);
                InstallInternal(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("Could not save run scene.");

                AssetDatabase.SaveAssets();
                ValidateInternal(scene);
                Debug.Log("Run Upgrade HUD wiring installed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Run Upgrade HUD wiring installation failed: {exception}");
            }
        }

        [MenuItem("Tools/Titanhold/Validate Run Upgrade HUD Wiring")]
        public static void Validate()
        {
            try
            {
                RequireEditMode("validation");
                Scene scene = EditorSceneManager.OpenScene(
                    ScenePath,
                    OpenSceneMode.Single);
                ValidateInternal(scene);
                Debug.Log("Run Upgrade HUD wiring validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Run Upgrade HUD wiring validation failed: {exception}");
            }
        }

        private static void InstallInternal(Scene scene)
        {
            GameObject playerHud = FindSceneObject(scene, PlayerHudName);
            Canvas canvas = playerHud != null
                ? playerHud.GetComponentInParent<Canvas>(true)
                : null;
            if (canvas == null)
            {
                throw new InvalidOperationException(
                    "Could not resolve the gameplay HUD canvas.");
            }

            TMP_Text styleSource =
                playerHud.GetComponentInChildren<TMP_Text>(true);
            GameObject root = FindSceneObject(scene, RootName) ??
                CreateUiObject(RootName, canvas.transform);
            root.layer = canvas.gameObject.layer;
            RectTransform rootRect = RequireRectTransform(root);
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);
            rootRect.anchoredPosition = new Vector2(24f, -24f);
            rootRect.sizeDelta = new Vector2(280f, 420f);

            GameObject content = FindDirectChild(root.transform, ContentName) ??
                CreateUiObject(ContentName, root.transform);
            RectTransform contentRect = RequireRectTransform(content);
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(0f, 1f);
            contentRect.pivot = new Vector2(0f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(280f, 0f);

            Image background = content.GetComponent<Image>() ??
                Undo.AddComponent<Image>(content);
            background.color = new Color(0.025f, 0.025f, 0.035f, 0.78f);
            background.raycastTarget = false;
            VerticalLayoutGroup contentLayout =
                content.GetComponent<VerticalLayoutGroup>() ??
                Undo.AddComponent<VerticalLayoutGroup>(content);
            contentLayout.padding = new RectOffset(12, 12, 10, 12);
            contentLayout.spacing = 5f;
            contentLayout.childAlignment = TextAnchor.UpperLeft;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            ContentSizeFitter contentFitter =
                content.GetComponent<ContentSizeFitter>() ??
                Undo.AddComponent<ContentSizeFitter>(content);
            contentFitter.horizontalFit =
                ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TMP_Text title = RequireText(
                content.transform,
                "Title",
                styleSource);
            ConfigureText(title, 18f, FontStyles.Bold, 28f);
            title.text = "RUN UPGRADES";
            title.color = new Color(0.95f, 0.78f, 0.28f, 1f);
            title.transform.SetSiblingIndex(0);

            GameObject rowsObject = FindDirectChild(
                    content.transform,
                    RowsName) ??
                CreateUiObject(RowsName, content.transform);
            RectTransform rowsRoot = RequireRectTransform(rowsObject);
            rowsRoot.sizeDelta = new Vector2(0f, 0f);
            VerticalLayoutGroup rowsLayout =
                rowsObject.GetComponent<VerticalLayoutGroup>() ??
                Undo.AddComponent<VerticalLayoutGroup>(rowsObject);
            rowsLayout.spacing = 3f;
            rowsLayout.childAlignment = TextAnchor.UpperLeft;
            rowsLayout.childControlWidth = true;
            rowsLayout.childControlHeight = true;
            rowsLayout.childForceExpandWidth = true;
            rowsLayout.childForceExpandHeight = false;
            ContentSizeFitter rowsFitter =
                rowsObject.GetComponent<ContentSizeFitter>() ??
                Undo.AddComponent<ContentSizeFitter>(rowsObject);
            rowsFitter.horizontalFit =
                ContentSizeFitter.FitMode.Unconstrained;
            rowsFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            rowsObject.transform.SetSiblingIndex(1);

            TMP_Text rowTemplate = RequireText(
                rowsRoot,
                RowTemplateName,
                styleSource);
            ConfigureText(rowTemplate, 15f, FontStyles.Normal, 24f);
            rowTemplate.text = "Upgrade";
            rowTemplate.color = Color.white;
            rowTemplate.gameObject.SetActive(false);

            RunUpgradeHudView view =
                root.GetComponent<RunUpgradeHudView>() ??
                Undo.AddComponent<RunUpgradeHudView>(root);
            RunUpgradeHudPresenter presenter =
                root.GetComponent<RunUpgradeHudPresenter>() ??
                Undo.AddComponent<RunUpgradeHudPresenter>(root);
            view.ConfigureForEditor(
                content,
                title,
                rowsRoot,
                rowTemplate);
            presenter.ConfigureForEditor(view, PlayerId);
            root.SetActive(true);
            content.SetActive(false);

            EditorUtility.SetDirty(rootRect);
            EditorUtility.SetDirty(background);
            EditorUtility.SetDirty(contentLayout);
            EditorUtility.SetDirty(contentFitter);
            EditorUtility.SetDirty(title);
            EditorUtility.SetDirty(rowsLayout);
            EditorUtility.SetDirty(rowsFitter);
            EditorUtility.SetDirty(rowTemplate);
            EditorUtility.SetDirty(view);
            EditorUtility.SetDirty(presenter);
        }

        private static void ValidateInternal(Scene scene)
        {
            GameObject root = FindSceneObject(scene, RootName);
            RunUpgradeHudView[] views =
                UnityEngine.Object.FindObjectsByType<RunUpgradeHudView>(
                    FindObjectsInactive.Include);
            RunUpgradeHudPresenter[] presenters =
                UnityEngine.Object.FindObjectsByType<RunUpgradeHudPresenter>(
                    FindObjectsInactive.Include);
            if (root == null || !root.activeSelf ||
                root.GetComponentInParent<Canvas>() == null ||
                views.Length != 1 || presenters.Length != 1 ||
                views[0].gameObject != root ||
                presenters[0].gameObject != root ||
                !views[0].HasRequiredReferences ||
                presenters[0].View != views[0] ||
                presenters[0].PlayerId != PlayerId ||
                views[0].ContentRoot.activeSelf ||
                views[0].RowTemplate.gameObject.activeSelf)
            {
                throw new InvalidOperationException(
                    "Run Upgrade HUD view or presenter wiring is invalid.");
            }

            GameObject legacyWaveHud =
                FindSceneObject(scene, LegacyWaveHudName);
            if (legacyWaveHud != null && legacyWaveHud.activeSelf)
            {
                throw new InvalidOperationException(
                    "Legacy WaveHUD must remain disabled.");
            }
        }

        private static void ConfigureText(
            TMP_Text text,
            float fontSize,
            FontStyles fontStyle,
            float preferredHeight)
        {
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            LayoutElement layout = text.GetComponent<LayoutElement>() ??
                Undo.AddComponent<LayoutElement>(text.gameObject);
            layout.preferredHeight = preferredHeight;
            layout.flexibleWidth = 1f;
        }

        private static TMP_Text RequireText(
            Transform parent,
            string name,
            TMP_Text styleSource)
        {
            GameObject target = FindDirectChild(parent, name) ??
                CreateUiObject(name, parent);
            TMP_Text text = target.GetComponent<TMP_Text>() ??
                Undo.AddComponent<TextMeshProUGUI>(target);
            if (styleSource != null && styleSource.font != null)
            {
                text.font = styleSource.font;
                text.fontSharedMaterial = styleSource.fontSharedMaterial;
            }

            return text;
        }

        private static RectTransform RequireRectTransform(GameObject target)
        {
            RectTransform rect = target.GetComponent<RectTransform>();
            if (rect == null)
            {
                throw new InvalidOperationException(
                    $"UI object '{target.name}' has no RectTransform.");
            }

            return rect;
        }

        private static GameObject CreateUiObject(
            string name,
            Transform parent)
        {
            GameObject created = new(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(created, $"Create {name}");
            created.transform.SetParent(parent, false);
            created.layer = parent.gameObject.layer;
            return created;
        }

        private static GameObject FindDirectChild(
            Transform parent,
            string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == name)
                    return child.gameObject;
            }

            return null;
        }

        private static GameObject FindSceneObject(Scene scene, string name)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform match = FindRecursive(roots[i].transform, name);
                if (match != null)
                    return match.gameObject;
            }

            return null;
        }

        private static Transform FindRecursive(Transform current, string name)
        {
            if (current.name == name)
                return current;

            for (int i = 0; i < current.childCount; i++)
            {
                Transform match = FindRecursive(current.GetChild(i), name);
                if (match != null)
                    return match;
            }

            return null;
        }

        private static void RequireEditMode(string operation)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    $"Exit Play Mode before Run Upgrade HUD {operation}.");
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
