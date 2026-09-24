using System;
using Titanhold.Enemies;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Run.Editor
{
    public static class BalanceOverviewValidationRunner
    {
        private const string EnemyCatalogPath =
            "Assets/_Project/ScriptableObjects/Enemies/EnemyDefinitionCatalog.asset";
        private const string RoundBalancePath =
            "Assets/_Project/ScriptableObjects/Run/RunRoundBalance_Prototype.asset";
        private const string ProgressionPath =
            "Assets/_Project/ScriptableObjects/Run/RunProgression_Prototype.asset";

        [MenuItem("Tools/Titanhold/Validate Balance Overview")]
        public static void Validate()
        {
            try
            {
                EnemyDefinitionCatalog enemies =
                    RequireAsset<EnemyDefinitionCatalog>(EnemyCatalogPath);
                RunRoundBalanceDefinition rounds =
                    RequireAsset<RunRoundBalanceDefinition>(RoundBalancePath);
                RunProgressionDefinition progression =
                    RequireAsset<RunProgressionDefinition>(ProgressionPath);
                Assert(rounds.Rounds.Count > 0, "Round balance is empty.");

                for (int i = 0; i < rounds.Rounds.Count; i++)
                {
                    RunRoundBalanceEntryDefinition entry = rounds.Rounds[i];
                    Assert(entry != null, $"Round entry {i} is missing.");
                    Assert(BalanceOverviewReportBuilder.TryBuild(
                            enemies,
                            rounds,
                            progression,
                            entry.RoundNumber,
                            out BalanceOverviewReport report,
                            out string error),
                        error);
                    Assert(report.Rounds.Count == rounds.Rounds.Count,
                        "Round overview lost authored entries.");
                    Assert(report.Enemies.Count == enemies.Definitions.Count,
                        "Enemy overview lost catalog entries.");
                    Assert(report.Levels.Count == progression.MaximumLevel - 1,
                        "RunXP overview has an invalid level count.");
                    Assert(report.TotalExperienceToMaximumLevel > 0,
                        "RunXP overview has no cumulative requirement.");

                    for (int enemyIndex = 0;
                         enemyIndex < report.Enemies.Count;
                         enemyIndex++)
                    {
                        BalanceEnemyRow enemy = report.Enemies[enemyIndex];
                        Assert(enemy.EffectiveHealth > 0f &&
                               enemy.EffectiveDamage >= 0f &&
                               enemy.EffectiveDps >= 0f,
                            $"Enemy '{enemy.EnemyId}' has invalid effective combat values.");
                        Assert(Mathf.Approximately(
                                enemy.EffectiveHealth,
                                enemy.BaseStats.MaximumHealth *
                                report.SelectedRound.EnemyScaling.HealthMultiplier),
                            $"Enemy '{enemy.EnemyId}' health projection is inconsistent.");
                        Assert(Mathf.Approximately(
                                enemy.EffectiveDamage,
                                enemy.BaseStats.BaseDamage *
                                report.SelectedRound.EnemyScaling.DamageMultiplier),
                            $"Enemy '{enemy.EnemyId}' damage projection is inconsistent.");
                    }
                }

                Debug.Log("Balance overview validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"Balance overview validation failed: {exception}");
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

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
