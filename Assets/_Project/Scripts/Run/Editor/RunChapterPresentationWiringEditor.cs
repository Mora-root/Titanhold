using System;
using TMPro;
using Titanhold.Session;
using Titanhold.UI.Run;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Titanhold.Run.Editor
{
    public static class RunChapterPresentationWiringEditor
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/SampleScene.unity";
        private const string RuntimeObjectName = "RunFlowRuntime";
        private const string CanvasObjectName = "Canvas";
        private const string PortalPrefabPath =
            "Assets/_Project/Prefabs/Run/RunChapterBossPortal.prefab";
        private const string HudPrefabPath =
            "Assets/_Project/Prefabs/UI/RunChapterFlowHUD.prefab";
        private const string CrystalMaterialPath =
            "Assets/_Project/Materials/CrystalShard.mat";
        private const string RimMaterialPath =
            "Assets/_Project/Materials/Glow.mat";
        private const int InteractableLayer = 10;
        private const string InteractableLayerName = "Interactable";
        private static readonly Vector2 CutoverHudPosition =
            new(12f, -12f);

        [MenuItem(
            "Tools/Titanhold/Install Run Chapter Portal and HUD Wiring")]
        public static void Install()
        {
            try
            {
                RequireEditMode("installation");
                Scene scene = RequireCleanSampleScene();
                ValidateInteractableLayer();
                RunChapterBossPortalInteractable portalPrefab =
                    CreateOrValidatePortalPrefab();
                RunChapterFlowHudPresenter hudPrefab =
                    CreateOrValidateHudPrefab();
                ConfigureScene(scene, portalPrefab, hudPrefab);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                ValidateInternal(scene, portalPrefab, hudPrefab);
                Debug.Log(
                    "Run Chapter portal and HUD wiring installed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "Run Chapter portal and HUD wiring installation " +
                    $"failed: {exception}");
            }
        }

        [MenuItem(
            "Tools/Titanhold/Validate Run Chapter Portal and HUD Wiring")]
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

                ValidateInteractableLayer();
                RunChapterBossPortalInteractable portalPrefab =
                    ValidatePortalPrefab();
                RunChapterFlowHudPresenter hudPrefab =
                    ValidateHudPrefab();
                ValidateInternal(scene, portalPrefab, hudPrefab);
                Debug.Log(
                    "Run Chapter portal and HUD wiring validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "Run Chapter portal and HUD wiring validation failed: " +
                    exception);
            }
        }

        [MenuItem("Tools/Titanhold/Install Run Chapter Cutover Wiring")]
        public static void InstallCutover()
        {
            try
            {
                RequireEditMode("chapter cutover installation");
                Scene scene = RequireCleanSampleScene();
                ConfigureCutover(scene);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                ValidateCutoverInternal(scene);
                Debug.Log("Run Chapter cutover wiring installed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "Run Chapter cutover wiring installation failed: " +
                    exception);
            }
        }

        [MenuItem("Tools/Titanhold/Validate Run Chapter Cutover Wiring")]
        public static void ValidateCutover()
        {
            try
            {
                RequireEditMode("chapter cutover validation");
                Scene scene = EditorSceneManager.GetActiveScene();
                if (scene.path != ScenePath)
                {
                    throw new InvalidOperationException(
                        $"Open {ScenePath} before validation.");
                }

                ValidateCutoverInternal(scene);
                Debug.Log("Run Chapter cutover wiring validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "Run Chapter cutover wiring validation failed: " +
                    exception);
            }
        }

        private static RunChapterBossPortalInteractable
            CreateOrValidatePortalPrefab()
        {
            RunChapterBossPortalInteractable existing =
                AssetDatabase.LoadAssetAtPath<
                    RunChapterBossPortalInteractable>(PortalPrefabPath);
            if (existing != null)
                return ValidatePortalPrefab();

            Material surfaceMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(
                    CrystalMaterialPath);
            Material rimMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(RimMaterialPath);
            if (surfaceMaterial == null || rimMaterial == null)
            {
                throw new InvalidOperationException(
                    "Portal placeholder materials are missing.");
            }

            GameObject root = new("RunChapterBossPortal");
            try
            {
                SetLayerRecursively(root, InteractableLayer);
                CapsuleCollider collider =
                    root.AddComponent<CapsuleCollider>();
                collider.isTrigger = true;
                collider.direction = 1;
                collider.center = new Vector3(0f, 1.45f, 0f);
                collider.radius = 1.05f;
                collider.height = 2.9f;
                root.AddComponent<TargetVisual>();
                RunChapterBossPortalInteractable interactable =
                    root.AddComponent<RunChapterBossPortalInteractable>();
                SerializedObject serializedInteractable =
                    new(interactable);
                serializedInteractable.FindProperty("interactionRange")
                    .floatValue = 2f;
                serializedInteractable.ApplyModifiedPropertiesWithoutUndo();

                CreatePortalSurface(root.transform, surfaceMaterial);
                CreatePortalRim(root.transform, rimMaterial);
                SetLayerRecursively(root, InteractableLayer);
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(
                    root,
                    PortalPrefabPath);
                if (saved == null)
                {
                    throw new InvalidOperationException(
                        $"Could not save {PortalPrefabPath}.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            AssetDatabase.ImportAsset(
                PortalPrefabPath,
                ImportAssetOptions.ForceSynchronousImport);
            return ValidatePortalPrefab();
        }

        private static void CreatePortalSurface(
            Transform parent,
            Material material)
        {
            GameObject surface =
                GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            surface.name = "PortalSurface";
            surface.transform.SetParent(parent, false);
            surface.transform.localPosition = new Vector3(0f, 1.45f, 0f);
            surface.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            surface.transform.localScale = new Vector3(1f, 0.045f, 1.4f);
            Collider collider = surface.GetComponent<Collider>();
            if (collider != null)
                UnityEngine.Object.DestroyImmediate(collider);

            MeshRenderer renderer = surface.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static void CreatePortalRim(
            Transform parent,
            Material material)
        {
            GameObject rim = new("PortalRim");
            rim.transform.SetParent(parent, false);
            LineRenderer line = rim.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 48;
            line.widthMultiplier = 0.11f;
            line.numCapVertices = 4;
            line.numCornerVertices = 4;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / line.positionCount;
                line.SetPosition(
                    i,
                    new Vector3(
                        Mathf.Cos(angle) * 1.03f,
                        1.45f + Mathf.Sin(angle) * 1.44f,
                        -0.06f));
            }
        }

        private static RunChapterFlowHudPresenter
            CreateOrValidateHudPrefab()
        {
            RunChapterFlowHudPresenter existing =
                AssetDatabase.LoadAssetAtPath<RunChapterFlowHudPresenter>(
                    HudPrefabPath);
            if (existing != null)
                return ValidateHudPrefab();

            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
            if (font == null)
            {
                throw new InvalidOperationException(
                    "The default TMP font asset is missing.");
            }

            GameObject root = new("RunChapterFlowHUD", typeof(RectTransform));
            try
            {
                root.layer = 5;
                RectTransform rootRect = root.GetComponent<RectTransform>();
                rootRect.anchorMin = new Vector2(0.5f, 1f);
                rootRect.anchorMax = new Vector2(0.5f, 1f);
                rootRect.pivot = new Vector2(0.5f, 1f);
                rootRect.anchoredPosition = new Vector2(0f, -28f);
                rootRect.sizeDelta = new Vector2(560f, 154f);

                Image background = root.AddComponent<Image>();
                background.color = new Color(0.035f, 0.045f, 0.07f, 0.86f);
                background.raycastTarget = false;

                TextMeshProUGUI title = CreateText(
                    rootRect,
                    "Title",
                    font,
                    24f,
                    FontStyles.Bold,
                    TextAlignmentOptions.Center,
                    new Vector2(16f, -36f),
                    new Vector2(-16f, -8f));
                Slider progress = CreateProgressBar(rootRect);
                TextMeshProUGUI progressText = CreateText(
                    rootRect,
                    "ProgressText",
                    font,
                    18f,
                    FontStyles.Bold,
                    TextAlignmentOptions.Center,
                    new Vector2(16f, -68f),
                    new Vector2(-16f, -42f));
                TextMeshProUGUI stage = CreateText(
                    rootRect,
                    "StageText",
                    font,
                    16f,
                    FontStyles.Normal,
                    TextAlignmentOptions.Left,
                    new Vector2(16f, -100f),
                    new Vector2(-370f, -76f));
                TextMeshProUGUI collapse = CreateText(
                    rootRect,
                    "CollapseText",
                    font,
                    16f,
                    FontStyles.Bold,
                    TextAlignmentOptions.Center,
                    new Vector2(190f, -100f),
                    new Vector2(-190f, -76f));
                collapse.color = new Color(1f, 0.72f, 0.28f, 1f);
                TextMeshProUGUI instability = CreateText(
                    rootRect,
                    "InstabilityText",
                    font,
                    16f,
                    FontStyles.Normal,
                    TextAlignmentOptions.Right,
                    new Vector2(370f, -100f),
                    new Vector2(-16f, -76f));
                TextMeshProUGUI scaling = CreateText(
                    rootRect,
                    "BossScalingText",
                    font,
                    15f,
                    FontStyles.Normal,
                    TextAlignmentOptions.Left,
                    new Vector2(16f, -136f),
                    new Vector2(-250f, -108f));
                TextMeshProUGUI portal = CreateText(
                    rootRect,
                    "PortalStatusText",
                    font,
                    15f,
                    FontStyles.Bold,
                    TextAlignmentOptions.Right,
                    new Vector2(250f, -136f),
                    new Vector2(-16f, -108f));
                portal.color = new Color(0.55f, 0.9f, 1f, 1f);

                RunChapterFlowHudView view =
                    root.AddComponent<RunChapterFlowHudView>();
                view.ConfigureForEditor(
                    progress,
                    title,
                    progressText,
                    stage,
                    collapse,
                    instability,
                    scaling,
                    portal);
                RunChapterFlowHudPresenter presenter =
                    root.AddComponent<RunChapterFlowHudPresenter>();
                presenter.ConfigureForEditor(null, view);
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(
                    root,
                    HudPrefabPath);
                if (saved == null)
                {
                    throw new InvalidOperationException(
                        $"Could not save {HudPrefabPath}.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            AssetDatabase.ImportAsset(
                HudPrefabPath,
                ImportAssetOptions.ForceSynchronousImport);
            return ValidateHudPrefab();
        }

        private static Slider CreateProgressBar(RectTransform parent)
        {
            GameObject root = new(
                "ProgressBar",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Slider));
            root.layer = 5;
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(16f, -64f);
            rect.offsetMax = new Vector2(-16f, -42f);
            Image background = root.GetComponent<Image>();
            background.color = new Color(0.08f, 0.1f, 0.14f, 1f);
            background.raycastTarget = false;

            GameObject fillObject = new(
                "Fill",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            fillObject.layer = 5;
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.SetParent(rect, false);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(3f, 3f);
            fillRect.offsetMax = new Vector2(-3f, -3f);
            Image fill = fillObject.GetComponent<Image>();
            fill.color = new Color(0.17f, 0.7f, 0.95f, 1f);
            fill.raycastTarget = false;

            Slider slider = root.GetComponent<Slider>();
            slider.transition = Selectable.Transition.None;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.fillRect = fillRect;
            slider.handleRect = null;
            slider.targetGraphic = background;
            slider.interactable = false;
            slider.SetValueWithoutNotify(0f);
            return slider;
        }

        private static TextMeshProUGUI CreateText(
            RectTransform parent,
            string name,
            TMP_FontAsset font,
            float fontSize,
            FontStyles fontStyle,
            TextAlignmentOptions alignment,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            GameObject gameObject = new(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            gameObject.layer = 5;
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            TextMeshProUGUI text = gameObject.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }

        private static void ConfigureScene(
            Scene scene,
            RunChapterBossPortalInteractable portalPrefab,
            RunChapterFlowHudPresenter hudPrefab)
        {
            GameObject runtimeObject = FindRootObject(
                scene,
                RuntimeObjectName);
            GameObject canvasObject = FindRootObject(scene, CanvasObjectName);
            if (runtimeObject == null || canvasObject == null)
            {
                throw new InvalidOperationException(
                    "SampleScene runtime or Canvas root is missing.");
            }

            RunChapterFlowRuntime runtime =
                runtimeObject.GetComponent<RunChapterFlowRuntime>();
            RunSceneSessionEntryPoint sessionEntryPoint =
                FindSceneComponent<RunSceneSessionEntryPoint>(scene);
            PlayerBrain player = FindSceneComponent<PlayerBrain>(scene);
            if (runtime == null || sessionEntryPoint == null || player == null)
            {
                throw new InvalidOperationException(
                    "Chapter runtime, session entry point, or player is missing.");
            }

            runtime.ConfigureForEditor(runtime.Definition, sessionEntryPoint);
            EditorUtility.SetDirty(runtime);
            RunChapterBossPortalSpawner spawner =
                runtimeObject.GetComponent<RunChapterBossPortalSpawner>();
            if (spawner == null)
            {
                spawner = Undo.AddComponent<RunChapterBossPortalSpawner>(
                    runtimeObject);
            }

            spawner.ConfigureForEditor(runtime, portalPrefab, player.transform);
            EditorUtility.SetDirty(spawner);

            RunChapterFlowHudPresenter presenter =
                FindChildComponentByName<RunChapterFlowHudPresenter>(
                    canvasObject.transform,
                    "RunChapterFlowHUD");
            if (presenter == null)
            {
                GameObject instance = PrefabUtility.InstantiatePrefab(
                    hudPrefab.gameObject,
                    scene) as GameObject;
                if (instance == null)
                {
                    throw new InvalidOperationException(
                        "Could not instantiate the Chapter HUD prefab.");
                }

                Undo.RegisterCreatedObjectUndo(
                    instance,
                    "Install Run Chapter HUD");
                instance.transform.SetParent(canvasObject.transform, false);
                presenter =
                    instance.GetComponent<RunChapterFlowHudPresenter>();
            }

            RunChapterFlowHudView view =
                presenter.GetComponent<RunChapterFlowHudView>();
            presenter.ConfigureForEditor(runtime, view);
            EditorUtility.SetDirty(presenter);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException($"Could not save {ScenePath}.");
        }

        private static void ConfigureCutover(Scene scene)
        {
            GameObject runtimeObject = FindRootObject(
                scene,
                RuntimeObjectName);
            GameObject canvasObject = FindRootObject(scene, CanvasObjectName);
            RunChapterFlowRuntime chapterRuntime =
                runtimeObject != null
                    ? runtimeObject.GetComponent<RunChapterFlowRuntime>()
                    : null;
            RunSceneSessionEntryPoint entryPoint =
                FindSceneComponent<RunSceneSessionEntryPoint>(scene);
            RunChapterFlowHudPresenter presenter =
                canvasObject != null
                    ? FindChildComponentByName<RunChapterFlowHudPresenter>(
                        canvasObject.transform,
                        "RunChapterFlowHUD")
                    : null;
            if (runtimeObject == null ||
                chapterRuntime == null ||
                entryPoint == null ||
                presenter == null)
            {
                throw new InvalidOperationException(
                    "Chapter runtime, session entry point, or HUD is missing.");
            }

            RunChapterCombatProgressAdapter chapterAdapter =
                runtimeObject.GetComponent<RunChapterCombatProgressAdapter>();
            if (chapterAdapter == null)
            {
                chapterAdapter =
                    Undo.AddComponent<RunChapterCombatProgressAdapter>(
                        runtimeObject);
            }

            Undo.RecordObject(chapterAdapter, "Configure Chapter Progress");
            chapterAdapter.ConfigureForEditor(chapterRuntime, entryPoint);
            chapterAdapter.enabled = true;
            EditorUtility.SetDirty(chapterAdapter);

            DisableLegacyComponent<ExplorationCombatExecutionAdapter>(
                runtimeObject);
            DisableLegacyComponent<RunFlowDebugOverlay>(runtimeObject);
            DisableLegacyComponent<RunPortalSpawner>(runtimeObject);
            DisableLegacyComponent<AssaultArenaTransitionController>(
                runtimeObject);
            DisableLegacyComponent<AssaultWaveSpawner>(runtimeObject);
            DisableLegacyComponent<AssaultReturnPortalSpawner>(runtimeObject);
            DisableLegacyComponent<AssaultRewardChestSpawner>(runtimeObject);

            RectTransform hudRect = presenter.GetComponent<RectTransform>();
            if (hudRect == null)
            {
                throw new InvalidOperationException(
                    "Chapter HUD root has no RectTransform.");
            }

            Undo.RecordObject(hudRect, "Move Chapter HUD To Run Meter Slot");
            hudRect.anchorMin = new Vector2(0f, 1f);
            hudRect.anchorMax = new Vector2(0f, 1f);
            hudRect.pivot = new Vector2(0f, 1f);
            hudRect.anchoredPosition = CutoverHudPosition;
            EditorUtility.SetDirty(hudRect);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException($"Could not save {ScenePath}.");
        }

        private static void DisableLegacyComponent<T>(GameObject runtimeObject)
            where T : Behaviour
        {
            T component = runtimeObject.GetComponent<T>();
            if (component == null)
            {
                throw new InvalidOperationException(
                    $"Legacy component {typeof(T).Name} is missing.");
            }

            Undo.RecordObject(component, "Disable Legacy Run Flow Component");
            component.enabled = false;
            EditorUtility.SetDirty(component);
        }

        private static RunChapterBossPortalInteractable
            ValidatePortalPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PortalPrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    $"Portal prefab not found: {PortalPrefabPath}");
            }

            RunChapterBossPortalInteractable interactable =
                prefab.GetComponent<RunChapterBossPortalInteractable>();
            CapsuleCollider collider = prefab.GetComponent<CapsuleCollider>();
            TargetVisual visual = prefab.GetComponent<TargetVisual>();
            if (interactable == null || collider == null || visual == null ||
                !collider.isTrigger)
            {
                throw new InvalidOperationException(
                    "Chapter portal prefab wiring is incomplete.");
            }

            foreach (Transform child in
                     prefab.GetComponentsInChildren<Transform>(true))
            {
                if (child.gameObject.layer != InteractableLayer)
                {
                    throw new InvalidOperationException(
                        "Chapter portal uses an invalid selection layer.");
                }
            }

            Renderer[] renderers =
                prefab.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(
                    "Chapter portal has no renderers.");
            }

            return interactable;
        }

        private static RunChapterFlowHudPresenter ValidateHudPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                HudPrefabPath);
            RunChapterFlowHudPresenter presenter =
                prefab != null
                    ? prefab.GetComponent<RunChapterFlowHudPresenter>()
                    : null;
            RunChapterFlowHudView view =
                prefab != null
                    ? prefab.GetComponent<RunChapterFlowHudView>()
                    : null;
            if (presenter == null || view == null ||
                presenter.View != view ||
                !view.IsConfigured)
            {
                throw new InvalidOperationException(
                    "Chapter HUD prefab wiring is incomplete.");
            }

            return presenter;
        }

        private static void ValidateInternal(
            Scene scene,
            RunChapterBossPortalInteractable portalPrefab,
            RunChapterFlowHudPresenter hudPrefab)
        {
            GameObject runtimeObject = FindRootObject(
                scene,
                RuntimeObjectName);
            GameObject canvasObject = FindRootObject(scene, CanvasObjectName);
            RunChapterFlowRuntime runtime =
                runtimeObject != null
                    ? runtimeObject.GetComponent<RunChapterFlowRuntime>()
                    : null;
            RunChapterBossPortalSpawner spawner =
                runtimeObject != null
                    ? runtimeObject.GetComponent<
                        RunChapterBossPortalSpawner>()
                    : null;
            RunSceneSessionEntryPoint entryPoint =
                FindSceneComponent<RunSceneSessionEntryPoint>(scene);
            if (runtime == null ||
                runtime.SessionEntryPoint != entryPoint ||
                spawner == null ||
                spawner.ChapterFlowRuntime != runtime ||
                spawner.PortalPrefab != portalPrefab ||
                spawner.LocalPlayer == null)
            {
                throw new InvalidOperationException(
                    "Chapter portal scene wiring is incomplete.");
            }

            RunChapterFlowHudPresenter presenter =
                canvasObject != null
                    ? FindChildComponentByName<
                        RunChapterFlowHudPresenter>(
                        canvasObject.transform,
                        "RunChapterFlowHUD")
                    : null;
            if (presenter == null ||
                presenter.ChapterFlowRuntime != runtime ||
                presenter.View == null ||
                !presenter.View.IsConfigured ||
                PrefabUtility.GetCorrespondingObjectFromSource(presenter) !=
                    hudPrefab)
            {
                throw new InvalidOperationException(
                    "Chapter HUD scene wiring is incomplete.");
            }

            if (runtimeObject.GetComponent<RunFlowRuntime>() == null ||
                runtimeObject.GetComponent<RunPortalSpawner>() == null)
            {
                throw new InvalidOperationException(
                    "Legacy run flow or portal wiring was removed.");
            }
        }

        private static void ValidateCutoverInternal(Scene scene)
        {
            GameObject runtimeObject = FindRootObject(
                scene,
                RuntimeObjectName);
            GameObject canvasObject = FindRootObject(scene, CanvasObjectName);
            RunChapterFlowRuntime chapterRuntime =
                runtimeObject != null
                    ? runtimeObject.GetComponent<RunChapterFlowRuntime>()
                    : null;
            RunSceneSessionEntryPoint entryPoint =
                FindSceneComponent<RunSceneSessionEntryPoint>(scene);
            RunChapterCombatProgressAdapter chapterAdapter =
                runtimeObject != null
                    ? runtimeObject.GetComponent<
                        RunChapterCombatProgressAdapter>()
                    : null;
            if (chapterRuntime == null ||
                !chapterRuntime.enabled ||
                chapterAdapter == null ||
                !chapterAdapter.enabled ||
                chapterAdapter.ChapterFlowRuntime != chapterRuntime ||
                chapterAdapter.SessionEntryPoint != entryPoint)
            {
                throw new InvalidOperationException(
                    "Chapter combat progress wiring is incomplete.");
            }

            ValidateLegacyComponentDisabled<
                ExplorationCombatExecutionAdapter>(runtimeObject);
            ValidateLegacyComponentDisabled<RunFlowDebugOverlay>(runtimeObject);
            ValidateLegacyComponentDisabled<RunPortalSpawner>(runtimeObject);
            ValidateLegacyComponentDisabled<
                AssaultArenaTransitionController>(runtimeObject);
            ValidateLegacyComponentDisabled<AssaultWaveSpawner>(runtimeObject);
            ValidateLegacyComponentDisabled<
                AssaultReturnPortalSpawner>(runtimeObject);
            ValidateLegacyComponentDisabled<
                AssaultRewardChestSpawner>(runtimeObject);

            RunFlowRuntime legacyRuntime =
                runtimeObject.GetComponent<RunFlowRuntime>();
            LocalAssaultArenaGateway arenaGateway =
                runtimeObject.GetComponent<LocalAssaultArenaGateway>();
            AssaultTargetRegistry targetRegistry =
                runtimeObject.GetComponent<AssaultTargetRegistry>();
            if (legacyRuntime == null ||
                !legacyRuntime.enabled ||
                arenaGateway == null ||
                !arenaGateway.enabled ||
                targetRegistry == null ||
                !targetRegistry.enabled)
            {
                throw new InvalidOperationException(
                    "Required shared legacy infrastructure was disabled or removed.");
            }

            RunChapterFlowHudPresenter presenter =
                canvasObject != null
                    ? FindChildComponentByName<RunChapterFlowHudPresenter>(
                        canvasObject.transform,
                        "RunChapterFlowHUD")
                    : null;
            RectTransform hudRect =
                presenter != null
                    ? presenter.GetComponent<RectTransform>()
                    : null;
            if (presenter == null ||
                !presenter.gameObject.activeSelf ||
                hudRect == null ||
                hudRect.anchorMin != new Vector2(0f, 1f) ||
                hudRect.anchorMax != new Vector2(0f, 1f) ||
                hudRect.pivot != new Vector2(0f, 1f) ||
                hudRect.anchoredPosition != CutoverHudPosition)
            {
                throw new InvalidOperationException(
                    "Chapter HUD is not in the legacy run-meter slot.");
            }
        }

        private static void ValidateLegacyComponentDisabled<T>(
            GameObject runtimeObject)
            where T : Behaviour
        {
            T component =
                runtimeObject != null ? runtimeObject.GetComponent<T>() : null;
            if (component == null || component.enabled)
            {
                throw new InvalidOperationException(
                    $"Legacy component {typeof(T).Name} must exist disabled.");
            }
        }

        private static Scene RequireCleanSampleScene()
        {
            Scene scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    $"Open {ScenePath} before installation.");
            }

            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "SampleScene has unrelated unsaved changes.");
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

        private static void ValidateInteractableLayer()
        {
            if (LayerMask.LayerToName(InteractableLayer) !=
                InteractableLayerName)
            {
                throw new InvalidOperationException(
                    $"Layer {InteractableLayer} must be named " +
                    $"{InteractableLayerName}.");
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

        private static T FindSceneComponent<T>(Scene scene)
            where T : Component
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                T component = roots[i].GetComponentInChildren<T>(true);
                if (component != null)
                    return component;
            }

            return null;
        }

        private static T FindChildComponentByName<T>(
            Transform parent,
            string objectName)
            where T : Component
        {
            T[] components = parent.GetComponentsInChildren<T>(true);
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i].gameObject.name == objectName)
                    return components[i];
            }

            return null;
        }

        private static void SetLayerRecursively(
            GameObject gameObject,
            int layer)
        {
            gameObject.layer = layer;
            foreach (Transform child in gameObject.transform)
                SetLayerRecursively(child.gameObject, layer);
        }
    }
}
