using System;
using System.Globalization;
using TMPro;
using Titanhold.Run;
using UnityEngine;
using UnityEngine.UI;

namespace Titanhold.UI.Run
{
    [DisallowMultipleComponent]
    public sealed class RunChapterFlowHudView : MonoBehaviour
    {
        [SerializeField] private Slider progressSlider;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private TMP_Text stageText;
        [SerializeField] private TMP_Text collapseText;
        [SerializeField] private TMP_Text instabilityText;
        [SerializeField] private TMP_Text bossScalingText;
        [SerializeField] private TMP_Text portalStatusText;

        public bool IsConfigured =>
            progressSlider != null &&
            titleText != null &&
            progressText != null &&
            stageText != null &&
            collapseText != null &&
            instabilityText != null &&
            bossScalingText != null &&
            portalStatusText != null;
        public RunChapterFlowPresentationSnapshot LastSnapshot { get; private set; }
        public bool HasRenderedSnapshot { get; private set; }
        public Slider ProgressSlider => progressSlider;
        public TMP_Text TitleText => titleText;
        public TMP_Text ProgressText => progressText;
        public TMP_Text StageText => stageText;
        public TMP_Text CollapseText => collapseText;
        public TMP_Text InstabilityText => instabilityText;
        public TMP_Text BossScalingText => bossScalingText;
        public TMP_Text PortalStatusText => portalStatusText;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            Slider configuredProgressSlider,
            TMP_Text configuredTitleText,
            TMP_Text configuredProgressText,
            TMP_Text configuredStageText,
            TMP_Text configuredCollapseText,
            TMP_Text configuredInstabilityText,
            TMP_Text configuredBossScalingText,
            TMP_Text configuredPortalStatusText)
        {
            progressSlider = configuredProgressSlider;
            titleText = configuredTitleText;
            progressText = configuredProgressText;
            stageText = configuredStageText;
            collapseText = configuredCollapseText;
            instabilityText = configuredInstabilityText;
            bossScalingText = configuredBossScalingText;
            portalStatusText = configuredPortalStatusText;
        }
#endif

        public bool Render(RunChapterFlowPresentationSnapshot snapshot)
        {
            if (!IsConfigured)
                return false;

            LastSnapshot = snapshot;
            HasRenderedSnapshot = true;
            progressSlider.SetValueWithoutNotify(snapshot.NormalizedProgress);
            titleText.text = string.Format(
                CultureInfo.InvariantCulture,
                "CHAPTER {0} · {1}",
                snapshot.ChapterNumber,
                FormatPhase(snapshot.Phase));
            progressText.text = string.Format(
                CultureInfo.InvariantCulture,
                "{0:0}/{1:0}",
                snapshot.CurrentProgress,
                snapshot.MaximumProgress);
            stageText.text = string.Format(
                CultureInfo.InvariantCulture,
                "STAGE {0}/{1}",
                snapshot.CurrentEscalationStage,
                snapshot.EscalationStageCount);
            instabilityText.text = string.Format(
                CultureInfo.InvariantCulture,
                "INSTABILITY {0}/{1}",
                snapshot.CurrentInstabilityStacks,
                snapshot.MaximumInstabilityStacks);
            bossScalingText.text = string.Format(
                CultureInfo.InvariantCulture,
                "BOSS HP ×{0:0.00} · DMG ×{1:0.00}",
                snapshot.BossHealthMultiplier,
                snapshot.BossDamageMultiplier);

            collapseText.gameObject.SetActive(snapshot.IsBossPortalAvailable);
            collapseText.text = snapshot.IsBossPortalAvailable
                ? string.Format(
                    CultureInfo.InvariantCulture,
                    "RIFT COLLAPSE {0:0}s",
                    Math.Ceiling(snapshot.CollapseTimeRemainingSeconds))
                : string.Empty;

            bool showPortalStatus =
                snapshot.IsBossPortalAvailable ||
                snapshot.BossTransitionWasForced;
            portalStatusText.gameObject.SetActive(showPortalStatus);
            portalStatusText.text = snapshot.BossTransitionWasForced
                ? "FORCED BOSS TRANSITION"
                : snapshot.IsBossPortalAvailable
                    ? "BOSS PORTAL AVAILABLE"
                    : string.Empty;
            return true;
        }

        private static string FormatPhase(RunChapterPhase phase)
        {
            return phase switch
            {
                RunChapterPhase.RiftCollapse => "RIFT COLLAPSE",
                RunChapterPhase.TransitionToBoss => "BOSS TRANSITION",
                RunChapterPhase.Boss => "BOSS",
                RunChapterPhase.Reward => "REWARD",
                RunChapterPhase.Completed => "COMPLETE",
                _ => "EXPLORATION"
            };
        }
    }
}
