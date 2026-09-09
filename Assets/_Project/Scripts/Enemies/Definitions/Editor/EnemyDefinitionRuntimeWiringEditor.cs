using System;
using System.Collections.Generic;
using Titanhold.Run;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Titanhold.Enemies.Editor
{
    public static class EnemyDefinitionRuntimeWiringEditor
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/SampleScene.unity";
        private const string DefinitionsFolder =
            "Assets/_Project/ScriptableObjects/Enemies";
        private const string CatalogPath =
            DefinitionsFolder + "/EnemyDefinitionCatalog.asset";

        private static readonly EnemyAssetSpec[] Specs =
        {
            new(
                "enemy:skeleton",
                "Assets/_Project/Prefabs/Enemy/Skelet.prefab",
                DefinitionsFolder + "/Enemy_Skelet.asset"),
            new(
                "enemy:skeleton-warrior",
                "Assets/_Project/Prefabs/Enemy/Skelet_Warrior.prefab",
                DefinitionsFolder + "/Enemy_Skelet_Warrior.asset"),
            new(
                "enemy:skeleton-assault",
                "Assets/_Project/Prefabs/Enemy/Skelet_Assault.prefab",
                DefinitionsFolder + "/Enemy_Skelet_Assault.asset"),
            new(
                "enemy:skeleton-boss-prototype",
                "Assets/_Project/Prefabs/Enemy/Skelet_Boss_Prototype.prefab",
                DefinitionsFolder + "/Enemy_Skelet_Boss_Prototype.asset")
        };

        [MenuItem(
            "Tools/Titanhold/Install Enemy Definition Runtime Wiring")]
        public static void Install()
        {
            try
            {
                RequireEditMode("installation");
                Scene scene = RequireCleanSampleScene();
                EnsureDefinitionsFolder();

                EnemyDefinition[] definitions =
                    new EnemyDefinition[Specs.Length];
                for (int i = 0; i < Specs.Length; i++)
                {
                    EnemyBaseStats capturedStats = InstallPrefabBinding(
                        Specs[i]);
                    definitions[i] = CreateOrValidateDefinition(
                        Specs[i],
                        capturedStats);
                }

                EnemyDefinitionCatalog catalog =
                    CreateOrUpdateCatalog(definitions);
                ConfigureScene(scene, catalog);
                AssetDatabase.SaveAssets();
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException(
                        $"Could not save {ScenePath}.");
                }

                ValidateInternal(scene);
                Debug.Log(
                    "Enemy definition runtime wiring installed for four active enemy prefabs.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Enemy definition runtime wiring installation failed: {exception}");
            }
        }

        [MenuItem(
            "Tools/Titanhold/Validate Enemy Definition Runtime Wiring")]
        public static void Validate()
        {
            try
            {
                RequireEditMode("validation");
                Scene scene = RequireCleanSampleScene();
                ValidateInternal(scene);
                Debug.Log(
                    "Enemy definition runtime wiring validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Enemy definition runtime wiring validation failed: {exception}");
            }
        }

        private static EnemyBaseStats InstallPrefabBinding(
            EnemyAssetSpec spec)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(
                spec.PrefabPath);
            if (contents == null)
            {
                throw new InvalidOperationException(
                    $"Could not load {spec.PrefabPath}.");
            }

            try
            {
                CharacterStats stats = RequireSingle<CharacterStats>(
                    contents,
                    spec.PrefabPath);
                Health health = RequireSingle<Health>(
                    contents,
                    spec.PrefabPath);
                EnemyCombat combat = RequireSingle<EnemyCombat>(
                    contents,
                    spec.PrefabPath);
                EnemyMovement movement = RequireSingle<EnemyMovement>(
                    contents,
                    spec.PrefabPath);
                EnemySensor sensor = RequireSingle<EnemySensor>(
                    contents,
                    spec.PrefabPath);
                NavMeshAgent agent = RequireSingle<NavMeshAgent>(
                    contents,
                    spec.PrefabPath);
                EnemyBaseStatsReceiver[] receivers =
                    contents.GetComponentsInChildren<EnemyBaseStatsReceiver>(
                        true);
                if (receivers.Length > 1)
                {
                    throw new InvalidOperationException(
                        $"{spec.PrefabPath} contains more than one EnemyBaseStatsReceiver.");
                }

                EnemyBaseStatsReceiver receiver = receivers.Length == 0
                    ? stats.gameObject.AddComponent<EnemyBaseStatsReceiver>()
                    : receivers[0];
                if (receiver.gameObject != stats.gameObject)
                {
                    throw new InvalidOperationException(
                        $"{spec.PrefabPath} has EnemyBaseStatsReceiver outside its CharacterStats object.");
                }

                EnemyBaseStats baseStats = CaptureLegacyBaseStats(
                    health,
                    combat,
                    movement,
                    sensor,
                    agent,
                    spec.PrefabPath);
                receiver.ConfigureForEditor(
                    stats,
                    health,
                    combat,
                    movement,
                    sensor);

                EnemyDefinitionBinding[] bindings =
                    contents.GetComponentsInChildren<EnemyDefinitionBinding>(
                        true);
                if (bindings.Length > 1)
                {
                    throw new InvalidOperationException(
                        $"{spec.PrefabPath} contains more than one EnemyDefinitionBinding.");
                }

                EnemyDefinitionBinding binding = bindings.Length == 0
                    ? stats.gameObject.AddComponent<EnemyDefinitionBinding>()
                    : bindings[0];
                if (binding.gameObject != stats.gameObject)
                {
                    throw new InvalidOperationException(
                        $"{spec.PrefabPath} has EnemyDefinitionBinding outside its CharacterStats object.");
                }

                binding.ConfigureForEditor(spec.EnemyId, receiver);
                ValidatePrefabContents(contents, spec);

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(
                    contents,
                    spec.PrefabPath,
                    out bool success);
                if (!success || saved == null)
                {
                    throw new InvalidOperationException(
                        $"Could not save {spec.PrefabPath}.");
                }

                return baseStats;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static EnemyBaseStats CaptureLegacyBaseStats(
            Health health,
            EnemyCombat combat,
            EnemyMovement movement,
            EnemySensor sensor,
            NavMeshAgent agent,
            string prefabPath)
        {
            float maximumHealth = ReadFloat(
                health,
                "maxHealth",
                prefabPath);
            float armor = 0f;
            float baseDamage = ReadFloat(
                combat,
                "damage",
                prefabPath);
            float attackCooldown = ReadFloat(
                combat,
                "attackCooldown",
                prefabPath);
            float attackRange = ReadFloat(
                combat,
                "attackRange",
                prefabPath);
            float attacksPerSecond = attackCooldown > 0f
                ? 1f / attackCooldown
                : 0f;
            EnemyBaseStats result = new(
                maximumHealth,
                armor,
                baseDamage,
                attacksPerSecond,
                attackRange,
                agent.speed,
                movement.RotationSpeed,
                sensor.DetectionRange);
            if (!result.TryValidate(out string error))
            {
                throw new InvalidOperationException(
                    $"Could not migrate base stats from {prefabPath}: {error}");
            }

            return result;
        }

        private static EnemyDefinition CreateOrValidateDefinition(
            EnemyAssetSpec spec,
            EnemyBaseStats capturedStats)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                spec.PrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    $"Enemy prefab is missing: {spec.PrefabPath}");
            }

            EnemyDefinition definition =
                AssetDatabase.LoadAssetAtPath<EnemyDefinition>(
                    spec.DefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<EnemyDefinition>();
                definition.ConfigureForEditor(
                    spec.EnemyId,
                    prefab,
                    capturedStats);
                AssetDatabase.CreateAsset(definition, spec.DefinitionPath);
                return definition;
            }

            if (!definition.TryCreateArchetype(
                    out EnemyArchetype archetype,
                    out string error))
            {
                throw new InvalidOperationException(error);
            }

            if (archetype.EnemyId != spec.EnemyId ||
                definition.Prefab != prefab)
            {
                throw new InvalidOperationException(
                    $"Existing definition at {spec.DefinitionPath} does not match its expected stable id and prefab.");
            }

            return definition;
        }

        private static EnemyDefinitionCatalog CreateOrUpdateCatalog(
            EnemyDefinition[] definitions)
        {
            EnemyDefinitionCatalog catalog =
                AssetDatabase.LoadAssetAtPath<EnemyDefinitionCatalog>(
                    CatalogPath);
            if (catalog == null)
            {
                catalog =
                    ScriptableObject.CreateInstance<EnemyDefinitionCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.ConfigureForEditor(definitions);
            EditorUtility.SetDirty(catalog);
            if (!catalog.IsValid)
            {
                throw new InvalidOperationException(
                    $"Enemy definition catalog is invalid: {catalog.ValidationError}");
            }

            return catalog;
        }

        private static void ConfigureScene(
            Scene scene,
            EnemyDefinitionCatalog catalog)
        {
            RunFlowRuntime runtime = RequireSingleSceneComponent<RunFlowRuntime>(
                scene);
            SerializedObject serializedRuntime = new(runtime);
            SerializedProperty catalogProperty =
                serializedRuntime.FindProperty("enemyDefinitionCatalog");
            if (catalogProperty == null)
            {
                throw new InvalidOperationException(
                    "RunFlowRuntime.enemyDefinitionCatalog is missing.");
            }

            catalogProperty.objectReferenceValue = catalog;
            serializedRuntime.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(runtime);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static void ValidateInternal(Scene scene)
        {
            EnemyDefinitionCatalog catalog =
                AssetDatabase.LoadAssetAtPath<EnemyDefinitionCatalog>(
                    CatalogPath);
            if (catalog == null || !catalog.IsValid)
            {
                throw new InvalidOperationException(
                    "The enemy definition catalog is missing or invalid.");
            }

            if (catalog.Definitions.Count != Specs.Length)
            {
                throw new InvalidOperationException(
                    "The enemy definition catalog does not contain exactly the four active definitions.");
            }

            for (int i = 0; i < Specs.Length; i++)
            {
                EnemyAssetSpec spec = Specs[i];
                EnemyDefinition definition =
                    AssetDatabase.LoadAssetAtPath<EnemyDefinition>(
                        spec.DefinitionPath);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    spec.PrefabPath);
                if (definition == null || prefab == null)
                {
                    throw new InvalidOperationException(
                        $"Definition or prefab is missing for {spec.EnemyId}.");
                }

                if (catalog.Definitions[i] != definition ||
                    !catalog.TryResolve(
                        spec.EnemyId,
                        out EnemyArchetype archetype) ||
                    archetype == null ||
                    !catalog.TryResolvePrefab(
                        spec.EnemyId,
                        out GameObject resolvedPrefab) ||
                    resolvedPrefab != prefab)
                {
                    throw new InvalidOperationException(
                        $"Catalog resolution is incomplete for {spec.EnemyId}.");
                }

                ValidatePrefabAsset(spec);
            }

            RunFlowRuntime runtime = RequireSingleSceneComponent<RunFlowRuntime>(
                scene);
            if (runtime.EnemyDefinitions != catalog)
            {
                throw new InvalidOperationException(
                    "SampleScene RunFlowRuntime does not reference the enemy definition catalog.");
            }

            ValidateSceneSpawnSources(scene, catalog);
        }

        private static void ValidatePrefabAsset(EnemyAssetSpec spec)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(
                spec.PrefabPath);
            if (contents == null)
            {
                throw new InvalidOperationException(
                    $"Could not load {spec.PrefabPath} for validation.");
            }

            try
            {
                ValidatePrefabContents(contents, spec);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void ValidatePrefabContents(
            GameObject contents,
            EnemyAssetSpec spec)
        {
            CharacterStats stats = RequireSingle<CharacterStats>(
                contents,
                spec.PrefabPath);
            EnemyBaseStatsReceiver receiver =
                RequireSingle<EnemyBaseStatsReceiver>(
                    contents,
                    spec.PrefabPath);
            EnemyDefinitionBinding binding =
                RequireSingle<EnemyDefinitionBinding>(
                    contents,
                    spec.PrefabPath);
            SerializedProperty receiverProperty =
                new SerializedObject(binding).FindProperty(
                    "baseStatsReceiver");
            if (binding.gameObject != stats.gameObject ||
                receiver.gameObject != stats.gameObject ||
                binding.EnemyId != spec.EnemyId ||
                receiverProperty == null ||
                receiverProperty.objectReferenceValue != receiver ||
                !receiver.HasRequiredReferences)
            {
                throw new InvalidOperationException(
                    $"{spec.PrefabPath} has incomplete enemy definition wiring.");
            }
        }

        private static void ValidateSceneSpawnSources(
            Scene scene,
            EnemyDefinitionCatalog catalog)
        {
            EnemyDefinitionBinding[] authoredBindings =
                GetSceneComponents<EnemyDefinitionBinding>(scene);
            for (int i = 0; i < authoredBindings.Length; i++)
            {
                EnemyDefinitionBinding binding = authoredBindings[i];
                if (!catalog.TryResolve(binding.EnemyId, out _))
                {
                    throw new InvalidOperationException(
                        $"Authored enemy '{binding.gameObject.name}' uses unknown definition id '{binding.EnemyId}'.");
                }
            }

            WorldEnemySpawnZone[] zones =
                GetSceneComponents<WorldEnemySpawnZone>(scene);
            for (int i = 0; i < zones.Length; i++)
            {
                GameObject prefab = ReadObjectReference<GameObject>(
                    zones[i],
                    "enemyPrefab");
                ValidateSpawnPrefab(prefab, catalog, zones[i].name);
            }

            WorldEnemyRespawnPoint[] points =
                GetSceneComponents<WorldEnemyRespawnPoint>(scene);
            for (int i = 0; i < points.Length; i++)
            {
                GameObject prefab = ReadObjectReference<GameObject>(
                    points[i],
                    "enemyPrefab");
                ValidateSpawnPrefab(prefab, catalog, points[i].name);
            }

            AssaultWaveSpawner[] assaultSpawners =
                GetSceneComponents<AssaultWaveSpawner>(scene);
            for (int i = 0; i < assaultSpawners.Length; i++)
            {
                ValidateWaveDefinition(
                    ReadObjectReference<AssaultWaveDefinition>(
                        assaultSpawners[i],
                        "waveDefinition"),
                    catalog,
                    "regular assault");
                ValidateWaveDefinition(
                    ReadObjectReference<AssaultWaveDefinition>(
                        assaultSpawners[i],
                        "bossWaveDefinition"),
                    catalog,
                    "boss assault");
            }
        }

        private static void ValidateWaveDefinition(
            AssaultWaveDefinition definition,
            EnemyDefinitionCatalog catalog,
            string context)
        {
            if (definition == null)
            {
                throw new InvalidOperationException(
                    $"The {context} definition is missing.");
            }

            if (!definition.TryCreatePlan(
                    out AssaultWavePlan plan,
                    out string error))
            {
                throw new InvalidOperationException(
                    $"Invalid {context} definition: {error}");
            }

            for (int i = 0; i < plan.Steps.Count; i++)
            {
                ValidateSpawnPrefab(
                    plan.Steps[i].EnemyPrefab,
                    catalog,
                    context);
            }
        }

        private static void ValidateSpawnPrefab(
            GameObject prefab,
            EnemyDefinitionCatalog catalog,
            string context)
        {
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    $"Spawn source '{context}' has no enemy prefab.");
            }

            EnemyDefinitionBinding binding =
                prefab.GetComponentInChildren<EnemyDefinitionBinding>(true);
            if (binding == null ||
                !catalog.TryResolvePrefab(
                    binding.EnemyId,
                    out GameObject resolvedPrefab) ||
                resolvedPrefab != prefab)
            {
                throw new InvalidOperationException(
                    $"Spawn source '{context}' uses an enemy prefab outside the definition catalog.");
            }
        }

        private static float ReadFloat(
            UnityEngine.Object target,
            string propertyName,
            string context)
        {
            SerializedProperty property =
                new SerializedObject(target).FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"{context} is missing serialized property '{propertyName}'.");
            }

            return property.floatValue;
        }

        private static T ReadObjectReference<T>(
            UnityEngine.Object target,
            string propertyName)
            where T : UnityEngine.Object
        {
            SerializedProperty property =
                new SerializedObject(target).FindProperty(propertyName);
            return property != null
                ? property.objectReferenceValue as T
                : null;
        }

        private static T RequireSingle<T>(
            GameObject root,
            string context)
            where T : Component
        {
            T[] components = root.GetComponentsInChildren<T>(true);
            if (components.Length != 1)
            {
                throw new InvalidOperationException(
                    $"{context} must contain exactly one {typeof(T).Name} component.");
            }

            return components[0];
        }

        private static T RequireSingleSceneComponent<T>(Scene scene)
            where T : Component
        {
            T[] components = GetSceneComponents<T>(scene);
            if (components.Length != 1)
            {
                throw new InvalidOperationException(
                    $"{ScenePath} must contain exactly one {typeof(T).Name} component.");
            }

            return components[0];
        }

        private static T[] GetSceneComponents<T>(Scene scene)
            where T : Component
        {
            List<T> result = new();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                result.AddRange(
                    roots[i].GetComponentsInChildren<T>(true));
            }

            return result.ToArray();
        }

        private static void EnsureDefinitionsFolder()
        {
            const string parent = "Assets/_Project/ScriptableObjects";
            if (!AssetDatabase.IsValidFolder(parent))
            {
                throw new InvalidOperationException(
                    $"Definitions parent folder is missing: {parent}");
            }

            if (!AssetDatabase.IsValidFolder(DefinitionsFolder))
                AssetDatabase.CreateFolder(parent, "Enemies");
        }

        private static void RequireEditMode(string operation)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    $"Enemy definition runtime wiring {operation} is available only in Edit Mode.");
            }
        }

        private static Scene RequireCleanSampleScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    $"Open {ScenePath} before enemy definition wiring.");
            }

            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "The active scene has unrelated unsaved changes. Save or revert them first.");
            }

            return scene;
        }

        private readonly struct EnemyAssetSpec
        {
            public EnemyAssetSpec(
                string enemyId,
                string prefabPath,
                string definitionPath)
            {
                EnemyId = enemyId;
                PrefabPath = prefabPath;
                DefinitionPath = definitionPath;
            }

            public string EnemyId { get; }
            public string PrefabPath { get; }
            public string DefinitionPath { get; }
        }
    }
}
