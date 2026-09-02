using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Titanhold.Run.Editor
{
    public static class BossEncounterVerticalSliceWiringEditor
    {
        private const string ScenePath = "Assets/_Project/Scenes/SampleScene.unity";
        private const string RuntimeObjectName = "RunFlowRuntime";
        private const string AssaultPrefabPath =
            "Assets/_Project/Prefabs/Enemy/Skelet_Assault.prefab";
        private const string BossPrefabPath =
            "Assets/_Project/Prefabs/Enemy/Skelet_Boss_Prototype.prefab";
        private const string BossWavePath =
            "Assets/_Project/ScriptableObjects/Run/AssaultWave_Boss_Prototype.asset";
        private const int RegularRoundCount = 3;
        private const float BossScaleMultiplier = 1.5f;
        private const float BossHealthMultiplier = 5f;
        private const float BossDamageMultiplier = 1.5f;

        [MenuItem("Tools/Titanhold/Install Boss Encounter Wiring")]
        public static void Install()
        {
            try
            {
                Scene scene = RequireActiveScene(requireClean: true);
                GameObject bossPrefab = CreateOrUpdateBossPrefab();
                AssaultWaveDefinition bossWave =
                    CreateOrUpdateBossWave(bossPrefab);
                ConfigureScene(scene, bossWave);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("Boss encounter vertical-slice wiring installed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Boss encounter vertical-slice wiring installation failed: {exception}");
            }
        }

        [MenuItem("Tools/Titanhold/Validate Boss Encounter Wiring")]
        public static void Validate()
        {
            try
            {
                ValidateBossPrefab();
                AssaultWaveDefinition bossWave = ValidateBossWave();
                ValidateScene(RequireActiveScene(requireClean: false), bossWave);
                Debug.Log("Boss encounter vertical-slice wiring validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Boss encounter vertical-slice wiring validation failed: {exception}");
            }
        }

        private static GameObject CreateOrUpdateBossPrefab()
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(AssaultPrefabPath);
            try
            {
                contents.name = "Skelet_Boss_Prototype";
                contents.transform.localScale *= BossScaleMultiplier;

                Health health = contents.GetComponentInChildren<Health>(true);
                EnemyCombat combat = contents.GetComponentInChildren<EnemyCombat>(true);
                if (health == null || combat == null)
                {
                    throw new InvalidOperationException(
                        "Skelet_Assault does not contain scalable combat components.");
                }

                SerializedObject serializedHealth = new SerializedObject(health);
                SerializedProperty maxHealth = serializedHealth.FindProperty("maxHealth");
                maxHealth.floatValue *= BossHealthMultiplier;
                serializedHealth.ApplyModifiedPropertiesWithoutUndo();

                SerializedObject serializedCombat = new SerializedObject(combat);
                SerializedProperty damage = serializedCombat.FindProperty("damage");
                damage.floatValue *= BossDamageMultiplier;
                serializedCombat.ApplyModifiedPropertiesWithoutUndo();

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(
                    contents,
                    BossPrefabPath);
                if (saved == null)
                    throw new InvalidOperationException($"Could not save {BossPrefabPath}.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
        }

        private static AssaultWaveDefinition CreateOrUpdateBossWave(
            GameObject bossPrefab)
        {
            AssaultWaveDefinition definition =
                AssetDatabase.LoadAssetAtPath<AssaultWaveDefinition>(BossWavePath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<AssaultWaveDefinition>();
                AssetDatabase.CreateAsset(definition, BossWavePath);
            }

            AssaultWaveDefinitionValidationRunner.ConfigureDefinition(
                definition,
                bossPrefab,
                initialDelay: 1f,
                enemyCount: 1,
                delayBeforeGroup: 0f,
                spawnInterval: 0f);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static void ConfigureScene(
            Scene scene,
            AssaultWaveDefinition bossWave)
        {
            RunFlowRuntime runtime = RequireRuntime(scene);
            AssaultWaveSpawner spawner = runtime.GetComponent<AssaultWaveSpawner>();
            if (spawner == null)
                throw new InvalidOperationException("AssaultWaveSpawner is missing.");

            SerializedObject serializedRuntime = new SerializedObject(runtime);
            serializedRuntime.FindProperty("regularRoundCount").intValue =
                RegularRoundCount;
            serializedRuntime.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject serializedSpawner = new SerializedObject(spawner);
            serializedSpawner.FindProperty("bossWaveDefinition").objectReferenceValue =
                bossWave;
            serializedSpawner.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException($"Could not save {ScenePath}.");
        }

        private static void ValidateBossPrefab()
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(AssaultPrefabPath);
            GameObject boss = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
            if (source == null || boss == null)
                throw new InvalidOperationException("Assault or boss prefab is missing.");

            if (PrefabUtility.GetPrefabAssetType(boss) != PrefabAssetType.Regular)
            {
                throw new InvalidOperationException(
                    "Boss must be an independent regular prefab, not a prefab variant.");
            }

            AssertVectorApproximately(
                boss.transform.localScale,
                source.transform.localScale * BossScaleMultiplier,
                "Boss root scale");

            Health sourceHealth = source.GetComponentInChildren<Health>(true);
            Health bossHealth = boss.GetComponentInChildren<Health>(true);
            EnemyCombat sourceCombat = source.GetComponentInChildren<EnemyCombat>(true);
            EnemyCombat bossCombat = boss.GetComponentInChildren<EnemyCombat>(true);
            if (sourceHealth == null || bossHealth == null ||
                sourceCombat == null || bossCombat == null)
            {
                throw new InvalidOperationException(
                    "Boss prefab combat composition is incomplete.");
            }

            AssertApproximately(
                bossHealth.MaxHealth,
                sourceHealth.MaxHealth * BossHealthMultiplier,
                "Boss base maximum health");
            AssertApproximately(
                bossCombat.Damage,
                sourceCombat.Damage * BossDamageMultiplier,
                "Boss base damage");

            if (boss.GetComponentInChildren<AssaultAggroTargetProvider>(true) == null ||
                boss.GetComponentInChildren<EnemyDeathNotifier>(true) == null ||
                boss.GetComponentInChildren<EnemyRewardSource>(true) == null)
            {
                throw new InvalidOperationException(
                    "Boss prefab is missing required assault encounter components.");
            }

            if (boss.GetComponentInChildren<EnemyLootTableDropper>(true) != null ||
                boss.GetComponentInChildren<EnemyRunContributionSource>(true) != null ||
                boss.GetComponentInChildren<EnemyThreatSource>(true) != null)
            {
                throw new InvalidOperationException(
                    "Boss must not emit exploration loot, Threat, or run contribution.");
            }
        }

        private static AssaultWaveDefinition ValidateBossWave()
        {
            AssaultWaveDefinition definition =
                AssetDatabase.LoadAssetAtPath<AssaultWaveDefinition>(BossWavePath);
            if (definition == null)
                throw new InvalidOperationException("Boss wave definition is missing.");

            if (!definition.TryCreatePlan(out AssaultWavePlan plan, out string error))
            {
                throw new InvalidOperationException(
                    $"Boss wave definition is invalid: {error}");
            }

            if (plan.PlannedEnemyCount != 1 || plan.Steps.Count != 1 ||
                AssetDatabase.GetAssetPath(plan.Steps[0].EnemyPrefab) != BossPrefabPath)
            {
                throw new InvalidOperationException(
                    "Boss wave must contain exactly one prototype boss.");
            }

            return definition;
        }

        private static void ValidateScene(
            Scene scene,
            AssaultWaveDefinition expectedBossWave)
        {
            RunFlowRuntime runtime = RequireRuntime(scene);
            SerializedObject serializedRuntime = new SerializedObject(runtime);
            if (serializedRuntime.FindProperty("regularRoundCount").intValue !=
                RegularRoundCount)
            {
                throw new InvalidOperationException(
                    "Run Flow is not configured for three regular rounds.");
            }

            AssaultWaveSpawner spawner = runtime.GetComponent<AssaultWaveSpawner>();
            if (spawner == null)
                throw new InvalidOperationException("AssaultWaveSpawner is missing.");

            SerializedObject serializedSpawner = new SerializedObject(spawner);
            UnityEngine.Object regularWave =
                serializedSpawner.FindProperty("waveDefinition").objectReferenceValue;
            UnityEngine.Object bossWave =
                serializedSpawner.FindProperty("bossWaveDefinition").objectReferenceValue;
            if (regularWave == null || bossWave != expectedBossWave || regularWave == bossWave)
            {
                throw new InvalidOperationException(
                    "Regular and boss wave definitions are not wired independently.");
            }
        }

        private static Scene RequireActiveScene(bool requireClean)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                throw new InvalidOperationException($"Open {ScenePath} first.");

            if (requireClean && scene.isDirty)
            {
                throw new InvalidOperationException(
                    "The active scene has unrelated unsaved changes. Save or revert them first.");
            }

            return scene;
        }

        private static RunFlowRuntime RequireRuntime(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == RuntimeObjectName)
                {
                    RunFlowRuntime runtime = roots[i].GetComponent<RunFlowRuntime>();
                    if (runtime != null)
                        return runtime;
                }
            }

            throw new InvalidOperationException("RunFlowRuntime scene object is missing.");
        }

        private static void AssertApproximately(float actual, float expected, string label)
        {
            if (Math.Abs(actual - expected) <= 0.0001f)
                return;

            throw new InvalidOperationException(
                $"{label} failed. Expected {expected}, got {actual}.");
        }

        private static void AssertVectorApproximately(
            Vector3 actual,
            Vector3 expected,
            string label)
        {
            if ((actual - expected).sqrMagnitude <= 0.000001f)
                return;

            throw new InvalidOperationException(
                $"{label} failed. Expected {expected}, got {actual}.");
        }
    }
}
