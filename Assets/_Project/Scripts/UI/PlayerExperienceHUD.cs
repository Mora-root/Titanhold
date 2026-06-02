using TMPro;
using UnityEngine;

public sealed class PlayerExperienceHUD : MonoBehaviour
{
    [SerializeField] private PlayerExperience playerExperience;
    [SerializeField] private TMP_Text experienceText;

    private void Awake()
    {
        playerExperience ??= FindAnyObjectByType<PlayerExperience>();
    }

    private void OnEnable()
    {
        if (playerExperience != null)
        {
            playerExperience.OnExperienceChanged += HandleExperienceChanged;
            playerExperience.OnLevelChanged += HandleLevelChanged;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (playerExperience != null)
        {
            playerExperience.OnExperienceChanged -= HandleExperienceChanged;
            playerExperience.OnLevelChanged -= HandleLevelChanged;
        }
    }

    public void Refresh()
    {
        if (experienceText == null)
            return;

        if (playerExperience != null)
        {
            experienceText.text = $"Lv {playerExperience.CurrentLevel} | XP: {playerExperience.CurrentExperience} / {playerExperience.ExperienceToNextLevel}";
        }
        else
        {
            experienceText.text = "XP: Missing";
        }
    }

    private void HandleExperienceChanged(int currentExperience)
    {
        Refresh();
    }

    private void HandleLevelChanged(int currentLevel)
    {
        Refresh();
    }
}
