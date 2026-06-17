using System;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private CharacterStats characterStats;
    [SerializeField] private PlayerExperience playerExperience;

    public float MaxHealth
    {
        get
        {
            if (characterStats != null)
            {
                float statValue = characterStats.GetValue(StatType.MaxHealth);
                if (statValue > 0f)
                    return statValue;
            }

            return maxHealth;
        }
    }

    public float CurrentHealth { get; private set; }
    public bool IsAlive => CurrentHealth > 0f;

    public event Action<float> OnDamage;
    public event Action<float, float> OnHealthChanged;
    public event Action OnDeath;

    private bool isDead;

    private void Awake()
    {
        ResolveReferences();

        CurrentHealth = MaxHealth;
        isDead = false;
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (characterStats != null)
            characterStats.OnStatChanged += HandleStatChanged;

        if (playerExperience != null)
            playerExperience.OnLevelChanged += HandleLevelChanged;
    }

    private void OnDisable()
    {
        if (characterStats != null)
            characterStats.OnStatChanged -= HandleStatChanged;

        if (playerExperience != null)
            playerExperience.OnLevelChanged -= HandleLevelChanged;
    }

    private void Start()
    {
        NotifyHealthChanged();
    }

    private void Update()
    {
        TickRegeneration(Time.deltaTime);
    }

    public void TakeDamage(float rawDamage)
    {
        if (isDead) return;
        if (rawDamage <= 0f) return;

        float armor = characterStats != null ? characterStats.GetValue(StatType.Armor) : 0f;
        float finalDamage = DamageMitigationCalculator.ApplyArmor(rawDamage, armor);
        if (finalDamage <= 0f) return;

        CurrentHealth -= finalDamage;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, MaxHealth);

        OnDamage?.Invoke(finalDamage);
        NotifyHealthChanged();

        if (CurrentHealth <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        if (amount <= 0f) return;

        CurrentHealth += amount;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, MaxHealth);

        NotifyHealthChanged();
    }

    public void TickRegeneration(float deltaTime)
    {
        if (isDead) return;
        if (deltaTime <= 0f) return;
        if (CurrentHealth >= MaxHealth) return;

        float regenPerSecond = characterStats != null ? characterStats.GetValue(StatType.HPRegen) : 0f;
        if (regenPerSecond <= 0f) return;

        Heal(regenPerSecond * deltaTime);
    }

    public void RestoreFull()
    {
        CurrentHealth = MaxHealth;
        isDead = false;
        NotifyHealthChanged();
    }

    private void HandleStatChanged(StatType type)
    {
        if (type != StatType.MaxHealth)
            return;

        CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, MaxHealth);
        NotifyHealthChanged();
    }

    private void HandleLevelChanged(int level)
    {
        RestoreFull();
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        CurrentHealth = 0f;

        NotifyHealthChanged();
        OnDeath?.Invoke();
    }

    private void NotifyHealthChanged()
    {
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    private void ResolveReferences()
    {
        if (characterStats == null)
        {
            characterStats = GetComponent<CharacterStats>();
        }
        if (characterStats == null)
        {
            characterStats = GetComponentInParent<CharacterStats>();
        }

        if (playerExperience == null)
        {
            playerExperience = GetComponent<PlayerExperience>();
        }
        if (playerExperience == null)
        {
            playerExperience = GetComponentInParent<PlayerExperience>();
        }
    }

}
