using System;
using System.Collections.Generic;
using Titanhold.Combat.Abilities;
using Titanhold.Combat.Effects;
using Titanhold.Run;
using Titanhold.UI.Hub;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Titanhold.Session.Editor
{
    public static class AbilityDefinitionCatalogWiringEditor
    {
        private const string HubScenePath =
            "Assets/_Project/Scenes/HubScene.unity";
        private const string AbilityFolder =
            "Assets/_Project/ScriptableObjects/Abilities";
        private const string RunFolder =
            "Assets/_Project/ScriptableObjects/Run";
        private const string AbilityCatalogPath =
            AbilityFolder + "/AbilityDefinitionCatalog.asset";
        private const string SpinPath =
            "Assets/_Project/ScriptableObjects/Configs/SpinAbility.asset";
        private const string HeavyStrikePath =
            AbilityFolder + "/HeavyStrike.asset";
        private const string CrushingStrikePath =
            AbilityFolder + "/CrushingStrike.asset";
        private const string CleavePath = AbilityFolder + "/Cleave.asset";
        private const string StartingPoolPath =
            RunFolder + "/WarriorStartingAbilityPool.asset";
        private const string StartingPoolCatalogPath =
            RunFolder + "/StartingAbilityPoolCatalog.asset";
        private const string ResourceLoadoutPath =
            RunFolder + "/WarriorCombatResourceLoadout.asset";
        private const string ResourceCatalogPath =
            RunFolder + "/CombatResourceLoadoutCatalog.asset";

        private const string WarriorArchetypeId = "archetype:warrior";
        private const string RageResourceId = "resource:rage";
        private const string SpinAbilityId = "ability:spin";
        private const string HeavyStrikeAbilityId = "ability:heavy-strike";
        private const string CrushingStrikeAbilityId =
            "ability:crushing-strike";
        private const string CleaveAbilityId = "ability:cleave";
        private const int EnemyMask = 1 << 6;
        private const int DefaultObstructionMask = 1 << 0;

        [MenuItem("Tools/Titanhold/Install Warrior Starter Ability Data")]
        public static void Install()
        {
            try
            {
                RequireEditMode("installation");
                RequireCleanOpenScene();

                AreaDamageAbilityDefinition spin =
                    AssetDatabase.LoadAssetAtPath<AreaDamageAbilityDefinition>(
                        SpinPath);
                if (spin == null || spin.AbilityId != SpinAbilityId)
                {
                    throw new InvalidOperationException(
                        $"'{SpinPath}' must define '{SpinAbilityId}'.");
                }

                TargetedDamageAbilityDefinition heavy =
                    LoadOrCreate<TargetedDamageAbilityDefinition>(
                        HeavyStrikePath);
                ConfigureTargeted(
                    heavy,
                    HeavyStrikeAbilityId,
                    "Heavy Strike",
                    "A powerful single-target strike. Generates 1 Rage on hit.",
                    damageMultiplier: 1.5f,
                    generateRage: true,
                    applyArmorBreak: false);

                TargetedDamageAbilityDefinition crushing =
                    LoadOrCreate<TargetedDamageAbilityDefinition>(
                        CrushingStrikePath);
                ConfigureTargeted(
                    crushing,
                    CrushingStrikeAbilityId,
                    "Crushing Strike",
                    "A lower-damage strike that generates 1 Rage and reduces " +
                    "Armor by 5% for 8 seconds, stacking up to 5 times.",
                    damageMultiplier: 1f,
                    generateRage: true,
                    applyArmorBreak: true);

                ConeDamageAbilityDefinition cleave =
                    LoadOrCreate<ConeDamageAbilityDefinition>(CleavePath);
                ConfigureCleave(cleave);

                AbilityDefinitionCatalog abilityCatalog =
                    LoadOrCreate<AbilityDefinitionCatalog>(
                        AbilityCatalogPath);
                abilityCatalog.ConfigureForEditor(
                    FindEveryProjectAbilityDefinition());
                EditorUtility.SetDirty(abilityCatalog);

                RunStartingAbilityPoolDefinition startingPool =
                    LoadOrCreate<RunStartingAbilityPoolDefinition>(
                        StartingPoolPath);
                startingPool.ConfigureForEditor(
                    "starting-pool:warrior",
                    WarriorArchetypeId,
                    new ScriptableObject[] { heavy, crushing, cleave });
                EditorUtility.SetDirty(startingPool);

                RunStartingAbilityPoolCatalog startingPoolCatalog =
                    LoadOrCreate<RunStartingAbilityPoolCatalog>(
                        StartingPoolCatalogPath);
                startingPoolCatalog.ConfigureForEditor(
                    abilityCatalog,
                    new[] { startingPool });
                EditorUtility.SetDirty(startingPoolCatalog);

                RunCombatResourceLoadoutDefinition resourceLoadout =
                    LoadOrCreate<RunCombatResourceLoadoutDefinition>(
                        ResourceLoadoutPath);
                resourceLoadout.ConfigureForEditor(
                    "combat-resources:warrior",
                    WarriorArchetypeId,
                    new[]
                    {
                        new RunCombatResourceEntry(
                            RageResourceId,
                            8f,
                            0f)
                    });
                EditorUtility.SetDirty(resourceLoadout);

                RunCombatResourceLoadoutCatalog resourceCatalog =
                    LoadOrCreate<RunCombatResourceLoadoutCatalog>(
                        ResourceCatalogPath);
                resourceCatalog.ConfigureForEditor(
                    new[] { resourceLoadout });
                EditorUtility.SetDirty(resourceCatalog);

                AssetDatabase.SaveAssets();
                WireHub(
                    abilityCatalog,
                    startingPoolCatalog,
                    resourceCatalog);
                AssetDatabase.SaveAssets();
                ValidateInternal();
                Debug.Log("Warrior Starter Ability Data wiring installed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Warrior Starter Ability Data installation failed: {exception}");
            }
        }

        [MenuItem("Tools/Titanhold/Install Ability Definition Catalog Wiring")]
        public static void InstallCatalogWiring()
        {
            Install();
        }

        [MenuItem("Tools/Titanhold/Validate Warrior Starter Ability Data")]
        public static void Validate()
        {
            try
            {
                RequireEditMode("validation");
                ValidateInternal();
                Debug.Log("Warrior Starter Ability Data validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Warrior Starter Ability Data validation failed: {exception}");
            }
        }

        [MenuItem("Tools/Titanhold/Validate Ability Definition Catalog Wiring")]
        public static void ValidateCatalogWiring()
        {
            Validate();
        }

        private static void ConfigureTargeted(
            TargetedDamageAbilityDefinition ability,
            string abilityId,
            string displayName,
            string description,
            float damageMultiplier,
            bool generateRage,
            bool applyArmorBreak)
        {
            SerializedObject serialized = new(ability);
            serialized.FindProperty("abilityId").stringValue = abilityId;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("description").stringValue = description;
            serialized.FindProperty("resourceCost").floatValue = 20f;
            serialized.FindProperty("cooldown").floatValue = 3f;
            serialized.FindProperty("windUp").floatValue = 0.23333333f;
            serialized.FindProperty("recovery").floatValue = 0.30000003f;
            serialized.FindProperty("damageMultiplier").floatValue =
                damageMultiplier;
            serialized.FindProperty("useRange").floatValue = 2f;
            serialized.FindProperty("releaseRangeMultiplier").floatValue =
                1.5f;
            serialized.FindProperty("obstructionMask").intValue =
                DefaultObstructionMask;
            serialized.FindProperty("maximumUseAngle").floatValue = 45f;
            serialized.FindProperty("animatorTrigger").stringValue = "Attack";
            ConfigureResourceGain(
                serialized.FindProperty("sourceResourceGain"),
                generateRage);
            ConfigureArmorBreak(
                serialized.FindProperty("onHitEffect"),
                applyArmorBreak);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ability);
        }

        private static void ConfigureCleave(ConeDamageAbilityDefinition ability)
        {
            SerializedObject serialized = new(ability);
            serialized.FindProperty("abilityId").stringValue = CleaveAbilityId;
            serialized.FindProperty("displayName").stringValue = "Cleave";
            serialized.FindProperty("description").stringValue =
                "Strike the selected target for full damage and other enemies " +
                "in a forward cone for 30% damage. Does not generate Rage.";
            serialized.FindProperty("resourceCost").floatValue = 20f;
            serialized.FindProperty("cooldown").floatValue = 3f;
            serialized.FindProperty("windUp").floatValue = 0.23333333f;
            serialized.FindProperty("recovery").floatValue = 0.30000003f;
            serialized.FindProperty("damageMultiplier").floatValue = 1f;
            serialized.FindProperty("secondaryDamageMultiplier").floatValue =
                0.3f;
            serialized.FindProperty("useRange").floatValue = 2.5f;
            serialized.FindProperty("coneAngle").floatValue = 120f;
            serialized.FindProperty("targetMask").intValue = EnemyMask;
            serialized.FindProperty("obstructionMask").intValue =
                DefaultObstructionMask;
            serialized.FindProperty("maximumUseAngle").floatValue = 45f;
            serialized.FindProperty("animatorTrigger").stringValue = "Attack";
            ConfigureResourceGain(
                serialized.FindProperty("sourceResourceGain"),
                enabled: false);
            ConfigureArmorBreak(
                serialized.FindProperty("onHitEffect"),
                enabled: false);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ability);
        }

        private static void ConfigureResourceGain(
            SerializedProperty property,
            bool enabled)
        {
            property.FindPropertyRelative("enabled").boolValue = enabled;
            property.FindPropertyRelative("resourceId").stringValue =
                RageResourceId;
            property.FindPropertyRelative("amount").floatValue =
                enabled ? 1f : 0f;
        }

        private static void ConfigureArmorBreak(
            SerializedProperty property,
            bool enabled)
        {
            property.FindPropertyRelative("enabled").boolValue = enabled;
            property.FindPropertyRelative("effectId").stringValue =
                "effect:crushing-strike-armor-break";
            property.FindPropertyRelative("statType").enumValueIndex =
                (int)StatType.Armor;
            property.FindPropertyRelative("modifierType").enumValueIndex =
                (int)StatModifierType.Increased;
            property.FindPropertyRelative("valuePerStack").floatValue = -5f;
            property.FindPropertyRelative("maximumStacks").intValue = 5;
            property.FindPropertyRelative("duration").floatValue = 8f;
        }

        private static void WireHub(
            AbilityDefinitionCatalog abilityCatalog,
            RunStartingAbilityPoolCatalog startingPoolCatalog,
            RunCombatResourceLoadoutCatalog resourceCatalog)
        {
            Scene scene = EditorSceneManager.OpenScene(
                HubScenePath,
                OpenSceneMode.Single);
            GameSessionRuntimeHost host =
                UnityEngine.Object.FindAnyObjectByType<
                    GameSessionRuntimeHost>(FindObjectsInactive.Include);
            HubRunPreparationView view =
                UnityEngine.Object.FindAnyObjectByType<
                    HubRunPreparationView>(FindObjectsInactive.Include);
            HubRunLaunchController launch =
                UnityEngine.Object.FindAnyObjectByType<
                    HubRunLaunchController>(FindObjectsInactive.Include);
            if (host == null || view == null || launch == null)
            {
                throw new InvalidOperationException(
                    "Hub session host or run launch wiring is missing.");
            }

            host.ConfigureForEditor(
                host.ItemDefinitions,
                abilityCatalog,
                startingPoolCatalog,
                host.RunProgression,
                host.ConclusionRewards);
            host.ConfigureCombatResourceLoadoutsForEditor(resourceCatalog);
            launch.ConfigureForEditor(
                view,
                host,
                "player:local",
                "character:warrior",
                WarriorArchetypeId,
                string.Empty,
                "difficulty:prototype",
                "SampleScene");
            EditorUtility.SetDirty(host);
            EditorUtility.SetDirty(launch);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Could not save Hub scene.");
        }

        private static void ValidateInternal()
        {
            AreaDamageAbilityDefinition spin = LoadRequired<
                AreaDamageAbilityDefinition>(SpinPath);
            TargetedDamageAbilityDefinition heavy = LoadRequired<
                TargetedDamageAbilityDefinition>(HeavyStrikePath);
            TargetedDamageAbilityDefinition crushing = LoadRequired<
                TargetedDamageAbilityDefinition>(CrushingStrikePath);
            ConeDamageAbilityDefinition cleave = LoadRequired<
                ConeDamageAbilityDefinition>(CleavePath);
            AbilityDefinitionCatalog abilityCatalog = LoadRequired<
                AbilityDefinitionCatalog>(AbilityCatalogPath);
            RunStartingAbilityPoolCatalog startingPoolCatalog = LoadRequired<
                RunStartingAbilityPoolCatalog>(StartingPoolCatalogPath);
            RunCombatResourceLoadoutCatalog resourceCatalog = LoadRequired<
                RunCombatResourceLoadoutCatalog>(ResourceCatalogPath);

            if (spin.AbilityId != SpinAbilityId ||
                !abilityCatalog.IsValid ||
                !ResolvesExact(abilityCatalog, SpinAbilityId, spin) ||
                !ResolvesExact(abilityCatalog, HeavyStrikeAbilityId, heavy) ||
                !ResolvesExact(
                    abilityCatalog,
                    CrushingStrikeAbilityId,
                    crushing) ||
                !ResolvesExact(abilityCatalog, CleaveAbilityId, cleave))
            {
                throw new InvalidOperationException(
                    "The ability catalog does not resolve every warrior starter and Spin.");
            }

            ValidateTargetedAbility(
                heavy,
                HeavyStrikeAbilityId,
                expectedDamage: 150f,
                expectArmorBreak: false);
            ValidateTargetedAbility(
                crushing,
                CrushingStrikeAbilityId,
                expectedDamage: 100f,
                expectArmorBreak: true);
            if (!cleave.TryCreateSnapshot(
                    100f,
                    out ConeDamageAbilitySnapshot cleaveSnapshot) ||
                cleave.AbilityId != CleaveAbilityId ||
                !Approximately(cleaveSnapshot.PrimaryDamage, 100f) ||
                !Approximately(cleaveSnapshot.SecondaryDamage, 30f) ||
                cleaveSnapshot.SourceResourceGain.IsValid ||
                cleaveSnapshot.TargetMask != EnemyMask ||
                cleaveSnapshot.Execution.Cooldown != 3d)
            {
                throw new InvalidOperationException(
                    "Cleave authored values are invalid.");
            }

            if (!startingPoolCatalog.IsValid ||
                startingPoolCatalog.AbilityCatalog != abilityCatalog ||
                !startingPoolCatalog.TryResolve(
                    WarriorArchetypeId,
                    out RunStartingAbilityPool pool) ||
                pool.AbilityIds.Count != 3 ||
                pool.AbilityIds[0] != HeavyStrikeAbilityId ||
                pool.AbilityIds[1] != CrushingStrikeAbilityId ||
                pool.AbilityIds[2] != CleaveAbilityId)
            {
                throw new InvalidOperationException(
                    "The warrior starting ability pool is invalid.");
            }

            if (!resourceCatalog.IsValid ||
                !resourceCatalog.TryResolve(
                    WarriorArchetypeId,
                    out RunCombatResourceLoadout resourceLoadout) ||
                resourceLoadout.Resources.Count != 1 ||
                resourceLoadout.Resources[0].ResourceId != RageResourceId ||
                !Approximately(resourceLoadout.Resources[0].Maximum, 8f) ||
                !Approximately(resourceLoadout.Resources[0].Initial, 0f))
            {
                throw new InvalidOperationException(
                    "The warrior Rage loadout is invalid.");
            }

            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != HubScenePath)
                throw new InvalidOperationException($"Open '{HubScenePath}'.");

            GameSessionRuntimeHost host =
                UnityEngine.Object.FindAnyObjectByType<GameSessionRuntimeHost>(
                    FindObjectsInactive.Include);
            HubRunLaunchController launch =
                UnityEngine.Object.FindAnyObjectByType<HubRunLaunchController>(
                    FindObjectsInactive.Include);
            if (host == null ||
                host.AbilityDefinitions != abilityCatalog ||
                host.StartingAbilityPools != startingPoolCatalog ||
                host.CombatResourceLoadouts != resourceCatalog ||
                launch == null ||
                launch.SessionHost != host ||
                launch.CharacterArchetypeId != WarriorArchetypeId ||
                launch.StartingAbilityId.Length != 0)
            {
                throw new InvalidOperationException(
                    "Hub does not use the warrior starter choice and Rage data.");
            }
        }

        private static void ValidateTargetedAbility(
            TargetedDamageAbilityDefinition ability,
            string expectedId,
            float expectedDamage,
            bool expectArmorBreak)
        {
            if (!ability.TryCreateSnapshot(
                    100f,
                    out TargetedDamageAbilitySnapshot snapshot) ||
                ability.AbilityId != expectedId ||
                !Approximately(snapshot.Damage, expectedDamage) ||
                !snapshot.SourceResourceGain.IsValid ||
                snapshot.SourceResourceGain.ResourceId != RageResourceId ||
                !Approximately(snapshot.SourceResourceGain.Amount, 1f) ||
                snapshot.Execution.Cooldown != 3d)
            {
                throw new InvalidOperationException(
                    $"Ability '{expectedId}' authored values are invalid.");
            }

            TimedStackingStatEffectDefinition effect = snapshot.OnHitEffect;
            if (!expectArmorBreak && effect != null)
            {
                throw new InvalidOperationException(
                    $"Ability '{expectedId}' unexpectedly applies an effect.");
            }

            if (expectArmorBreak &&
                (effect == null ||
                 effect.StatType != StatType.Armor ||
                 effect.ModifierType != StatModifierType.Increased ||
                 !Approximately(effect.ValuePerStack, -5f) ||
                 effect.MaximumStacks != 5 ||
                 effect.Duration != 8d))
            {
                throw new InvalidOperationException(
                    "Crushing Strike armor reduction is invalid.");
            }
        }

        private static ScriptableObject[] FindEveryProjectAbilityDefinition()
        {
            string[] guids = AssetDatabase.FindAssets(
                "t:ScriptableObject",
                new[] { "Assets/_Project/ScriptableObjects" });
            List<ScriptableObject> definitions = new();
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                ScriptableObject asset =
                    AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset is IAbilityDefinition)
                    definitions.Add(asset);
            }

            definitions.Sort((left, right) => string.Compare(
                AssetDatabase.GetAssetPath(left),
                AssetDatabase.GetAssetPath(right),
                StringComparison.Ordinal));
            if (definitions.Count == 0)
            {
                throw new InvalidOperationException(
                    "No project ability definitions were found.");
            }

            return definitions.ToArray();
        }

        private static bool ResolvesExact(
            AbilityDefinitionCatalog catalog,
            string abilityId,
            ScriptableObject expected)
        {
            return catalog.TryResolve(
                       abilityId,
                       out IAbilityDefinition resolved) &&
                   ReferenceEquals(resolved, expected);
        }

        private static T LoadOrCreate<T>(string path)
            where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static T LoadRequired<T>(string path)
            where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException(
                    $"Required asset '{path}' is missing.");
            }

            return asset;
        }

        private static bool Approximately(float left, float right)
        {
            return Mathf.Abs(left - right) <= 0.0001f;
        }

        private static void RequireEditMode(string operation)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    $"Exit Play Mode before warrior ability data {operation}.");
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
