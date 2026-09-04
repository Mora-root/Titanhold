using TMPro;
using UnityEngine;

namespace Titanhold.UI.Run
{
    [DisallowMultipleComponent]
    public sealed class RunProgressionHudView : MonoBehaviour
    {
        [SerializeField] private TMP_Text progressionText;
        [SerializeField] private GameObject levelUpRoot;
        [SerializeField] private TMP_Text levelUpText;

        public TMP_Text ProgressionText => progressionText;
        public GameObject LevelUpRoot => levelUpRoot;
        public TMP_Text LevelUpText => levelUpText;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            TMP_Text configuredProgressionText,
            GameObject configuredLevelUpRoot,
            TMP_Text configuredLevelUpText)
        {
            progressionText = configuredProgressionText;
            levelUpRoot = configuredLevelUpRoot;
            levelUpText = configuredLevelUpText;
        }
#endif

        private void Awake()
        {
            HideLevelUp();
        }

        public void RenderProgression(
            int level,
            int experience,
            int experienceRequired,
            bool isMaximumLevel,
            int gold)
        {
            if (progressionText == null)
                return;

            string experienceLabel = isMaximumLevel
                ? "XP MAX"
                : $"XP {experience} / {experienceRequired}";
            progressionText.text =
                $"RUN LV {level}  |  {experienceLabel}  |  GOLD {gold}";
        }

        public void ShowLevelUp(int level, int levelsGained)
        {
            if (levelUpText != null)
            {
                levelUpText.text = levelsGained > 1
                    ? $"RUN LEVEL UP!  LV {level} (+{levelsGained})"
                    : $"RUN LEVEL UP!  LV {level}";
            }

            if (levelUpRoot != null)
                levelUpRoot.SetActive(true);
        }

        public void HideLevelUp()
        {
            if (levelUpRoot != null)
                levelUpRoot.SetActive(false);
        }
    }
}
