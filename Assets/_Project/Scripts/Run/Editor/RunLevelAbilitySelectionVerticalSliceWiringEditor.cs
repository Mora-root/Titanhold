using System;
using Titanhold.Combat.Abilities;
using Titanhold.Session;
using Titanhold.UI.Hub;
using Titanhold.UI.Run;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Titanhold.Run.Editor
{
    public static class RunLevelAbilitySelectionVerticalSliceWiringEditor
    {
        private const string HubScenePath =
            "Assets/_Project/Scenes/HubScene.unity";
        private const string RunScenePath =
            "Assets/_Project/Scenes/SampleScene.unity";
        private const string AbilityCatalogPath =
            "Assets/_Project/ScriptableObjects/Abilities/AbilityDefinitionCatalog.asset";
        private const string HeavyStrikePath =
            "Assets/_Project/ScriptableObjects/Abilities/HeavyStrike.asset";
        private const string CrushingStrikePath =
            "Assets/_Project/ScriptableObjects/Abilities/CrushingStrike.asset";
        private const string CleavePath =
            "Assets/_Project/ScriptableObjects/Abilities/Cleave.asset";
        private const string SchedulePath =
            "Assets/_Project/ScriptableObjects/Run/WarriorAbilityUnlockSchedule.asset";
        private const string CatalogPath =
            "Assets/_Project/ScriptableObjects/Run/AbilityUnlockScheduleCatalog.asset";
        private const string ScheduleId = "ability-schedule:warrior";
        private const string ArchetypeId = "archetype:warrior";
        private const string PlayerId = "player:local";

        private static readonly int[] AuthoredLevels = { 3, 7, 10, 15 };

        [MenuItem("Tools/Titanhold/Install Run Level Ability Selection Wiring")]
        public static void Install()
        {
            try
            {
                RequireEditMode("installation");
                RequireCleanOpenScene();
                AbilityDefinitionCatalog abilities =
                    RequireAsset<AbilityDefinitionCatalog>(AbilityCatalogPath);
                RunAbilityUnlockScheduleDefinition schedule =
                    CreateOrUpdateSchedule();
                RunAbilityUnlockScheduleCatalog catalog =
                    CreateOrUpdateCatalog(abilities, schedule);
                WireHub(catalog);
                WireRunScene();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                ValidateInternal(schedule, catalog, abilities);
                Debug.Log(
                    "Run Level Ability Selection vertical-slice wiring installed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "Run Level Ability Selection wiring installation failed: " +
                    exception);
            }
        }

        [MenuItem("Tools/Titanhold/Validate Run Level Ability Selection Wiring")]
        public static void Validate()
        {
            try
            {
                RequireEditMode("validation");
                AbilityDefinitionCatalog abilities =
                    RequireAsset<AbilityDefinitionCatalog>(AbilityCatalogPath);
                RunAbilityUnlockScheduleDefinition schedule =
                    RequireAsset<RunAbilityUnlockScheduleDefinition>(
                        SchedulePath);
                RunAbilityUnlockScheduleCatalog catalog =
                    RequireAsset<RunAbilityUnlockScheduleCatalog>(CatalogPath);
                ValidateInternal(schedule, catalog, abilities);
                Debug.Log(
                    "Run Level Ability Selection wiring validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "Run Level Ability Selection wiring validation failed: " +
                    exception);
            }
        }

        private static RunAbilityUnlockScheduleDefinition
            CreateOrUpdateSchedule()
        {
            RunAbilityUnlockScheduleDefinition schedule =
                AssetDatabase.LoadAssetAtPath<
                    RunAbilityUnlockScheduleDefinition>(SchedulePath);
            if (schedule == null)
            {
                schedule = ScriptableObject.CreateInstance<
                    RunAbilityUnlockScheduleDefinition>();
                AssetDatabase.CreateAsset(schedule, SchedulePath);
            }

            ScriptableObject[] starterAbilities =
            {
                RequireAsset<ScriptableObject>(HeavyStrikePath),
                RequireAsset<ScriptableObject>(CrushingStrikePath),
                RequireAsset<ScriptableObject>(CleavePath)
            };
            RunAbilityUnlockMilestoneDefinition[] milestones =
                new RunAbilityUnlockMilestoneDefinition[AuthoredLevels.Length];
            milestones[0] = CreateMilestone(
                AuthoredLevels[0],
                targetSlotIndex: 1,
                optionCount: 2,
                starterAbilities,
                isEnabled: true);
            for (int i = 1; i < milestones.Length; i++)
            {
                milestones[i] = CreateMilestone(
                    AuthoredLevels[i],
                    targetSlotIndex: i + 1,
                    optionCount: 3,
                    Array.Empty<ScriptableObject>(),
                    isEnabled: false);
            }

            schedule.ConfigureForEditor(
                ScheduleId,
                ArchetypeId,
                milestones);
            EditorUtility.SetDirty(schedule);
            return schedule;
        }

        private static RunAbilityUnlockMilestoneDefinition CreateMilestone(
            int level,
            int targetSlotIndex,
            int optionCount,
            ScriptableObject[] abilities,
            bool isEnabled)
        {
            RunAbilityUnlockMilestoneDefinition milestone = new();
            milestone.ConfigureForEditor(
                level,
                targetSlotIndex,
                optionCount,
                abilities,
                isEnabled);
            return milestone;
        }

        private static RunAbilityUnlockScheduleCatalog CreateOrUpdateCatalog(
            AbilityDefinitionCatalog abilities,
            RunAbilityUnlockScheduleDefinition schedule)
        {
            RunAbilityUnlockScheduleCatalog catalog =
                AssetDatabase.LoadAssetAtPath<
                    RunAbilityUnlockScheduleCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<
                    RunAbilityUnlockScheduleCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.ConfigureForEditor(abilities, new[] { schedule });
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static void WireHub(
            RunAbilityUnlockScheduleCatalog catalog)
        {
            Scene scene = EditorSceneManager.OpenScene(
                HubScenePath,
                OpenSceneMode.Single);
            GameSessionRuntimeHost host =
                UnityEngine.Object.FindAnyObjectByType<GameSessionRuntimeHost>(
                    FindObjectsInactive.Include);
            if (host == null)
                throw new InvalidOperationException("Hub session host is missing.");

            host.ConfigureAbilityUnlockSchedulesForEditor(catalog);
            EditorUtility.SetDirty(host);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Could not save Hub scene.");
        }

        private static void WireRunScene()
        {
            Scene scene = EditorSceneManager.OpenScene(
                RunScenePath,
                OpenSceneMode.Single);
            HubStartingAbilitySelectionView view = RequireSelectionView();
            Transform panel = view.SelectionRoot != null
                ? view.SelectionRoot.transform.Find("SelectionPanel")
                : null;
            TMP_Text title = panel != null
                ? panel.Find("Title")?.GetComponent<TMP_Text>()
                : null;
            TMP_Text subtitle = panel != null
                ? panel.Find("Subtitle")?.GetComponent<TMP_Text>()
                : null;
            if (title == null || subtitle == null)
            {
                throw new InvalidOperationException(
                    "Ability selection heading texts are missing.");
            }

            view.ConfigureHeadingsForEditor(title, subtitle);
            RunLevelAbilitySelectionController controller =
                view.GetComponent<RunLevelAbilitySelectionController>();
            if (controller == null)
            {
                controller = Undo.AddComponent<
                    RunLevelAbilitySelectionController>(view.gameObject);
            }

            controller.ConfigureForEditor(view, PlayerId);
            EditorUtility.SetDirty(view);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Could not save run scene.");
        }

        private static void ValidateInternal(
            RunAbilityUnlockScheduleDefinition schedule,
            RunAbilityUnlockScheduleCatalog catalog,
            AbilityDefinitionCatalog abilities)
        {
            ValidateAssets(schedule, catalog, abilities);
            ValidateHub(catalog);
            ValidateRunScene();
        }

        private static void ValidateAssets(
            RunAbilityUnlockScheduleDefinition schedule,
            RunAbilityUnlockScheduleCatalog catalog,
            AbilityDefinitionCatalog abilities)
        {
            if (schedule.ScheduleId != ScheduleId ||
                schedule.CharacterArchetypeId != ArchetypeId ||
                schedule.Milestones.Count != AuthoredLevels.Length)
            {
                throw new InvalidOperationException(
                    "Warrior ability schedule identity or milestone count is invalid.");
            }

            for (int i = 0; i < schedule.Milestones.Count; i++)
            {
                RunAbilityUnlockMilestoneDefinition milestone =
                    schedule.Milestones[i];
                if (milestone == null ||
                    milestone.UnlockLevel != AuthoredLevels[i] ||
                    milestone.TargetSlotIndex != i + 1 ||
                    milestone.IsEnabled != (i == 0))
                {
                    throw new InvalidOperationException(
                        $"Warrior milestone {i} is not configured correctly.");
                }
            }

            RunAbilityUnlockMilestoneDefinition first =
                schedule.Milestones[0];
            if (first.OptionCount != 2 ||
                first.AbilityDefinitions.Count != 3)
            {
                throw new InvalidOperationException(
                    "Run level three must offer two of the three starter abilities.");
            }

            if (catalog.AbilityCatalog != abilities ||
                !catalog.IsValid ||
                catalog.Definitions.Count != 1 ||
                catalog.Definitions[0] != schedule ||
                !catalog.TryResolve(
                    ArchetypeId,
                    out RunAbilityUnlockSchedule runtimeSchedule) ||
                runtimeSchedule.Milestones.Count != 1 ||
                runtimeSchedule.Milestones[0].UnlockLevel != 3)
            {
                throw new InvalidOperationException(
                    "Ability unlock schedule catalog is invalid.");
            }
        }

        private static void ValidateHub(
            RunAbilityUnlockScheduleCatalog expectedCatalog)
        {
            EditorSceneManager.OpenScene(
                HubScenePath,
                OpenSceneMode.Single);
            GameSessionRuntimeHost host =
                UnityEngine.Object.FindAnyObjectByType<GameSessionRuntimeHost>(
                    FindObjectsInactive.Include);
            if (host == null || host.AbilityUnlockSchedules != expectedCatalog)
            {
                throw new InvalidOperationException(
                    "Hub session host is not wired to the ability schedule catalog.");
            }
        }

        private static void ValidateRunScene()
        {
            EditorSceneManager.OpenScene(
                RunScenePath,
                OpenSceneMode.Single);
            HubStartingAbilitySelectionView view = RequireSelectionView();
            RunLevelAbilitySelectionController[] controllers =
                UnityEngine.Object.FindObjectsByType<
                    RunLevelAbilitySelectionController>(
                    FindObjectsInactive.Include);
            if (!view.HasRequiredReferences ||
                !view.HasHeadingReferences ||
                controllers.Length != 1 ||
                !controllers[0].HasRequiredReferences ||
                controllers[0].View != view ||
                controllers[0].PlayerId != PlayerId)
            {
                throw new InvalidOperationException(
                    "Run-level ability selection UI wiring is invalid.");
            }
        }

        private static HubStartingAbilitySelectionView RequireSelectionView()
        {
            HubStartingAbilitySelectionView[] views =
                UnityEngine.Object.FindObjectsByType<
                    HubStartingAbilitySelectionView>(
                    FindObjectsInactive.Include);
            if (views.Length != 1)
            {
                throw new InvalidOperationException(
                    "Expected exactly one shared ability selection view.");
            }

            return views[0];
        }

        private static T RequireAsset<T>(string path)
            where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new InvalidOperationException($"Asset is missing: {path}");

            return asset;
        }

        private static void RequireEditMode(string operation)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    $"Exit Play Mode before ability selection {operation}.");
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
