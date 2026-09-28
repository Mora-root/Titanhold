using System;
using System.Collections.Generic;
using UnityEngine;

namespace Titanhold.Run
{
    [CreateAssetMenu(
        fileName = "RunChapterFlow",
        menuName = "Titanhold/Run/Chapter Flow")]
    public sealed class RunChapterFlowDefinition : ScriptableObject
    {
        [SerializeField, Min(1)] private int chapterNumber = 1;
        [SerializeField, Min(0.01f)] private float maximumProgress = 100f;
        [SerializeField] private float[] normalizedEscalationThresholds =
        {
            0f,
            0.30f,
            0.60f,
            0.85f
        };
        [SerializeField, Min(0.01f)]
        private float collapseDurationSeconds = 120f;
        [SerializeField, Min(0.01f)]
        private float instabilityStackIntervalSeconds = 20f;
        [SerializeField, Min(0f)]
        private float bossHealthBonusPerStack = 0.10f;
        [SerializeField, Min(0f)]
        private float bossDamageBonusPerStack = 0.05f;

        public int ChapterNumber => chapterNumber;
        public float MaximumProgress => maximumProgress;
        public IReadOnlyList<float> NormalizedEscalationThresholds =>
            normalizedEscalationThresholds ?? Array.Empty<float>();
        public float CollapseDurationSeconds => collapseDurationSeconds;
        public float InstabilityStackIntervalSeconds =>
            instabilityStackIntervalSeconds;
        public float BossHealthBonusPerStack => bossHealthBonusPerStack;
        public float BossDamageBonusPerStack => bossDamageBonusPerStack;
        public bool IsValid => TryCreateConfiguration(out _, out _);

        public bool TryCreateConfiguration(
            out RunChapterFlowConfiguration configuration,
            out string error)
        {
            configuration = null;
            error = string.Empty;
            try
            {
                configuration = new RunChapterFlowConfiguration(
                    chapterNumber,
                    maximumProgress,
                    normalizedEscalationThresholds ?? Array.Empty<float>(),
                    collapseDurationSeconds,
                    instabilityStackIntervalSeconds,
                    bossHealthBonusPerStack,
                    bossDamageBonusPerStack);
                return true;
            }
            catch (ArgumentException exception)
            {
                error =
                    $"Run chapter flow definition '{name}' is invalid: " +
                    $"{exception.ParamName}.";
                return false;
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int configuredChapterNumber,
            float configuredMaximumProgress,
            float[] configuredNormalizedEscalationThresholds,
            float configuredCollapseDurationSeconds,
            float configuredInstabilityStackIntervalSeconds,
            float configuredBossHealthBonusPerStack,
            float configuredBossDamageBonusPerStack)
        {
            chapterNumber = configuredChapterNumber;
            maximumProgress = configuredMaximumProgress;
            normalizedEscalationThresholds =
                configuredNormalizedEscalationThresholds != null
                    ? (float[])configuredNormalizedEscalationThresholds.Clone()
                    : Array.Empty<float>();
            collapseDurationSeconds = configuredCollapseDurationSeconds;
            instabilityStackIntervalSeconds =
                configuredInstabilityStackIntervalSeconds;
            bossHealthBonusPerStack = configuredBossHealthBonusPerStack;
            bossDamageBonusPerStack = configuredBossDamageBonusPerStack;
        }
#endif
    }
}
