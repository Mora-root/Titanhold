using UnityEngine;

namespace Titanhold.Session
{
    public readonly struct RunChapterCompletionReward
    {
        public RunChapterCompletionReward(
            string rewardId,
            int chapterNumber,
            int characterExperience,
            int crystals)
        {
            RewardId = rewardId?.Trim() ?? string.Empty;
            ChapterNumber = chapterNumber;
            CharacterExperience = characterExperience;
            Crystals = crystals;
        }

        public string RewardId { get; }
        public int ChapterNumber { get; }
        public int CharacterExperience { get; }
        public int Crystals { get; }
        public bool IsValid =>
            RewardId.Length > 0 &&
            ChapterNumber > 0 &&
            CharacterExperience >= 0 &&
            Crystals >= 0;
    }

    [CreateAssetMenu(
        menuName = "Titanhold/Run/Chapter Completion Reward",
        fileName = "RunChapterCompletionReward")]
    public sealed class RunChapterCompletionRewardDefinition :
        ScriptableObject
    {
        [SerializeField] private string rewardId =
            "reward:chapter:1:prototype";
        [SerializeField, Min(1)] private int chapterNumber = 1;
        [SerializeField, Min(0)] private int characterExperience = 1200;
        [SerializeField, Min(0)] private int crystals = 60;

        public bool TryCreateReward(
            out RunChapterCompletionReward reward,
            out string error)
        {
            reward = new RunChapterCompletionReward(
                rewardId,
                chapterNumber,
                characterExperience,
                crystals);
            if (reward.IsValid)
            {
                error = string.Empty;
                return true;
            }

            error = "Chapter completion reward is invalid.";
            return false;
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string configuredRewardId,
            int configuredChapterNumber,
            int configuredCharacterExperience,
            int configuredCrystals)
        {
            rewardId = configuredRewardId?.Trim() ?? string.Empty;
            chapterNumber = configuredChapterNumber;
            characterExperience = configuredCharacterExperience;
            crystals = configuredCrystals;
        }
#endif

        private void OnValidate()
        {
            rewardId = rewardId?.Trim() ?? string.Empty;
            chapterNumber = Mathf.Max(1, chapterNumber);
            characterExperience = Mathf.Max(0, characterExperience);
            crystals = Mathf.Max(0, crystals);
        }
    }
}
