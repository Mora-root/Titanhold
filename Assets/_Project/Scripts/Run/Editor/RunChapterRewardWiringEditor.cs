using System;
using System.Collections.Generic;
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
    public static class RunChapterRewardWiringEditor
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/SampleScene.unity";
        private const string RuntimeObjectName = "RunFlowRuntime";
        private const string CanvasObjectName = "Canvas";
        private const string UiObjectName = "RunChapterCompletionUI";
        private const string ExitPortalPrefabPath =
            "Assets/_Project/Prefabs/Run/RunChapterExitPortal.prefab";
        private const string BossPortalPrefabPath =
            "Assets/_Project/Prefabs/Run/RunChapterBossPortal.prefab";
        private const string CompletionUiPrefabPath =
            "Assets/_Project/Prefabs/UI/RunChapterCompletionUI.prefab";
        private const string RewardDefinitionPath =
            "Assets/_Project/ScriptableObjects/Run/" +
            "RunChapterReward_Prototype.asset";
        private const string BossLootPath =
            "Assets/_Project/ScriptableObjects/EnemyLootTable/" +
            "ChapterBoss_Prototype.asset";
        private const string AssaultRewardLootPath =
            "Assets/_Project/ScriptableObjects/Run/" +
            "AssaultReward_Prototype.asset";
        private const string BaseBossPrefabPath =
            "Assets/_Project/Prefabs/Enemy/Skelet_Boss_Prototype.prefab";
        private const string ChapterBossPrefabPath =
            "Assets/_Project/Prefabs/Enemy/" +
            "Skelet_ChapterBoss_Prototype.prefab";
        private const string ExplorationSkeletonPrefabPath =
            "Assets/_Project/Prefabs/Enemy/Skelet.prefab";

        [MenuItem(
            "Tools/Titanhold/Install Run Chapter Reward Wiring")]
        public static void Install()
        {
            try
            {
                RequireEditMode("installation");
                Scene scene = RequireSampleScene(requireClean: true);
                RunChapterCompletionRewardDefinition reward =
                    CreateOrUpdateRewardDefinition();
                LootTable bossLoot = CreateOrValidateBossLoot();
                GameObject chapterBoss = CreateOrValidateChapterBoss(
                    bossLoot);
                RunChapterExitPortalInteractable exitPortal =
                    CreateOrValidateExitPortal();
                RunChapterCompletionController completionUi =
                    CreateOrValidateCompletionUi();

                ConfigureScene(
                    scene,
                    reward,
                    chapterBoss,
                    exitPortal,
                    completionUi);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                ValidateInternal(scene);
                Debug.Log("Run Chapter Reward wiring installed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "Run Chapter Reward wiring installation failed: " +
                    exception);
            }
        }

        [MenuItem(
            "Tools/Titanhold/Validate Run Chapter Reward Wiring")]
        public static void Validate()
        {
            try
            {
                RequireEditMode("validation");
                Scene scene = RequireSampleScene(requireClean: false);
                ValidateInternal(scene);
                Debug.Log(
                    "Run Chapter Reward wiring validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "Run Chapter Reward wiring validation failed: " +
                    exception);
            }
        }

        private static RunChapterCompletionRewardDefinition
            CreateOrUpdateRewardDefinition()
        {
            RunChapterCompletionRewardDefinition definition =
                AssetDatabase.LoadAssetAtPath<
                    RunChapterCompletionRewardDefinition>(
                    RewardDefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<
                    RunChapterCompletionRewardDefinition>();
                AssetDatabase.CreateAsset(definition, RewardDefinitionPath);
            }

            definition.ConfigureForEditor(
                "reward:chapter:1:prototype",
                configuredChapterNumber: 1,
                configuredCharacterExperience: 1200,
                configuredCrystals: 60);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static LootTable CreateOrValidateBossLoot()
        {
            LootTable loot = AssetDatabase.LoadAssetAtPath<LootTable>(
                BossLootPath);
            if (loot == null)
            {
                if (!AssetDatabase.CopyAsset(
                        AssaultRewardLootPath,
                        BossLootPath))
                {
                    throw new InvalidOperationException(
                        "Could not create the chapter boss loot table.");
                }

                AssetDatabase.ImportAsset(
                    BossLootPath,
                    ImportAssetOptions.ForceSynchronousImport);
                loot = AssetDatabase.LoadAssetAtPath<LootTable>(
                    BossLootPath);
                if (loot == null)
                    throw new InvalidOperationException(
                        "Chapter boss loot table was not imported.");

                loot.name = "ChapterBoss_Prototype";
                EditorUtility.SetDirty(loot);
            }

            LootTable source = AssetDatabase.LoadAssetAtPath<LootTable>(
                AssaultRewardLootPath);
            if (source == null || loot.Entries.Count != source.Entries.Count)
            {
                throw new InvalidOperationException(
                    "Chapter boss loot does not match its prototype source.");
            }

            return loot;
        }

        private static GameObject CreateOrValidateChapterBoss(
            LootTable bossLoot)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(
                ChapterBossPrefabPath);
            if (existing != null)
            {
                NormalizeChapterBossVariant();
                existing = AssetDatabase.LoadAssetAtPath<GameObject>(
                    ChapterBossPrefabPath);
                ValidateChapterBoss(existing, bossLoot);
                return existing;
            }

            GameObject baseBoss = AssetDatabase.LoadAssetAtPath<GameObject>(
                BaseBossPrefabPath);
            GameObject skeleton = AssetDatabase.LoadAssetAtPath<GameObject>(
                ExplorationSkeletonPrefabPath);
            EnemyLootTableDropper sourceDropper = skeleton != null
                ? skeleton.GetComponentInChildren<EnemyLootTableDropper>(true)
                : null;
            if (baseBoss == null || sourceDropper == null)
            {
                throw new InvalidOperationException(
                    "Boss base prefab or generic loot wiring is missing.");
            }

            GameObject instance =
                PrefabUtility.InstantiatePrefab(baseBoss) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException(
                    "Could not instantiate the base boss prefab.");
            }

            try
            {
                instance.name = "Skelet_ChapterBoss_Prototype";
                EnemyLootTableDropper dropper =
                    instance.AddComponent<EnemyLootTableDropper>();
                CopyDropperReferences(sourceDropper, dropper, bossLoot);
                PrefabUtility.RevertObjectOverride(
                    instance.transform,
                    InteractionMode.AutomatedAction);
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(
                    instance,
                    ChapterBossPrefabPath);
                if (saved == null)
                {
                    throw new InvalidOperationException(
                        "Could not save the chapter boss prefab variant.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }

            AssetDatabase.ImportAsset(
                ChapterBossPrefabPath,
                ImportAssetOptions.ForceSynchronousImport);
            GameObject result = AssetDatabase.LoadAssetAtPath<GameObject>(
                ChapterBossPrefabPath);
            ValidateChapterBoss(result, bossLoot);
            return result;
        }

        private static void NormalizeChapterBossVariant()
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(
                ChapterBossPrefabPath);
            try
            {
                PrefabUtility.RevertObjectOverride(
                    contents.transform,
                    InteractionMode.AutomatedAction);
                PrefabUtility.SaveAsPrefabAsset(
                    contents,
                    ChapterBossPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void CopyDropperReferences(
            EnemyLootTableDropper source,
            EnemyLootTableDropper target,
            LootTable loot)
        {
            SerializedObject sourceObject = new(source);
            SerializedObject targetObject = new(target);
            string[] copiedProperties =
            {
                "itemPickupPrefab",
                "goldPickupPrefab",
                "dropRadius",
                "dropSpawnHeight",
                "snapToGround",
                "groundMask",
                "groundProbeHeight",
                "groundProbeDistance",
                "landingYOffset"
            };
            for (int i = 0; i < copiedProperties.Length; i++)
            {
                SerializedProperty sourceProperty =
                    sourceObject.FindProperty(copiedProperties[i]);
                targetObject.CopyFromSerializedProperty(sourceProperty);
            }

            targetObject.FindProperty("lootTable").objectReferenceValue = loot;
            targetObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static RunChapterExitPortalInteractable
            CreateOrValidateExitPortal()
        {
            RunChapterExitPortalInteractable existing =
                AssetDatabase.LoadAssetAtPath<
                    RunChapterExitPortalInteractable>(
                    ExitPortalPrefabPath);
            if (existing != null)
                return ValidateExitPortal();

            GameObject contents = PrefabUtility.LoadPrefabContents(
                BossPortalPrefabPath);
            try
            {
                contents.name = "RunChapterExitPortal";
                RunChapterBossPortalInteractable oldInteractable =
                    contents.GetComponent<
                        RunChapterBossPortalInteractable>();
                if (oldInteractable != null)
                {
                    UnityEngine.Object.DestroyImmediate(oldInteractable);
                }

                RunChapterExitPortalInteractable interactable =
                    contents.AddComponent<
                        RunChapterExitPortalInteractable>();
                SerializedObject serialized = new(interactable);
                serialized.FindProperty("interactionRange").floatValue = 2f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(
                    contents,
                    ExitPortalPrefabPath);
                if (saved == null)
                {
                    throw new InvalidOperationException(
                        "Could not save the chapter exit portal prefab.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            AssetDatabase.ImportAsset(
                ExitPortalPrefabPath,
                ImportAssetOptions.ForceSynchronousImport);
            return ValidateExitPortal();
        }

        private static RunChapterExitPortalInteractable ValidateExitPortal()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                ExitPortalPrefabPath);
            RunChapterExitPortalInteractable interactable = prefab != null
                ? prefab.GetComponent<RunChapterExitPortalInteractable>()
                : null;
            if (interactable == null ||
                prefab.GetComponent<RunChapterBossPortalInteractable>() !=
                    null ||
                prefab.GetComponent<TargetVisual>() == null ||
                prefab.GetComponent<Collider>() == null)
            {
                throw new InvalidOperationException(
                    "Chapter exit portal prefab wiring is invalid.");
            }

            return interactable;
        }

        private static RunChapterCompletionController
            CreateOrValidateCompletionUi()
        {
            RunChapterCompletionController existing =
                AssetDatabase.LoadAssetAtPath<
                    RunChapterCompletionController>(
                    CompletionUiPrefabPath);
            if (existing != null)
                return ValidateCompletionUiPrefab();

            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
            if (font == null)
            {
                throw new InvalidOperationException(
                    "The default TMP font asset is missing.");
            }

            GameObject root = new(UiObjectName, typeof(RectTransform));
            try
            {
                root.layer = 5;
                Stretch(root.GetComponent<RectTransform>());
                RunChapterCompletionView view =
                    root.AddComponent<RunChapterCompletionView>();
                RunChapterCompletionController controller =
                    root.AddComponent<RunChapterCompletionController>();

                GameObject confirmation = CreateModalPanel(
                    root.transform,
                    "ConfirmationPanel");
                CreateText(
                    confirmation.transform,
                    "Title",
                    font,
                    "Завершить забег?",
                    34f,
                    new Vector2(0f, 118f),
                    new Vector2(620f, 55f));
                CreateText(
                    confirmation.transform,
                    "Message",
                    font,
                    "После подтверждения вернуться за оставшимся " +
                    "лутом будет нельзя.",
                    22f,
                    new Vector2(0f, 42f),
                    new Vector2(680f, 80f));
                Button cancel = CreateButton(
                    confirmation.transform,
                    "CancelButton",
                    font,
                    "Продолжить сбор",
                    new Vector2(-155f, -80f));
                Button confirm = CreateButton(
                    confirmation.transform,
                    "ConfirmButton",
                    font,
                    "Завершить",
                    new Vector2(155f, -80f));

                GameObject completed = CreateModalPanel(
                    root.transform,
                    "CompletedPanel");
                CreateText(
                    completed.transform,
                    "Title",
                    font,
                    "Забег завершён",
                    36f,
                    new Vector2(0f, 125f),
                    new Vector2(620f, 60f));
                TextMeshProUGUI rewardText = CreateText(
                    completed.transform,
                    "RewardText",
                    font,
                    string.Empty,
                    25f,
                    new Vector2(0f, 34f),
                    new Vector2(620f, 100f));
                Button returnButton = CreateButton(
                    completed.transform,
                    "ReturnToHubButton",
                    font,
                    "Вернуться в хаб",
                    new Vector2(0f, -92f));

                TextMeshProUGUI statusText = CreateText(
                    root.transform,
                    "StatusText",
                    font,
                    string.Empty,
                    18f,
                    new Vector2(0f, -235f),
                    new Vector2(760f, 50f));
                statusText.color = new Color(1f, 0.55f, 0.45f, 1f);

                view.ConfigureForEditor(
                    confirmation,
                    completed,
                    cancel,
                    confirm,
                    returnButton,
                    rewardText,
                    statusText);
                confirmation.SetActive(false);
                completed.SetActive(false);
                EditorUtility.SetDirty(view);
                EditorUtility.SetDirty(controller);

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(
                    root,
                    CompletionUiPrefabPath);
                if (saved == null)
                {
                    throw new InvalidOperationException(
                        "Could not save the chapter completion UI prefab.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            AssetDatabase.ImportAsset(
                CompletionUiPrefabPath,
                ImportAssetOptions.ForceSynchronousImport);
            return ValidateCompletionUiPrefab();
        }

        private static GameObject CreateModalPanel(
            Transform parent,
            string name)
        {
            GameObject panel = new(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            panel.layer = 5;
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Stretch(rect);
            Image image = panel.GetComponent<Image>();
            image.color = new Color(0.025f, 0.03f, 0.05f, 0.94f);
            return panel;
        }

        private static TextMeshProUGUI CreateText(
            Transform parent,
            string name,
            TMP_FontAsset font,
            string content,
            float fontSize,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            GameObject textObject = new(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.layer = 5;
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            TextMeshProUGUI text =
                textObject.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            text.text = content;
            return text;
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            TMP_FontAsset font,
            string label,
            Vector2 anchoredPosition)
        {
            GameObject buttonObject = new(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            buttonObject.layer = 5;
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(270f, 58f);
            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.18f, 0.32f, 0.52f, 1f);
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            CreateText(
                buttonObject.transform,
                "Label",
                font,
                label,
                20f,
                Vector2.zero,
                rect.sizeDelta);
            return button;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static RunChapterCompletionController
            ValidateCompletionUiPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                CompletionUiPrefabPath);
            RunChapterCompletionView view = prefab != null
                ? prefab.GetComponent<RunChapterCompletionView>()
                : null;
            RunChapterCompletionController controller = prefab != null
                ? prefab.GetComponent<RunChapterCompletionController>()
                : null;
            if (view == null || controller == null || !view.IsConfigured)
            {
                throw new InvalidOperationException(
                    "Chapter completion UI prefab wiring is invalid.");
            }

            return controller;
        }

        private static void ConfigureScene(
            Scene scene,
            RunChapterCompletionRewardDefinition reward,
            GameObject chapterBoss,
            RunChapterExitPortalInteractable exitPortal,
            RunChapterCompletionController completionUiPrefab)
        {
            GameObject runtimeObject = RequireRootObject(
                scene,
                RuntimeObjectName);
            GameObject canvasObject = RequireRootObject(
                scene,
                CanvasObjectName);
            RunChapterFlowRuntime chapterFlow =
                RequireComponent<RunChapterFlowRuntime>(runtimeObject);
            RunSceneSessionEntryPoint sessionEntry =
                FindSceneComponent<RunSceneSessionEntryPoint>(scene);
            PlayerBrain player = FindSceneComponent<PlayerBrain>(scene);
            PlayerInput[] inputs = FindSceneComponents<PlayerInput>(scene);
            if (sessionEntry == null || player == null || inputs.Length == 0)
            {
                throw new InvalidOperationException(
                    "Session entry, player, or player input is missing.");
            }

            RunChapterCompletionRuntime completionRuntime =
                runtimeObject.GetComponent<RunChapterCompletionRuntime>();
            if (completionRuntime == null)
            {
                completionRuntime = Undo.AddComponent<
                    RunChapterCompletionRuntime>(runtimeObject);
            }

            completionRuntime.ConfigureForEditor(
                chapterFlow,
                sessionEntry,
                reward);
            EditorUtility.SetDirty(completionRuntime);

            RunChapterExitPortalSpawner spawner =
                runtimeObject.GetComponent<RunChapterExitPortalSpawner>();
            if (spawner == null)
            {
                spawner = Undo.AddComponent<
                    RunChapterExitPortalSpawner>(runtimeObject);
            }

            spawner.ConfigureForEditor(
                chapterFlow,
                exitPortal,
                player.transform);
            EditorUtility.SetDirty(spawner);

            RunChapterCompletionController controller =
                FindChildComponentByName<
                    RunChapterCompletionController>(
                    canvasObject.transform,
                    UiObjectName);
            if (controller == null)
            {
                GameObject instance = PrefabUtility.InstantiatePrefab(
                    completionUiPrefab.gameObject,
                    canvasObject.transform) as GameObject;
                if (instance == null)
                {
                    throw new InvalidOperationException(
                        "Could not instantiate chapter completion UI.");
                }

                Undo.RegisterCreatedObjectUndo(
                    instance,
                    "Install Run Chapter Completion UI");
                controller = instance.GetComponent<
                    RunChapterCompletionController>();
            }

            PrefabUtility.RevertObjectOverride(
                controller.transform,
                InteractionMode.AutomatedAction);

            RunChapterCompletionView view =
                controller.GetComponent<RunChapterCompletionView>();
            controller.ConfigureForEditor(
                completionRuntime,
                spawner,
                view,
                inputs,
                "HubScene");
            EditorUtility.SetDirty(controller);

            RunPauseController pauseController =
                FindSceneComponent<RunPauseController>(scene);
            if (pauseController != null)
                AppendEscapePriorityWindow(pauseController, controller);

            RunChapterBossEncounterCoordinator bossCoordinator =
                RequireComponent<RunChapterBossEncounterCoordinator>(
                    runtimeObject);
            bossCoordinator.ConfigureForEditor(
                bossCoordinator.ChapterFlowRuntime,
                bossCoordinator.ArenaGatewaySource,
                bossCoordinator.TargetRegistry,
                bossCoordinator.EnemyDefinitions,
                chapterBoss,
                bossCoordinator.BossSpawnPoint,
                bossCoordinator.LocalPlayer);
            EditorUtility.SetDirty(bossCoordinator);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException(
                    $"Could not save {ScenePath}.");
        }

        private static void AppendEscapePriorityWindow(
            RunPauseController pauseController,
            RunChapterCompletionController completionController)
        {
            SerializedObject serialized = new(pauseController);
            SerializedProperty windows =
                serialized.FindProperty("escapePriorityWindows");
            for (int i = 0; i < windows.arraySize; i++)
            {
                if (windows.GetArrayElementAtIndex(i)
                        .objectReferenceValue == completionController)
                {
                    return;
                }
            }

            int index = windows.arraySize;
            windows.InsertArrayElementAtIndex(index);
            windows.GetArrayElementAtIndex(index).objectReferenceValue =
                completionController;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pauseController);
        }

        private static void ValidateInternal(Scene scene)
        {
            RunChapterCompletionRewardDefinition rewardDefinition =
                AssetDatabase.LoadAssetAtPath<
                    RunChapterCompletionRewardDefinition>(
                    RewardDefinitionPath);
            if (rewardDefinition == null ||
                !rewardDefinition.TryCreateReward(
                    out RunChapterCompletionReward reward,
                    out _) ||
                reward.ChapterNumber != 1 ||
                reward.CharacterExperience != 1200 ||
                reward.Crystals != 60)
            {
                throw new InvalidOperationException(
                    "Chapter completion reward definition is invalid.");
            }

            LootTable bossLoot = AssetDatabase.LoadAssetAtPath<LootTable>(
                BossLootPath);
            GameObject chapterBoss = AssetDatabase.LoadAssetAtPath<GameObject>(
                ChapterBossPrefabPath);
            ValidateChapterBoss(chapterBoss, bossLoot);
            ValidateExitPortal();
            RunChapterCompletionController prefab =
                ValidateCompletionUiPrefab();

            GameObject runtimeObject = RequireRootObject(
                scene,
                RuntimeObjectName);
            GameObject canvasObject = RequireRootObject(
                scene,
                CanvasObjectName);
            RunChapterFlowRuntime flow =
                RequireComponent<RunChapterFlowRuntime>(runtimeObject);
            RunChapterCompletionRuntime completionRuntime =
                RequireComponent<RunChapterCompletionRuntime>(runtimeObject);
            RunChapterExitPortalSpawner spawner =
                RequireComponent<RunChapterExitPortalSpawner>(runtimeObject);
            RunChapterBossEncounterCoordinator bossCoordinator =
                RequireComponent<RunChapterBossEncounterCoordinator>(
                    runtimeObject);
            RunChapterCompletionController controller =
                FindChildComponentByName<
                    RunChapterCompletionController>(
                    canvasObject.transform,
                    UiObjectName);
            if (!completionRuntime.HasRequiredReferences ||
                completionRuntime.ChapterFlowRuntime != flow ||
                completionRuntime.RewardDefinition != rewardDefinition ||
                spawner.ChapterFlowRuntime != flow ||
                AssetDatabase.GetAssetPath(spawner.PortalPrefab) !=
                    ExitPortalPrefabPath ||
                spawner.LocalPlayer == null ||
                controller == null ||
                !controller.HasRequiredReferences ||
                controller.CompletionRuntime != completionRuntime ||
                controller.ExitPortalSpawner != spawner ||
                controller.HubSceneName != "HubScene" ||
                PrefabUtility.GetCorrespondingObjectFromSource(controller) !=
                    prefab ||
                AssetDatabase.GetAssetPath(bossCoordinator.BossPrefab) !=
                    ChapterBossPrefabPath)
            {
                throw new InvalidOperationException(
                    "Chapter reward scene wiring is incomplete.");
            }

            if (runtimeObject.GetComponent<RunFlowRuntime>() == null ||
                AssetDatabase.LoadAssetAtPath<GameObject>(BaseBossPrefabPath)
                    .GetComponentInChildren<EnemyLootTableDropper>(true) !=
                    null)
            {
                throw new InvalidOperationException(
                    "Legacy run flow or legacy boss boundary was changed.");
            }
        }

        private static void ValidateChapterBoss(
            GameObject boss,
            LootTable expectedLoot)
        {
            EnemyLootTableDropper dropper = boss != null
                ? boss.GetComponentInChildren<EnemyLootTableDropper>(true)
                : null;
            if (boss == null ||
                expectedLoot == null ||
                dropper == null ||
                PrefabUtility.GetPrefabAssetType(boss) !=
                    PrefabAssetType.Variant)
            {
                throw new InvalidOperationException(
                    "Chapter boss prefab variant is invalid.");
            }

            SerializedObject serialized = new(dropper);
            if (serialized.FindProperty("lootTable").objectReferenceValue !=
                    expectedLoot ||
                serialized.FindProperty("itemPickupPrefab")
                    .objectReferenceValue == null ||
                serialized.FindProperty("goldPickupPrefab")
                    .objectReferenceValue == null)
            {
                throw new InvalidOperationException(
                    "Chapter boss loot dropper wiring is invalid.");
            }
        }

        private static Scene RequireSampleScene(bool requireClean)
        {
            Scene scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                throw new InvalidOperationException(
                    $"Open {ScenePath} before continuing.");
            if (requireClean && scene.isDirty)
            {
                throw new InvalidOperationException(
                    "SampleScene has unrelated unsaved changes.");
            }

            return scene;
        }

        private static GameObject RequireRootObject(
            Scene scene,
            string name)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == name)
                    return roots[i];
            }

            throw new InvalidOperationException(
                $"Scene object '{name}' is missing.");
        }

        private static T RequireComponent<T>(GameObject owner)
            where T : Component
        {
            T component = owner.GetComponent<T>();
            if (component == null)
            {
                throw new InvalidOperationException(
                    $"'{owner.name}' is missing {typeof(T).Name}.");
            }

            return component;
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

        private static T[] FindSceneComponents<T>(Scene scene)
            where T : Component
        {
            List<T> results = new();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                results.AddRange(
                    roots[i].GetComponentsInChildren<T>(true));
            }

            return results.ToArray();
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

        private static void RequireEditMode(string operation)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    $"Exit Play Mode before {operation}.");
            }
        }
    }
}
