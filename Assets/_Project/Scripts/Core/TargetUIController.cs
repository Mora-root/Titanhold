using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays the UI when selecting a target
/// </summary>
public class TargetUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TargetSelection targetSelection;

    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text healthText;

    private Health currentHealth;

    private void Awake()
    {
        Hide();
    }

    private void OnEnable()
    {
        targetSelection.OnSelected += HandleSelected;
        targetSelection.OnCleared += HandleCleared;
    }

    private void OnDisable()
    {
        targetSelection.OnSelected -= HandleSelected;
        targetSelection.OnCleared -= HandleCleared;

        UnsubscribeFromHealth();
    }

    private void HandleSelected(ISelectable selectable)
    {
        UnsubscribeFromHealth();

        if (selectable == null || !selectable.IsSelectable)
        {
            Hide();
            return;
        }

        var selectableComponent = selectable as MonoBehaviour;

        if (selectableComponent == null)
        {
            Hide();
            return;
        }
        // Getting information for the UI
        TargetInfo info = selectableComponent.GetComponentInParent<TargetInfo>();
        currentHealth = selectableComponent.GetComponentInParent<Health>();

        if (info == null)
        {
            Hide();
            return;
        }

        Show();

        nameText.text = info.DisplayName;
        levelText.text = $"Lv {info.Level}";

        if (iconImage != null)
        {
            iconImage.sprite = info.Icon;
            iconImage.enabled = info.Icon != null;
        }

        if (currentHealth != null)
        {
            currentHealth.OnHealthChanged += UpdateHealth;
            currentHealth.OnDeath += HandleDeath;

            UpdateHealth(currentHealth.CurrentHealth, currentHealth.MaxHealth);
        }
        else
        {
            if (healthSlider != null)
                healthSlider.gameObject.SetActive(false);

            if (healthText != null)
                healthText.gameObject.SetActive(false);
        }
    }

    private void HandleCleared()
    {
        UnsubscribeFromHealth();
        Hide();
    }

    private void UpdateHealth(float current, float max)
    {
        if (healthSlider != null)
        {
            healthSlider.gameObject.SetActive(true);
            healthSlider.value = max > 0 ? current / max : 0f;
        }

        if (healthText != null)
        {
            healthText.gameObject.SetActive(true);
            healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }
    }

    private void HandleDeath()
    {
        targetSelection.Clear();
    }

    private void Show()
    {
        if (root != null)
            root.SetActive(true);
    }

    private void Hide()
    {
        if (root != null)
            root.SetActive(false);
    }

    private void UnsubscribeFromHealth()
    {
        if (currentHealth == null) return;

        currentHealth.OnHealthChanged -= UpdateHealth;
        currentHealth.OnDeath -= HandleDeath;
        currentHealth = null;
    }
}
