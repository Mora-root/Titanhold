using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Titanhold.Run.Editor
{
    public static class RunRoundBalanceVerticalSliceWiringEditor
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/SampleScene.unity";
        private const string BalancePath =
            "Assets/_Project/ScriptableObjects/Run/RunRoundBalance_Prototype.asset";
        private const string ProgressionPath =
            "Assets/_Project/ScriptableObjects/Run/RunProgression_Prototype.asset";
        private const string WarriorPrefabPath =
            "Assets/_Project/Prefabs/Enemy/Skelet_Warrior.prefab";
        private const string RuntimeObjectName = "RunFlowRuntime";
        private const int RegularRoundCount = 9;
        private const int FinalRoundNumber = RegularRoundCount + 1;
        private const int MaximumRunLevel = 20;
        private const int BaseExperience = 70;
        private const int ExperienceIncrease = 25;
        private const float WarriorThreat = 10f;
        private const int WarriorRunExperience = 15;

        private static readonly float[] ThreatByRound =
        {
            120f, 150f, 200f, 250f, 300f,
            350f, 400f, 450f, 500f, 500f
        };

        [MenuItem("Tools/Titanhold/Install Run Round Balance Wiring")]
        public static void Install()
        {
            try
            {
                RequireEditMode("installation");
                RequireCleanOpenScene();
                RunRoundBalanceDefinition balance =
                    CreateOrUpdateBalanceDefinition();
                ConfigureProgressionDefinition();
                ConfigureWarriorPrefab();
                ConfigureRunScene(balance);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                ValidateInternal(balance);
                Debug.Log("Run round balance vertical-slice wiring installed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Run round balance wiring installation failed: {exception}");
            }
        }

        [MenuItem("Tools/Titanhold/Validate Run Round Balance Wiring")]
        public static void Validate()
        {
            try
            {
                RequireEditMode("validation");
                RunRoundBalanceDefinition balance =
                    AssetDatabase.LoadAssetAtPath<RunRoundBalanceDefinition>(
                        BalancePath);
                ValidateInternal(balance);
                Debug.Log("Run round balance vertical-slice wiring validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Run round balance wiring validation failed: {exception}");
            }
        }

        private static RunRoundBalanceDefinition
            CreateOrUpdateBalanceDefinition()
        {
            RunRoundBalanceDefinition definition =
                AssetDatabase.LoadAssetAtPath<RunRoundBalanceDefinition>(
                    BalancePath);
            if (definition == null)
            {
                definition =
                    ScriptableObject.CreateInstance<RunRoundBalanceDefinition>();
                AssetDatabase.CreateAsset(definition, BalancePath);
            }

            RunRoundBalanceEntryDefinition[] entries =
                new RunRoundBalanceEntryDefinition[FinalRoundNumber];
            for (int index = 0; index < entries.Length; index++)
            {
                int roundNumber = index + 1;
                RunRoundBalanceEntryDefinition entry =
                    new RunRoundBalanceEntryDefinition();
                entry.ConfigureForEditor(
                    roundNumber,
                    ThreatByRound[index],
                    1f + 0.20f * index,
                    1f + 0.10f * index,
                    1f + 0.20f * index);
                entries[index] = entry;
            }

            definition.ConfigureForEditor(entries);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static void ConfigureProgressionDefinition()
        {
            RunProgressionDefinition definition =
                AssetDatabase.LoadAssetAtPath<RunProgressionDefinition>(
                    ProgressionPath);
            if (definition == null)
            {
                throw new InvalidOperationException(
                    "Run Progression Definition is missing.");
            }

            definition.ConfigureForEditor(
                MaximumRunLevel,
                BaseExperience,
                ExperienceIncrease);
            EditorUtility.SetDirty(definition);
        }

        private static void ConfigureWarriorPrefab()
        {
            GameObject contents =
                PrefabUtility.LoadPrefabContents(WarriorPrefabPath);
            try
            {
                EnemyRunContributionSource contribution =
                    contents.GetComponentInChildren<
                        EnemyRunContributionSource>(true);
                EnemyRewardSource reward =
                    contents.GetComponentInChildren<EnemyRewardSource>(true);
                if (contribution == null || reward == null)
                {
                    throw new InvalidOperationException(
                        "Skelet_Warrior is missing run contribution or reward data.");
                }

                SerializedObject serializedContribution =
                    new SerializedObject(contribution);
                serializedContribution.FindProperty("threatAmount").floatValue =
                    WarriorThreat;
                serializedContribution.ApplyModifiedPropertiesWithoutUndo();

                if (reward.RunExperienceAmount != WarriorRunExperience)
                {
                    throw new InvalidOperationException(
                        "Skelet_Warrior RunXP must remain 15 while Threat changes.");
                }

                PrefabUtility.SaveAsPrefabAsset(contents, WarriorPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void ConfigureRunScene(
            RunRoundBalanceDefinition balance)
        {
            Scene scene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Single);
            RunFlowRuntime runtime = RequireRuntime(scene);
            SerializedObject serializedRuntime = new SerializedObject(runtime);
            serializedRuntime.FindProperty("regularRoundCount").intValue =
                RegularRoundCount;
            serializedRuntime.FindProperty("startingRound").intValue = 1;
            serializedRuntime.FindProperty("roundBalanceDefinition")
                .objectReferenceValue = balance;
            serializedRuntime.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(runtime);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException($"Could not save {ScenePath}.");
        }

        private static void ValidateInternal(
            RunRoundBalanceDefinition definition)
        {
            ValidateBalanceDefinition(definition);
            ValidateProgressionDefinition();
            ValidateWarriorPrefab();

            Scene scene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Single);
            RunFlowRuntime runtime = RequireRuntime(scene);
            SerializedObject serializedRuntime = new SerializedObject(runtime);
            if (serializedRuntime.FindProperty("regularRoundCount").intValue !=
                    RegularRoundCount ||
                serializedRuntime.FindProperty("startingRound").intValue != 1 ||
                serializedRuntime.FindProperty("roundBalanceDefinition")
                    .objectReferenceValue != definition)
            {
                throw new InvalidOperationException(
                    "SampleScene does not use the authored ten-round balance.");
            }
        }

        private static void ValidateBalanceDefinition(
            RunRoundBalanceDefinition definition)
        {
            if (definition == null)
            {
                throw new InvalidOperationException(
                    "Run round balance is missing.");
            }

            if (!definition.TryCreateTable(
                    out RunRoundBalanceTable table,
                    out string error))
            {
                throw new InvalidOperationException(
                    $"Run round balance is invalid: {error}");
            }

            if (definition.Rounds.Count != FinalRoundNumber)
            {
                throw new InvalidOperationException(
                    $"Expected {FinalRoundNumber} round balance entries.");
            }

            for (int index = 0; index < ThreatByRound.Length; index++)
            {
                int roundNumber = index + 1;
                if (!table.TryResolve(
                        roundNumber,
                        out RunRoundBalanceSnapshot snapshot))
                {
                    throw new InvalidOperationException(
                        $"Round {roundNumber} is missing from the balance table.");
                }

                AssertApproximately(
                    snapshot.MaxThreat,
                    ThreatByRound[index],
                    $"Round {roundNumber} meter");
                AssertApproximately(
                    snapshot.EnemyScaling.HealthMultiplier,
                    1f + 0.20f * index,
                    $"Round {roundNumber} health multiplier");
                AssertApproximately(
                    snapshot.EnemyScaling.DamageMultiplier,
                    1f + 0.10f * index,
                    $"Round {roundNumber} damage multiplier");
                AssertApproximately(
                    snapshot.ExperienceMultiplier,
                    1f + 0.20f * index,
                    $"Round {roundNumber} experience multiplier");
            }
        }

        private static void ValidateProgressionDefinition()
        {
            RunProgressionDefinition progression =
                AssetDatabase.LoadAssetAtPath<RunProgressionDefinition>(
                    ProgressionPath);
            if (progression == null || !progression.IsValid ||
                progression.MaximumLevel != MaximumRunLevel ||
                progression.BaseExperienceToNextLevel != BaseExperience ||
                progression.ExperienceIncreasePerLevel != ExperienceIncrease)
            {
                throw new InvalidOperationException(
                    "Run progression does not use the 20-level 70/+25 curve.");
            }
        }

        private static void ValidateWarriorPrefab()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(WarriorPrefabPath);
            EnemyRunContributionSource contribution = prefab != null
                ? prefab.GetComponentInChildren<
                    EnemyRunContributionSource>(true)
                : null;
            EnemyRewardSource reward = prefab != null
                ? prefab.GetComponentInChildren<EnemyRewardSource>(true)
                : null;
            if (contribution == null || reward == null)
            {
                throw new InvalidOperationException(
                    "Skelet_Warrior run reward data is incomplete.");
            }

            AssertApproximately(
                contribution.ThreatAmount,
                WarriorThreat,
                "Skelet_Warrior Threat");
            if (reward.RunExperienceAmount != WarriorRunExperience)
            {
                throw new InvalidOperationException(
                    $"Skelet_Warrior expected {WarriorRunExperience} RunXP, " +
                    $"got {reward.RunExperienceAmount}.");
            }
        }

        private static RunFlowRuntime RequireRuntime(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                if (roots[index].name != RuntimeObjectName)
                    continue;

                RunFlowRuntime runtime =
                    roots[index].GetComponent<RunFlowRuntime>();
                if (runtime != null)
                    return runtime;
            }

            throw new InvalidOperationException(
                "RunFlowRuntime scene object is missing.");
        }

        private static void RequireEditMode(string operation)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    $"Exit Play Mode before round balance {operation}.");
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

        private static void AssertApproximately(
            float actual,
            float expected,
            string label)
        {
            if (Math.Abs(actual - expected) <= 0.0001f)
                return;

            throw new InvalidOperationException(
                $"{label} failed. Expected {expected}, got {actual}.");
        }
    }
}
