using System;
using UnityEngine;

public sealed class PlayerExperience : MonoBehaviour
{
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int currentExperience;
    [SerializeField] private int baseExperienceToNextLevel = 100;
    [SerializeField] private int experienceIncreasePerLevel = 50;

    public int CurrentLevel => currentLevel;
    public int CurrentExperience => currentExperience;
    public int ExperienceToNextLevel => GetExperienceToNextLevel();

    public event Action<int> OnExperienceChanged;
    public event Action<int> OnLevelChanged;

    public void AddExperience(int amount)
    {
        if (amount <= 0)
            return;

        currentExperience += amount;

        while (currentExperience >= ExperienceToNextLevel)
        {
            currentExperience -= ExperienceToNextLevel;
            currentLevel++;
            OnLevelChanged?.Invoke(currentLevel);
        }

        OnExperienceChanged?.Invoke(currentExperience);
    }

    internal bool CanRestoreState(int level, int experience)
    {
        return level >= 1 &&
               experience >= 0 &&
               experience < GetExperienceToNextLevel(level);
    }

    internal void RestoreState(int level, int experience)
    {
        if (!CanRestoreState(level, experience))
            throw new ArgumentOutOfRangeException(nameof(experience));

        bool levelChanged = currentLevel != level;
        bool experienceChanged = currentExperience != experience;
        currentLevel = level;
        currentExperience = experience;

        if (levelChanged)
            OnLevelChanged?.Invoke(currentLevel);

        if (experienceChanged || levelChanged)
            OnExperienceChanged?.Invoke(currentExperience);
    }

    internal bool TryCalculateStateAfterGain(
        int amount,
        out int resultingLevel,
        out int resultingExperience)
    {
        resultingLevel = currentLevel;
        resultingExperience = currentExperience;
        if (amount < 0)
            return false;

        long experience = (long)currentExperience + amount;
        int level = currentLevel;
        while (true)
        {
            int required = GetExperienceToNextLevel(level);
            if (experience < required)
                break;

            experience -= required;
            if (level == int.MaxValue)
                return false;

            level++;
        }

        resultingLevel = level;
        resultingExperience = (int)experience;
        return true;
    }

    private int GetExperienceToNextLevel()
    {
        return GetExperienceToNextLevel(currentLevel);
    }

    private int GetExperienceToNextLevel(int level)
    {
        int levelIndex = Mathf.Max(0, level - 1);
        long experienceToNextLevel =
            baseExperienceToNextLevel +
            (long)experienceIncreasePerLevel * levelIndex;

        return (int)Math.Max(1L, Math.Min(int.MaxValue, experienceToNextLevel));
    }
}
