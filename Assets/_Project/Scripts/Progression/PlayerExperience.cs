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

    private int GetExperienceToNextLevel()
    {
        int levelIndex = Mathf.Max(0, currentLevel - 1);
        int experienceToNextLevel = baseExperienceToNextLevel + experienceIncreasePerLevel * levelIndex;

        return Mathf.Max(1, experienceToNextLevel);
    }
}
