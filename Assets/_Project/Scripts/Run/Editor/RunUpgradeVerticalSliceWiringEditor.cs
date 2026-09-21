using System;
using System.Collections.Generic;
using Titanhold.Session;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Titanhold.Run.Editor
{
    public static class RunUpgradeVerticalSliceWiringEditor
    {
        private const string HubScenePath =
            "Assets/_Project/Scenes/HubScene.unity";
        private const string RunFolder =
            "Assets/_Project/ScriptableObjects/Run";
        private const string UpgradeFolder = RunFolder + "/Upgrades";
        private const string DefinitionCatalogPath =
            RunFolder + "/RunUpgradeDefinitionCatalog.asset";
        private const string SchedulePath =
            RunFolder + "/WarriorUpgradeUnlockSchedule.asset";
        private const string ScheduleCatalogPath =
            RunFolder + "/UpgradeUnlockScheduleCatalog.asset";
        private const string AbilityScheduleCatalogPath =
            RunFolder + "/AbilityUnlockScheduleCatalog.asset";
        private const string ArchetypeId = "archetype:warrior";

        private static readonly int[] UpgradeLevels =
        {
            2, 4, 5, 6, 8, 9, 11, 12, 13, 14, 16, 17, 18, 19, 20
        };

        private static readonly UpgradeConfig[] UpgradeConfigs =
        {
            new(
                "Damage",
                "run-upgrade:damage",
                "Damage",
                "Increases all damage by 10%.",
                StatType.Damage,
                StatModifierType.Increased,
                10f),
            new(
                "MaximumHealth",
                "run-upgrade:max-health",
                "Maximum Health",
                "Increases maximum health by 10% without healing.",
                StatType.MaxHealth,
                StatModifierType.Increased,
                10f),
            new(
                "Armor",
                "run-upgrade:armor",
                "Armor",
                "Increases armor by 10%.",
                StatType.Armor,
                StatModifierType.Increased,
                10f),
            new(
                "AttackSpeed",
                "run-upgrade:attack-speed",
                "Attack Speed",
                "Increases attack speed by 10%.",
                StatType.AttackSpeed,
                StatModifierType.Increased,
                10f),
            new(
                "MovementSpeed",
                "run-upgrade:movement-speed",
                "Movement Speed",
                "Increases movement speed by 5%.",
                StatType.MoveSpeed,
                StatModifierType.Increased,
                5f),
            new(
                "MaximumResource",
                "run-upgrade:max-resource",
                "Maximum Resource",
                "Increases maximum energy or mana by 10%.",
                StatType.MaxResource,
                StatModifierType.Increased,
                10f),
            new(
                "ResourceRegeneration",
                "run-upgrade:resource-regeneration",
                "Resource Regeneration",
                "Increases energy or mana regeneration by 20%.",
                StatType.ResourceRegen,
                StatModifierType.Increased,
                20f),
            new(
                "HealthRegeneration",
                "run-upgrade:health-regeneration",
                "Health Regeneration",
                "Increases health regeneration by 20%.",
                StatType.HPRegen,
                StatModifierType.Increased,
                20f)
        };

        [MenuItem("Tools/Titanhold/Install Run Upgrade Wiring")]
        public static void Install()
        {
            try
            {
                RequireEditMode("installation");
                RequireCleanOpenScene();
                EnsureFolder(UpgradeFolder);
                RunStatUpgradeDefinition[] upgrades =
                    CreateOrUpdateUpgrades();
                RunUpgradeDefinitionCatalog definitions =
                    CreateOrUpdateDefinitionCatalog(upgrades);
                RunUpgradeUnlockScheduleDefinition schedule =
                    CreateOrUpdateSchedule(upgrades);
                RunUpgradeUnlockScheduleCatalog schedules =
                    CreateOrUpdateScheduleCatalog(definitions, schedule);
                WireHub(definitions, schedules);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                ValidateInternal(
                    definitions,
                    schedule,
                    schedules,
                    upgrades);
                Debug.Log("Run Upgrade vertical-slice wiring installed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Run Upgrade wiring installation failed: {exception}");
            }
        }

        [MenuItem("Tools/Titanhold/Validate Run Upgrade Wiring")]
        public static void Validate()
        {
            try
            {
                RequireEditMode("validation");
                RunStatUpgradeDefinition[] upgrades =
                    LoadUpgrades();
                ValidateInternal(
                    RequireAsset<RunUpgradeDefinitionCatalog>(
                        DefinitionCatalogPath),
                    RequireAsset<RunUpgradeUnlockScheduleDefinition>(
                        SchedulePath),
                    RequireAsset<RunUpgradeUnlockScheduleCatalog>(
                        ScheduleCatalogPath),
                    upgrades);
                Debug.Log("Run Upgrade wiring validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Run Upgrade wiring validation failed: {exception}");
            }
        }

        private static RunStatUpgradeDefinition[] CreateOrUpdateUpgrades()
        {
            RunStatUpgradeDefinition[] definitions =
                new RunStatUpgradeDefinition[UpgradeConfigs.Length];
            for (int i = 0; i < UpgradeConfigs.Length; i++)
            {
                UpgradeConfig config = UpgradeConfigs[i];
                string path = GetUpgradePath(config.AssetName);
                RunStatUpgradeDefinition definition =
                    AssetDatabase.LoadAssetAtPath<
                        RunStatUpgradeDefinition>(path);
                if (definition == null)
                {
                    definition = ScriptableObject.CreateInstance<
                        RunStatUpgradeDefinition>();
                    AssetDatabase.CreateAsset(definition, path);
                }

                definition.ConfigureForEditor(
                    config.UpgradeId,
                    config.DisplayName,
                    config.Description,
                    null,
                    new[]
                    {
                        new StatModifierData(
                            config.Stat,
                            config.ModifierType,
                            config.Value)
                    });
                EditorUtility.SetDirty(definition);
                definitions[i] = definition;
            }

            return definitions;
        }

        private static RunStatUpgradeDefinition[] LoadUpgrades()
        {
            RunStatUpgradeDefinition[] definitions =
                new RunStatUpgradeDefinition[UpgradeConfigs.Length];
            for (int i = 0; i < definitions.Length; i++)
            {
                definitions[i] = RequireAsset<RunStatUpgradeDefinition>(
                    GetUpgradePath(UpgradeConfigs[i].AssetName));
            }

            return definitions;
        }

        private static RunUpgradeDefinitionCatalog
            CreateOrUpdateDefinitionCatalog(
                RunStatUpgradeDefinition[] upgrades)
        {
            RunUpgradeDefinitionCatalog catalog =
                AssetDatabase.LoadAssetAtPath<
                    RunUpgradeDefinitionCatalog>(DefinitionCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<
                    RunUpgradeDefinitionCatalog>();
                AssetDatabase.CreateAsset(catalog, DefinitionCatalogPath);
            }

            catalog.ConfigureForEditor(upgrades);
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static RunUpgradeUnlockScheduleDefinition
            CreateOrUpdateSchedule(RunStatUpgradeDefinition[] upgrades)
        {
            RunUpgradeUnlockScheduleDefinition schedule =
                AssetDatabase.LoadAssetAtPath<
                    RunUpgradeUnlockScheduleDefinition>(SchedulePath);
            if (schedule == null)
            {
                schedule = ScriptableObject.CreateInstance<
                    RunUpgradeUnlockScheduleDefinition>();
                AssetDatabase.CreateAsset(schedule, SchedulePath);
            }

            schedule.ConfigureForEditor(
                "upgrade-schedule:warrior",
                ArchetypeId,
                3,
                UpgradeLevels,
                upgrades);
            EditorUtility.SetDirty(schedule);
            return schedule;
        }

        private static RunUpgradeUnlockScheduleCatalog
            CreateOrUpdateScheduleCatalog(
                RunUpgradeDefinitionCatalog upgrades,
                RunUpgradeUnlockScheduleDefinition schedule)
        {
            RunUpgradeUnlockScheduleCatalog catalog =
                AssetDatabase.LoadAssetAtPath<
                    RunUpgradeUnlockScheduleCatalog>(ScheduleCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<
                    RunUpgradeUnlockScheduleCatalog>();
                AssetDatabase.CreateAsset(catalog, ScheduleCatalogPath);
            }

            catalog.ConfigureForEditor(upgrades, new[] { schedule });
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static void WireHub(
            RunUpgradeDefinitionCatalog upgrades,
            RunUpgradeUnlockScheduleCatalog schedules)
        {
            Scene scene = EditorSceneManager.OpenScene(
                HubScenePath,
                OpenSceneMode.Single);
            GameSessionRuntimeHost host =
                UnityEngine.Object.FindAnyObjectByType<
                    GameSessionRuntimeHost>(FindObjectsInactive.Include);
            if (host == null)
                throw new InvalidOperationException("Hub session host is missing.");

            host.ConfigureRunUpgradesForEditor(upgrades, schedules);
            EditorUtility.SetDirty(host);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Could not save Hub scene.");
        }

        private static void ValidateInternal(
            RunUpgradeDefinitionCatalog definitions,
            RunUpgradeUnlockScheduleDefinition schedule,
            RunUpgradeUnlockScheduleCatalog schedules,
            RunStatUpgradeDefinition[] upgrades)
        {
            if (!definitions.IsValid ||
                definitions.Definitions.Count != UpgradeConfigs.Length)
            {
                throw new InvalidOperationException(
                    "Run upgrade definition catalog is invalid.");
            }

            for (int i = 0; i < upgrades.Length; i++)
            {
                UpgradeConfig expected = UpgradeConfigs[i];
                RunStatUpgradeDefinition actual = upgrades[i];
                if (!actual.TryValidate(out string error) ||
                    actual.UpgradeId != expected.UpgradeId ||
                    actual.DisplayName != expected.DisplayName ||
                    actual.Modifiers.Count != 1 ||
                    actual.Modifiers[0].Type != expected.Stat ||
                    actual.Modifiers[0].ModifierType !=
                        expected.ModifierType ||
                    Math.Abs(actual.Modifiers[0].Value - expected.Value) >
                        0.0001f)
                {
                    throw new InvalidOperationException(
                        $"Run upgrade '{actual.name}' is invalid: {error}");
                }
            }

            if (schedule.OptionCount != 3 ||
                schedule.UnlockLevels.Count != UpgradeLevels.Length ||
                schedule.UpgradeDefinitions.Count != upgrades.Length ||
                !schedules.IsValid ||
                schedules.UpgradeDefinitions != definitions ||
                !schedules.TryResolve(
                    ArchetypeId,
                    out RunUpgradeUnlockSchedule runtimeSchedule) ||
                runtimeSchedule.Milestones.Count != UpgradeLevels.Length)
            {
                throw new InvalidOperationException(
                    "Warrior upgrade schedule is invalid.");
            }

            for (int i = 0; i < UpgradeLevels.Length; i++)
            {
                if (schedule.UnlockLevels[i] != UpgradeLevels[i] ||
                    runtimeSchedule.Milestones[i].UnlockLevel !=
                        UpgradeLevels[i] ||
                    runtimeSchedule.Milestones[i].OptionCount != 3)
                {
                    throw new InvalidOperationException(
                        $"Upgrade milestone {i} is invalid.");
                }
            }

            RunAbilityUnlockScheduleCatalog abilities =
                RequireAsset<RunAbilityUnlockScheduleCatalog>(
                    AbilityScheduleCatalogPath);
            if (!abilities.TryResolve(
                    ArchetypeId,
                    out RunAbilityUnlockSchedule abilitySchedule) ||
                HasConflictingLevels(abilitySchedule, runtimeSchedule))
            {
                throw new InvalidOperationException(
                    "Ability and upgrade schedules conflict.");
            }

            EditorSceneManager.OpenScene(
                HubScenePath,
                OpenSceneMode.Single);
            GameSessionRuntimeHost host =
                UnityEngine.Object.FindAnyObjectByType<
                    GameSessionRuntimeHost>(FindObjectsInactive.Include);
            if (host == null || host.RunUpgrades != definitions ||
                host.UpgradeUnlockSchedules != schedules)
            {
                throw new InvalidOperationException(
                    "Hub session host is not wired to run upgrades.");
            }
        }

        private static bool HasConflictingLevels(
            RunAbilityUnlockSchedule abilities,
            RunUpgradeUnlockSchedule upgrades)
        {
            HashSet<int> levels = new();
            for (int i = 0; i < abilities.Milestones.Count; i++)
                levels.Add(abilities.Milestones[i].UnlockLevel);

            for (int i = 0; i < upgrades.Milestones.Count; i++)
            {
                if (levels.Contains(upgrades.Milestones[i].UnlockLevel))
                    return true;
            }

            return false;
        }

        private static string GetUpgradePath(string assetName)
        {
            return $"{UpgradeFolder}/{assetName}.asset";
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = path.Substring(0, path.LastIndexOf('/'));
            string name = path.Substring(path.LastIndexOf('/') + 1);
            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, name);
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
                    $"Exit Play Mode before run upgrade {operation}.");
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

        private readonly struct UpgradeConfig
        {
            public UpgradeConfig(
                string assetName,
                string upgradeId,
                string displayName,
                string description,
                StatType stat,
                StatModifierType modifierType,
                float value)
            {
                AssetName = assetName;
                UpgradeId = upgradeId;
                DisplayName = displayName;
                Description = description;
                Stat = stat;
                ModifierType = modifierType;
                Value = value;
            }

            public string AssetName { get; }
            public string UpgradeId { get; }
            public string DisplayName { get; }
            public string Description { get; }
            public StatType Stat { get; }
            public StatModifierType ModifierType { get; }
            public float Value { get; }
        }
    }
}
