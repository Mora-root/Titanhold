using System;
using UnityEngine;

public sealed class PlayerExperience : MonoBehaviour
{
    [SerializeField] private int currentExperience;

    public int CurrentExperience => currentExperience;

    public event Action<int> OnExperienceChanged;

    public void AddExperience(int amount)
    {
        if (amount <= 0)
            return;

        currentExperience += amount;
        OnExperienceChanged?.Invoke(currentExperience);
    }
}
