using System;
using System.Collections.Generic;

namespace Titanhold.Run
{
    public enum RunChapterPhase
    {
        Exploration,
        RiftCollapse,
        TransitionToBoss,
        Boss,
        Reward,
        Completed
    }

    public enum RunChapterFlowError
    {
        None,
        InvalidProgress,
        InvalidSimulationTime,
        SimulationTimeMovedBackwards,
        InvalidPhase
    }

    public readonly struct RunChapterBossScalingSnapshot
    {
        public RunChapterBossScalingSnapshot(
            int instabilityStacks,
            float healthMultiplier,
            float damageMultiplier)
        {
            if (instabilityStacks < 0)
                throw new ArgumentOutOfRangeException(nameof(instabilityStacks));

            if (!IsFinitePositive(healthMultiplier))
                throw new ArgumentOutOfRangeException(nameof(healthMultiplier));

            if (!IsFinitePositive(damageMultiplier))
                throw new ArgumentOutOfRangeException(nameof(damageMultiplier));

            InstabilityStacks = instabilityStacks;
            HealthMultiplier = healthMultiplier;
            DamageMultiplier = damageMultiplier;
        }

        public int InstabilityStacks { get; }
        public float HealthMultiplier { get; }
        public float DamageMultiplier { get; }

        private static bool IsFinitePositive(float value)
        {
            return value > 0f &&
                   !float.IsNaN(value) &&
                   !float.IsInfinity(value);
        }
    }

    public sealed class RunChapterFlowConfiguration
    {
        private readonly float[] escalationThresholds;

        public RunChapterFlowConfiguration(
            int chapterNumber,
            float maximumProgress,
            IReadOnlyList<float> normalizedEscalationThresholds,
            double collapseDurationSeconds,
            double instabilityStackIntervalSeconds,
            float bossHealthBonusPerStack,
            float bossDamageBonusPerStack)
        {
            if (chapterNumber <= 0)
                throw new ArgumentOutOfRangeException(nameof(chapterNumber));

            if (!IsFinitePositive(maximumProgress))
                throw new ArgumentOutOfRangeException(nameof(maximumProgress));

            if (!IsFinitePositive(collapseDurationSeconds))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(collapseDurationSeconds));
            }

            if (!IsFinitePositive(instabilityStackIntervalSeconds))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(instabilityStackIntervalSeconds));
            }

            if (instabilityStackIntervalSeconds > collapseDurationSeconds)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(instabilityStackIntervalSeconds));
            }

            if (!IsFiniteNonNegative(bossHealthBonusPerStack))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(bossHealthBonusPerStack));
            }

            if (!IsFiniteNonNegative(bossDamageBonusPerStack))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(bossDamageBonusPerStack));
            }

            escalationThresholds = CopyAndValidateThresholds(
                normalizedEscalationThresholds);
            ChapterNumber = chapterNumber;
            MaximumProgress = maximumProgress;
            CollapseDurationSeconds = collapseDurationSeconds;
            InstabilityStackIntervalSeconds = instabilityStackIntervalSeconds;
            BossHealthBonusPerStack = bossHealthBonusPerStack;
            BossDamageBonusPerStack = bossDamageBonusPerStack;
        }

        public int ChapterNumber { get; }
        public float MaximumProgress { get; }
        public IReadOnlyList<float> EscalationThresholds =>
            escalationThresholds;
        public double CollapseDurationSeconds { get; }
        public double InstabilityStackIntervalSeconds { get; }
        public float BossHealthBonusPerStack { get; }
        public float BossDamageBonusPerStack { get; }
        public int MaximumInstabilityStacks =>
            (int)Math.Floor(
                CollapseDurationSeconds /
                InstabilityStackIntervalSeconds);

        public static RunChapterFlowConfiguration CreatePrototypeDefaults()
        {
            return new RunChapterFlowConfiguration(
                chapterNumber: 1,
                maximumProgress: 100f,
                normalizedEscalationThresholds: new[]
                {
                    0f,
                    0.30f,
                    0.60f,
                    0.85f
                },
                collapseDurationSeconds: 120d,
                instabilityStackIntervalSeconds: 20d,
                bossHealthBonusPerStack: 0.10f,
                bossDamageBonusPerStack: 0.05f);
        }

        internal int ResolveEscalationStage(float progress)
        {
            float normalized = Math.Min(1f, progress / MaximumProgress);
            int resolvedIndex = 0;
            for (int i = escalationThresholds.Length - 1; i >= 0; i--)
            {
                if (normalized >= escalationThresholds[i])
                {
                    resolvedIndex = i;
                    break;
                }
            }

            return resolvedIndex + 1;
        }

        internal RunChapterBossScalingSnapshot CreateBossScaling(
            int instabilityStacks)
        {
            int clampedStacks = Math.Min(
                Math.Max(0, instabilityStacks),
                MaximumInstabilityStacks);
            return new RunChapterBossScalingSnapshot(
                clampedStacks,
                CalculateMultiplier(clampedStacks, BossHealthBonusPerStack),
                CalculateMultiplier(clampedStacks, BossDamageBonusPerStack));
        }

        private static float[] CopyAndValidateThresholds(
            IReadOnlyList<float> thresholds)
        {
            if (thresholds == null || thresholds.Count == 0)
            {
                throw new ArgumentException(
                    "At least one escalation threshold is required.",
                    nameof(thresholds));
            }

            float[] copied = new float[thresholds.Count];
            float previous = -1f;
            for (int i = 0; i < thresholds.Count; i++)
            {
                float threshold = thresholds[i];
                if (float.IsNaN(threshold) ||
                    float.IsInfinity(threshold) ||
                    threshold < 0f ||
                    threshold >= 1f ||
                    threshold <= previous)
                {
                    throw new ArgumentException(
                        "Escalation thresholds must be unique, ordered values in [0, 1).",
                        nameof(thresholds));
                }

                if (i == 0 && threshold != 0f)
                {
                    throw new ArgumentException(
                        "The first escalation threshold must be zero.",
                        nameof(thresholds));
                }

                copied[i] = threshold;
                previous = threshold;
            }

            return copied;
        }

        private static float CalculateMultiplier(int count, float bonus)
        {
            double multiplier = 1d + (double)count * bonus;
            return multiplier >= float.MaxValue
                ? float.MaxValue
                : (float)multiplier;
        }

        private static bool IsFinitePositive(float value)
        {
            return value > 0f &&
                   !float.IsNaN(value) &&
                   !float.IsInfinity(value);
        }

        private static bool IsFinitePositive(double value)
        {
            return value > 0d &&
                   !double.IsNaN(value) &&
                   !double.IsInfinity(value);
        }

        private static bool IsFiniteNonNegative(float value)
        {
            return value >= 0f &&
                   !float.IsNaN(value) &&
                   !float.IsInfinity(value);
        }
    }

    public sealed class RunChapterFlowState
    {
        private readonly RunChapterFlowConfiguration configuration;

        internal RunChapterFlowState(
            RunChapterFlowConfiguration configuration)
        {
            this.configuration = configuration ??
                throw new ArgumentNullException(nameof(configuration));
            Phase = RunChapterPhase.Exploration;
            EscalationStage = configuration.ResolveEscalationStage(0f);
            BossScaling = configuration.CreateBossScaling(0);
        }

        public int ChapterNumber => configuration.ChapterNumber;
        public RunChapterPhase Phase { get; private set; }
        public float CurrentProgress { get; private set; }
        public float MaximumProgress => configuration.MaximumProgress;
        public float NormalizedProgress => CurrentProgress / MaximumProgress;
        public bool IsProgressFull => CurrentProgress >= MaximumProgress;
        public int EscalationStage { get; private set; }
        public int EscalationStageCount =>
            configuration.EscalationThresholds.Count;
        public double LastSimulationTime { get; private set; }
        public double CollapseStartedAt { get; private set; }
        public double CollapseDeadline =>
            CollapseStartedAt + configuration.CollapseDurationSeconds;
        public double CollapseTimeRemaining =>
            Phase == RunChapterPhase.RiftCollapse
                ? Math.Max(0d, CollapseDeadline - LastSimulationTime)
                : 0d;
        public int InstabilityStacks { get; private set; }
        public RunChapterBossScalingSnapshot BossScaling { get; private set; }
        public bool BossTransitionWasForced { get; private set; }

        internal bool TryObserveTime(
            double simulationTime,
            out RunChapterFlowError error)
        {
            error = RunChapterFlowError.None;
            if (simulationTime < 0d ||
                double.IsNaN(simulationTime) ||
                double.IsInfinity(simulationTime))
            {
                error = RunChapterFlowError.InvalidSimulationTime;
                return false;
            }

            if (simulationTime < LastSimulationTime)
            {
                error = RunChapterFlowError.SimulationTimeMovedBackwards;
                return false;
            }

            LastSimulationTime = simulationTime;
            return true;
        }

        internal float AddProgress(float amount)
        {
            float previous = CurrentProgress;
            double total = (double)CurrentProgress + amount;
            CurrentProgress = total >= MaximumProgress
                ? MaximumProgress
                : (float)total;
            EscalationStage = configuration.ResolveEscalationStage(
                CurrentProgress);
            return CurrentProgress - previous;
        }

        internal void BeginCollapse(double simulationTime)
        {
            CollapseStartedAt = simulationTime;
            InstabilityStacks = 0;
            BossScaling = configuration.CreateBossScaling(0);
            Phase = RunChapterPhase.RiftCollapse;
        }

        internal bool RefreshCollapse()
        {
            if (Phase != RunChapterPhase.RiftCollapse)
                return false;

            double elapsed = Math.Max(
                0d,
                LastSimulationTime - CollapseStartedAt);
            int resolvedStacks = Math.Min(
                configuration.MaximumInstabilityStacks,
                (int)Math.Floor(
                    elapsed /
                    configuration.InstabilityStackIntervalSeconds));
            if (resolvedStacks == InstabilityStacks)
                return false;

            InstabilityStacks = resolvedStacks;
            BossScaling = configuration.CreateBossScaling(resolvedStacks);
            return true;
        }

        internal void BeginBossTransition(bool forced)
        {
            BossTransitionWasForced = forced;
            Phase = RunChapterPhase.TransitionToBoss;
        }

        internal void SetPhase(RunChapterPhase phase)
        {
            Phase = phase;
        }
    }

    public readonly struct RunChapterFlowResult
    {
        private RunChapterFlowResult(
            bool success,
            RunChapterFlowError error,
            RunChapterPhase previousPhase,
            RunChapterPhase currentPhase,
            float progressAdded,
            bool escalationStageChanged,
            bool collapseStarted,
            bool forcedBossTransition)
        {
            Success = success;
            Error = error;
            PreviousPhase = previousPhase;
            CurrentPhase = currentPhase;
            ProgressAdded = progressAdded;
            EscalationStageChanged = escalationStageChanged;
            CollapseStarted = collapseStarted;
            ForcedBossTransition = forcedBossTransition;
        }

        public bool Success { get; }
        public RunChapterFlowError Error { get; }
        public RunChapterPhase PreviousPhase { get; }
        public RunChapterPhase CurrentPhase { get; }
        public float ProgressAdded { get; }
        public bool EscalationStageChanged { get; }
        public bool CollapseStarted { get; }
        public bool ForcedBossTransition { get; }

        internal static RunChapterFlowResult Succeeded(
            RunChapterPhase previousPhase,
            RunChapterPhase currentPhase,
            float progressAdded = 0f,
            bool escalationStageChanged = false,
            bool collapseStarted = false,
            bool forcedBossTransition = false)
        {
            return new RunChapterFlowResult(
                true,
                RunChapterFlowError.None,
                previousPhase,
                currentPhase,
                progressAdded,
                escalationStageChanged,
                collapseStarted,
                forcedBossTransition);
        }

        internal static RunChapterFlowResult Failed(
            RunChapterFlowError error,
            RunChapterPhase phase)
        {
            return new RunChapterFlowResult(
                false,
                error,
                phase,
                phase,
                0f,
                false,
                false,
                false);
        }
    }
}
