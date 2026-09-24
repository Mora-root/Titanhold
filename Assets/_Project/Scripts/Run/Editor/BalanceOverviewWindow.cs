using System;
using Titanhold.Enemies;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Run.Editor
{
    public sealed class BalanceOverviewWindow : EditorWindow
    {
        private const string EnemyCatalogPath =
            "Assets/_Project/ScriptableObjects/Enemies/EnemyDefinitionCatalog.asset";
        private const string RoundBalancePath =
            "Assets/_Project/ScriptableObjects/Run/RunRoundBalance_Prototype.asset";
        private const string ProgressionPath =
            "Assets/_Project/ScriptableObjects/Run/RunProgression_Prototype.asset";

        [SerializeField] private EnemyDefinitionCatalog enemyCatalog;
        [SerializeField] private RunRoundBalanceDefinition roundBalance;
        [SerializeField] private RunProgressionDefinition progression;
        [SerializeField] private int selectedRoundNumber = 1;
        [SerializeField] private bool showRoundTable = true;
        [SerializeField] private bool showEnemyTable = true;
        [SerializeField] private bool showLevelTable;

        private Vector2 scroll;
        private Vector2 enemyTableScroll;

        [MenuItem("Tools/Titanhold/Balance Overview")]
        public static void Open()
        {
            BalanceOverviewWindow window = GetWindow<BalanceOverviewWindow>();
            window.titleContent = new GUIContent("Titanhold Balance");
            window.minSize = new Vector2(860f, 480f);
            window.Show();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Titanhold Balance");
            ResolveDefaults();
        }

        private void OnGUI()
        {
            DrawDataSources();
            if (!TryResolveSelectedRound())
            {
                EditorGUILayout.HelpBox(
                    "Select a valid round-balance definition.",
                    MessageType.Warning);
                return;
            }

            if (!BalanceOverviewReportBuilder.TryBuild(
                    enemyCatalog,
                    roundBalance,
                    progression,
                    selectedRoundNumber,
                    out BalanceOverviewReport report,
                    out string error))
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawSummary(report);
            EditorGUILayout.Space(8f);
            showRoundTable = EditorGUILayout.Foldout(
                showRoundTable,
                "Round Curve",
                true);
            if (showRoundTable)
                DrawRoundTable(report);

            EditorGUILayout.Space(6f);
            showEnemyTable = EditorGUILayout.Foldout(
                showEnemyTable,
                $"Enemy Comparison — Round {selectedRoundNumber}",
                true);
            if (showEnemyTable)
                DrawEnemyTable(report);

            EditorGUILayout.Space(6f);
            showLevelTable = EditorGUILayout.Foldout(
                showLevelTable,
                "RunXP Level Curve",
                true);
            if (showLevelTable)
                DrawLevelTable(report);
            EditorGUILayout.EndScrollView();
        }

        private void DrawDataSources()
        {
            EditorGUILayout.LabelField("Data Sources", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                enemyCatalog = (EnemyDefinitionCatalog)EditorGUILayout.ObjectField(
                    "Enemies",
                    enemyCatalog,
                    typeof(EnemyDefinitionCatalog),
                    false);
                if (GUILayout.Button("Defaults", GUILayout.Width(72f)))
                    ResolveDefaults(force: true);
            }

            roundBalance = (RunRoundBalanceDefinition)EditorGUILayout.ObjectField(
                "Rounds",
                roundBalance,
                typeof(RunRoundBalanceDefinition),
                false);
            progression = (RunProgressionDefinition)EditorGUILayout.ObjectField(
                "RunXP",
                progression,
                typeof(RunProgressionDefinition),
                false);

            string[] labels = BuildRoundLabels();
            if (labels.Length > 0)
            {
                int selectedIndex = FindSelectedRoundIndex();
                int nextIndex = EditorGUILayout.Popup(
                    "Preview Round",
                    Mathf.Max(0, selectedIndex),
                    labels);
                if (nextIndex >= 0 && nextIndex < roundBalance.Rounds.Count)
                    selectedRoundNumber = roundBalance.Rounds[nextIndex].RoundNumber;
            }

            EditorGUILayout.Space(6f);
        }

        private static void DrawSummary(BalanceOverviewReport report)
        {
            RunRoundBalanceSnapshot round = report.SelectedRound;
            EditorGUILayout.LabelField("Selected Round", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                Metric("Round", round.RoundNumber.ToString());
                Metric("Meter", round.MaxThreat.ToString("0.##"));
                Metric("Enemy HP", $"×{round.EnemyScaling.HealthMultiplier:0.##}");
                Metric("Enemy Damage", $"×{round.EnemyScaling.DamageMultiplier:0.##}");
                Metric("RunXP", $"×{round.ExperienceMultiplier:0.##}");
                Metric("XP to Max", report.TotalExperienceToMaximumLevel.ToString("N0"));
            }
        }

        private static void DrawRoundTable(BalanceOverviewReport report)
        {
            DrawRow(
                true,
                ("Round", 70f),
                ("Meter", 90f),
                ("HP ×", 90f),
                ("Damage ×", 90f),
                ("RunXP ×", 90f));
            for (int i = 0; i < report.Rounds.Count; i++)
            {
                BalanceRoundRow row = report.Rounds[i];
                DrawRow(
                    false,
                    (row.RoundNumber.ToString(), 70f),
                    (row.MaxThreat.ToString("0.##"), 90f),
                    (row.HealthMultiplier.ToString("0.##"), 90f),
                    (row.DamageMultiplier.ToString("0.##"), 90f),
                    (row.ExperienceMultiplier.ToString("0.##"), 90f));
            }
        }

        private void DrawEnemyTable(BalanceOverviewReport report)
        {
            enemyTableScroll = EditorGUILayout.BeginScrollView(
                enemyTableScroll,
                true,
                false,
                GUILayout.MinHeight(150f));
            DrawRow(
                true,
                ("Enemy ID", 190f),
                ("Prefab", 145f),
                ("HP base", 75f),
                ("HP effective", 88f),
                ("Damage base", 85f),
                ("Damage effective", 100f),
                ("DPS", 65f),
                ("APS", 55f),
                ("Range", 58f),
                ("Move", 55f),
                ("Detect", 58f),
                ("XP base", 62f),
                ("XP effective", 82f),
                ("Threat", 60f),
                ("Kills/meter", 75f),
                ("Instability", 72f),
                ("Loot", 48f));
            for (int i = 0; i < report.Enemies.Count; i++)
            {
                BalanceEnemyRow row = report.Enemies[i];
                DrawRow(
                    false,
                    (row.EnemyId, 190f),
                    (row.PrefabName, 145f),
                    (row.BaseStats.MaximumHealth.ToString("0.##"), 75f),
                    (row.EffectiveHealth.ToString("0.##"), 88f),
                    (row.BaseStats.BaseDamage.ToString("0.##"), 85f),
                    (row.EffectiveDamage.ToString("0.##"), 100f),
                    (row.EffectiveDps.ToString("0.##"), 65f),
                    (row.BaseStats.AttacksPerSecond.ToString("0.##"), 55f),
                    (row.BaseStats.AttackRange.ToString("0.##"), 58f),
                    (row.BaseStats.MovementSpeed.ToString("0.##"), 55f),
                    (row.BaseStats.DetectionRange.ToString("0.##"), 58f),
                    (row.BaseRunExperience.ToString(), 62f),
                    (row.ScaledRunExperience.ToString(), 82f),
                    (row.ThreatAmount > 0f ? row.ThreatAmount.ToString("0.##") : "—", 60f),
                    (row.KillsToFillMeter > 0 ? row.KillsToFillMeter.ToString() : "—", 75f),
                    (row.InstabilityPoints > 0 ? row.InstabilityPoints.ToString() : "—", 72f),
                    (row.HasLootTable ? "Yes" : "No", 48f));
            }

            EditorGUILayout.EndScrollView();
        }

        private static void DrawLevelTable(BalanceOverviewReport report)
        {
            DrawRow(
                true,
                ("Current Level", 100f),
                ("XP to Next", 100f),
                ("Cumulative XP", 120f));
            for (int i = 0; i < report.Levels.Count; i++)
            {
                BalanceLevelRow row = report.Levels[i];
                DrawRow(
                    false,
                    (row.CurrentLevel.ToString(), 100f),
                    (row.ExperienceToNext.ToString(), 100f),
                    (row.CumulativeExperience.ToString("N0"), 120f));
            }
        }

        private static void DrawRow(
            bool header,
            params (string value, float width)[] cells)
        {
            GUIStyle style = header
                ? EditorStyles.miniBoldLabel
                : EditorStyles.miniLabel;
            using (new EditorGUILayout.HorizontalScope(
                       header ? EditorStyles.toolbar : GUIStyle.none))
            {
                for (int i = 0; i < cells.Length; i++)
                    GUILayout.Label(cells[i].value, style, GUILayout.Width(cells[i].width));
            }
        }

        private static void Metric(string label, string value)
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.MinWidth(90f)))
            {
                GUILayout.Label(label, EditorStyles.miniLabel);
                GUILayout.Label(value, EditorStyles.boldLabel);
            }
        }

        private bool TryResolveSelectedRound()
        {
            if (roundBalance == null || roundBalance.Rounds.Count == 0)
                return false;
            if (FindSelectedRoundIndex() >= 0)
                return true;

            RunRoundBalanceEntryDefinition first = roundBalance.Rounds[0];
            if (first == null)
                return false;

            selectedRoundNumber = first.RoundNumber;
            return true;
        }

        private int FindSelectedRoundIndex()
        {
            if (roundBalance == null)
                return -1;
            for (int i = 0; i < roundBalance.Rounds.Count; i++)
            {
                if (roundBalance.Rounds[i] != null &&
                    roundBalance.Rounds[i].RoundNumber == selectedRoundNumber)
                {
                    return i;
                }
            }

            return -1;
        }

        private string[] BuildRoundLabels()
        {
            if (roundBalance == null)
                return Array.Empty<string>();
            string[] labels = new string[roundBalance.Rounds.Count];
            for (int i = 0; i < labels.Length; i++)
            {
                RunRoundBalanceEntryDefinition entry = roundBalance.Rounds[i];
                labels[i] = entry != null
                    ? $"Round {entry.RoundNumber}"
                    : $"Invalid entry {i}";
            }

            return labels;
        }

        private void ResolveDefaults(bool force = false)
        {
            if (force || enemyCatalog == null)
            {
                enemyCatalog = AssetDatabase.LoadAssetAtPath<EnemyDefinitionCatalog>(
                    EnemyCatalogPath);
            }

            if (force || roundBalance == null)
            {
                roundBalance = AssetDatabase.LoadAssetAtPath<RunRoundBalanceDefinition>(
                    RoundBalancePath);
            }

            if (force || progression == null)
            {
                progression = AssetDatabase.LoadAssetAtPath<RunProgressionDefinition>(
                    ProgressionPath);
            }

            TryResolveSelectedRound();
            Repaint();
        }
    }
}
