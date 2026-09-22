using System;
using System.Collections.Generic;
using Titanhold.Combat.Abilities;
using Titanhold.Combat.Effects;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Run.Editor
{
    public static class WarriorSelfBuffVerticalSliceWiringEditor
    {
        private const string IronGuardPath =
            "Assets/_Project/ScriptableObjects/Abilities/IronGuard.asset";
        private const string BattleCryPath =
            "Assets/_Project/ScriptableObjects/Abilities/BattleCry.asset";
        private const string AbilityCatalogPath =
            "Assets/_Project/ScriptableObjects/Abilities/AbilityDefinitionCatalog.asset";
        private const string SchedulePath =
            "Assets/_Project/ScriptableObjects/Run/WarriorAbilityUnlockSchedule.asset";
        private const string WhirlwindPath =
            "Assets/_Project/ScriptableObjects/Abilities/Whirlwind.asset";
        private const string ChargePath =
            "Assets/_Project/ScriptableObjects/Abilities/Charge.asset";
        private const string PlayerPrefabPath =
            "Assets/_Project/Prefabs/Player.prefab";

        [MenuItem("Tools/Titanhold/Install Warrior Self-Buff Ability Content")]
        public static void Install()
        {
            try
            {
                RequireEditMode();
                SelfStatEffectAbilityDefinition ironGuard =
                    CreateOrLoad(IronGuardPath);
                SelfStatEffectAbilityDefinition battleCry =
                    CreateOrLoad(BattleCryPath);
                AbilityDefinitionCatalog abilities =
                    RequireAsset<AbilityDefinitionCatalog>(
                        AbilityCatalogPath);
                RunAbilityUnlockScheduleDefinition schedule =
                    RequireAsset<RunAbilityUnlockScheduleDefinition>(
                        SchedulePath);

                ConfigureAbility(
                    ironGuard,
                    "ability:iron-guard",
                    "Iron Guard",
                    "Gain 30% Armor for 8 seconds.",
                    "effect:iron-guard-armor",
                    StatType.Armor,
                    30f);
                ConfigureAbility(
                    battleCry,
                    "ability:battle-cry",
                    "Battle Cry",
                    "Gain 20% Damage for 8 seconds.",
                    "effect:battle-cry-damage",
                    StatType.Damage,
                    20f);
                EnsureCatalogContains(
                    abilities,
                    ironGuard,
                    battleCry);
                ConfigureSchedule(schedule, ironGuard, battleCry);
                AssetDatabase.SaveAssets();
                ValidateInternal(
                    ironGuard,
                    battleCry,
                    abilities,
                    schedule);
                Debug.Log(
                    "Warrior self-buff ability content installed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "Warrior self-buff ability content installation failed: " +
                    exception);
            }
        }

        [MenuItem("Tools/Titanhold/Validate Warrior Self-Buff Ability Content")]
        public static void Validate()
        {
            try
            {
                RequireEditMode();
                ValidateInternal(
                    RequireAsset<SelfStatEffectAbilityDefinition>(
                        IronGuardPath),
                    RequireAsset<SelfStatEffectAbilityDefinition>(
                        BattleCryPath),
                    RequireAsset<AbilityDefinitionCatalog>(
                        AbilityCatalogPath),
                    RequireAsset<RunAbilityUnlockScheduleDefinition>(
                        SchedulePath));
                Debug.Log(
                    "Warrior self-buff ability content validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "Warrior self-buff ability content validation failed: " +
                    exception);
            }
        }

        private static SelfStatEffectAbilityDefinition CreateOrLoad(
            string path)
        {
            SelfStatEffectAbilityDefinition ability =
                AssetDatabase.LoadAssetAtPath<
                    SelfStatEffectAbilityDefinition>(path);
            if (ability != null)
                return ability;

            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
            {
                throw new InvalidOperationException(
                    $"Ability path is occupied by another asset: {path}");
            }

            ability = ScriptableObject.CreateInstance<
                SelfStatEffectAbilityDefinition>();
            AssetDatabase.CreateAsset(ability, path);
            return ability;
        }

        private static void ConfigureAbility(
            SelfStatEffectAbilityDefinition ability,
            string abilityId,
            string displayName,
            string description,
            string effectId,
            StatType statType,
            float value)
        {
            SerializedObject data = new(ability);
            data.FindProperty("abilityId").stringValue = abilityId;
            data.FindProperty("displayName").stringValue = displayName;
            data.FindProperty("description").stringValue = description;
            data.FindProperty("resourceCost").floatValue = 20f;
            data.FindProperty("cooldown").floatValue = 20f;
            data.FindProperty("windUp").floatValue = 0f;
            data.FindProperty("recovery").floatValue = 0.2f;
            data.FindProperty("animatorTrigger").stringValue = string.Empty;
            SerializedProperty effect = data.FindProperty("selfEffect");
            effect.FindPropertyRelative("enabled").boolValue = true;
            effect.FindPropertyRelative("effectId").stringValue = effectId;
            effect.FindPropertyRelative("statType").enumValueIndex =
                (int)statType;
            effect.FindPropertyRelative("modifierType").enumValueIndex =
                (int)StatModifierType.Increased;
            effect.FindPropertyRelative("valuePerStack").floatValue = value;
            effect.FindPropertyRelative("maximumStacks").intValue = 1;
            effect.FindPropertyRelative("duration").floatValue = 8f;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ability);
        }

        private static void EnsureCatalogContains(
            AbilityDefinitionCatalog catalog,
            params SelfStatEffectAbilityDefinition[] additions)
        {
            List<ScriptableObject> definitions =
                new(catalog.Definitions.Count + additions.Length);
            for (int i = 0; i < catalog.Definitions.Count; i++)
                definitions.Add(catalog.Definitions[i]);

            for (int i = 0; i < additions.Length; i++)
            {
                if (!definitions.Contains(additions[i]))
                    definitions.Add(additions[i]);
            }

            catalog.ConfigureForEditor(definitions.ToArray());
            EditorUtility.SetDirty(catalog);
        }

        private static void ConfigureSchedule(
            RunAbilityUnlockScheduleDefinition schedule,
            SelfStatEffectAbilityDefinition ironGuard,
            SelfStatEffectAbilityDefinition battleCry)
        {
            RunAbilityUnlockMilestoneDefinition[] milestones =
            {
                Milestone(
                    3,
                    1,
                    1,
                    new[]
                    {
                        RequireAsset<ScriptableObject>(WhirlwindPath)
                    }),
                Milestone(
                    7,
                    2,
                    1,
                    new[]
                    {
                        RequireAsset<ScriptableObject>(ChargePath)
                    }),
                Milestone(
                    10,
                    3,
                    1,
                    new ScriptableObject[] { ironGuard }),
                Milestone(
                    15,
                    4,
                    1,
                    new ScriptableObject[] { battleCry })
            };
            schedule.ConfigureForEditor(
                "ability-schedule:warrior",
                "archetype:warrior",
                milestones);
            EditorUtility.SetDirty(schedule);
        }

        private static RunAbilityUnlockMilestoneDefinition Milestone(
            int level,
            int slotIndex,
            int optionCount,
            ScriptableObject[] abilities)
        {
            RunAbilityUnlockMilestoneDefinition milestone = new();
            milestone.ConfigureForEditor(
                level,
                slotIndex,
                optionCount,
                abilities,
                configuredEnabled: true);
            return milestone;
        }

        private static void ValidateInternal(
            SelfStatEffectAbilityDefinition ironGuard,
            SelfStatEffectAbilityDefinition battleCry,
            AbilityDefinitionCatalog abilities,
            RunAbilityUnlockScheduleDefinition schedule)
        {
            ValidateAbility(
                ironGuard,
                "ability:iron-guard",
                "Iron Guard",
                "effect:iron-guard-armor",
                StatType.Armor,
                30f);
            ValidateAbility(
                battleCry,
                "ability:battle-cry",
                "Battle Cry",
                "effect:battle-cry-damage",
                StatType.Damage,
                20f);

            if (!abilities.IsValid ||
                !abilities.TryResolve(
                    ironGuard.AbilityId,
                    out IAbilityDefinition resolvedGuard) ||
                !ReferenceEquals(resolvedGuard, ironGuard) ||
                !abilities.TryResolve(
                    battleCry.AbilityId,
                    out IAbilityDefinition resolvedCry) ||
                !ReferenceEquals(resolvedCry, battleCry))
            {
                throw new InvalidOperationException(
                    "Warrior self-buffs are missing from the ability catalog.");
            }

            if (!schedule.TryCreateSchedule(
                    out RunAbilityUnlockSchedule runtime,
                    out string error) ||
                runtime.Milestones.Count != 4 ||
                runtime.Milestones[3].UnlockLevel != 15 ||
                runtime.Milestones[3].TargetSlotIndex != 4 ||
                runtime.Milestones[2].UnlockLevel != 10 ||
                runtime.Milestones[2].TargetSlotIndex != 3 ||
                runtime.Milestones[2].OptionCount != 1 ||
                runtime.Milestones[2].CandidateAbilityIds.Count != 1 ||
                runtime.Milestones[2].CandidateAbilityIds[0] !=
                    "ability:iron-guard" ||
                runtime.Milestones[3].UnlockLevel != 15 ||
                runtime.Milestones[3].TargetSlotIndex != 4 ||
                runtime.Milestones[3].OptionCount != 1 ||
                runtime.Milestones[3].CandidateAbilityIds.Count != 1 ||
                runtime.Milestones[3].CandidateAbilityIds[0] !=
                    "ability:battle-cry")
            {
                throw new InvalidOperationException(
                    $"Warrior level-15 milestone is invalid: {error}");
            }

            GameObject root = PrefabUtility.LoadPrefabContents(
                PlayerPrefabPath);
            try
            {
                if (root.GetComponent<TimedStackingStatEffectReceiver>() == null)
                {
                    throw new InvalidOperationException(
                        "Player prefab is missing its timed stat-effect receiver.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ValidateAbility(
            SelfStatEffectAbilityDefinition ability,
            string abilityId,
            string displayName,
            string effectId,
            StatType statType,
            float value)
        {
            if (!ability.TryCreateSnapshot(
                    out SelfStatEffectAbilitySnapshot snapshot) ||
                ability.DisplayName != displayName ||
                snapshot.Execution.AbilityId != abilityId ||
                snapshot.Execution.ResourceCost != 20f ||
                snapshot.Execution.Cooldown != 20d ||
                snapshot.Execution.WindUp != 0d ||
                Math.Abs(
                    snapshot.Execution.Recovery - 0.2d) > 0.000001d ||
                snapshot.AnimatorTrigger.Length != 0 ||
                snapshot.Effect.EffectId != effectId ||
                snapshot.Effect.StatType != statType ||
                snapshot.Effect.ModifierType !=
                    StatModifierType.Increased ||
                snapshot.Effect.ValuePerStack != value ||
                snapshot.Effect.MaximumStacks != 1 ||
                snapshot.Effect.Duration != 8d)
            {
                throw new InvalidOperationException(
                    $"Ability '{displayName}' does not match prototype balance.");
            }
        }

        private static T RequireAsset<T>(string path)
            where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new InvalidOperationException($"Asset is missing: {path}");

            return asset;
        }

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Exit Play Mode before self-buff content wiring.");
            }
        }
    }
}
