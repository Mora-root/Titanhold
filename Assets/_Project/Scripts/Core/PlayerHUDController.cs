using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUDController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInfo playerInfo;
    [SerializeField] private Health playerHealth;
    [SerializeField] private PlayerResource playerResource;

    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Player Info")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;

    [Header("Health")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text healthText;

    [Header("Resource")]
    [SerializeField] private Slider resourceSlider;
    [SerializeField] private TMP_Text resourceText;

    private void Awake()
    {
        if (root != null)
            root.SetActive(true);
    }

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += UpdateHealth;
            playerHealth.OnDeath += HandleDeath;
        }

        if (playerResource != null)
        {
            playerResource.OnResourceChanged += UpdateResource;
        }
    }

    private void Start()
    {
        UpdateStaticInfo();

        if (playerHealth != null)
            UpdateHealth(playerHealth.CurrentHealth, playerHealth.MaxHealth);

        if (playerResource != null)
            UpdateResource(playerResource.CurrentResource, playerResource.MaxResource);
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateHealth;
            playerHealth.OnDeath -= HandleDeath;
        }

        if (playerResource != null)
        {
            playerResource.OnResourceChanged -= UpdateResource;
        }
    }

    private void UpdateStaticInfo()
    {
        if (playerInfo == null) return;

        if (nameText != null)
            nameText.text = playerInfo.PlayerName;

        if (levelText != null)
            levelText.text = $"Lv {playerInfo.Level}";
    }

    private void UpdateHealth(float current, float max)
    {
        if (healthSlider != null)
            healthSlider.value = max > 0f ? current / max : 0f;

        if (healthText != null)
            healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }

    private void UpdateResource(float current, float max)
    {
        if (resourceSlider != null)
            resourceSlider.value = max > 0f ? current / max : 0f;

        if (resourceText != null)
            resourceText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }

    private void HandleDeath()
    {
        Debug.Log("Player died");
    }
}
