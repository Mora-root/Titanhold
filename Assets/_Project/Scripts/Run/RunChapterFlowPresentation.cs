using System;

namespace Titanhold.Run
{
    public readonly struct RunChapterFlowPresentationSnapshot
    {
        internal RunChapterFlowPresentationSnapshot(
            int chapterNumber,
            float currentProgress,
            float maximumProgress,
            float normalizedProgress,
            int currentEscalationStage,
            int escalationStageCount,
            RunChapterPhase phase,
            bool isBossPortalAvailable,
            bool isExitPortalAvailable,
            double collapseDurationSeconds,
            double collapseTimeRemainingSeconds,
            int currentInstabilityStacks,
            int maximumInstabilityStacks,
            float bossHealthMultiplier,
            float bossDamageMultiplier,
            bool bossTransitionWasForced)
        {
            ChapterNumber = chapterNumber;
            CurrentProgress = currentProgress;
            MaximumProgress = maximumProgress;
            NormalizedProgress = normalizedProgress;
            CurrentEscalationStage = currentEscalationStage;
            EscalationStageCount = escalationStageCount;
            Phase = phase;
            IsBossPortalAvailable = isBossPortalAvailable;
            IsExitPortalAvailable = isExitPortalAvailable;
            CollapseDurationSeconds = collapseDurationSeconds;
            CollapseTimeRemainingSeconds = collapseTimeRemainingSeconds;
            CurrentInstabilityStacks = currentInstabilityStacks;
            MaximumInstabilityStacks = maximumInstabilityStacks;
            BossHealthMultiplier = bossHealthMultiplier;
            BossDamageMultiplier = bossDamageMultiplier;
            BossTransitionWasForced = bossTransitionWasForced;
        }

        public int ChapterNumber { get; }
        public float CurrentProgress { get; }
        public float MaximumProgress { get; }
        public float NormalizedProgress { get; }
        public int CurrentEscalationStage { get; }
        public int EscalationStageCount { get; }
        public RunChapterPhase Phase { get; }
        public bool IsBossPortalAvailable { get; }
        public bool IsExitPortalAvailable { get; }
        public double CollapseDurationSeconds { get; }
        public double CollapseTimeRemainingSeconds { get; }
        public int CurrentInstabilityStacks { get; }
        public int MaximumInstabilityStacks { get; }
        public float BossHealthMultiplier { get; }
        public float BossDamageMultiplier { get; }
        public bool BossTransitionWasForced { get; }
    }

    public sealed class RunChapterFlowPresentationProjection
    {
        private readonly RunChapterFlowState state;

        public RunChapterFlowPresentationProjection(
            RunChapterFlowState state)
        {
            this.state = state ??
                throw new ArgumentNullException(nameof(state));
        }

        public RunChapterFlowPresentationSnapshot Capture()
        {
            RunChapterBossScalingSnapshot bossScaling = state.BossScaling;
            return new RunChapterFlowPresentationSnapshot(
                state.ChapterNumber,
                state.CurrentProgress,
                state.MaximumProgress,
                state.NormalizedProgress,
                state.EscalationStage,
                state.EscalationStageCount,
                state.Phase,
                state.Phase == RunChapterPhase.RiftCollapse,
                state.Phase == RunChapterPhase.Reward,
                state.CollapseDurationSeconds,
                state.CollapseTimeRemaining,
                state.InstabilityStacks,
                state.MaximumInstabilityStacks,
                bossScaling.HealthMultiplier,
                bossScaling.DamageMultiplier,
                state.BossTransitionWasForced);
        }
    }
}
