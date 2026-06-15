using System;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private CharacterStats characterStats;

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
        ResolveStats();

        CurrentHealth = MaxHealth;
        isDead = false;
    }

    private void OnEnable()
    {
        ResolveStats();

        if (characterStats != null)
            characterStats.OnStatChanged += HandleStatChanged;
    }

    private void OnDisable()
    {
        if (characterStats != null)
            characterStats.OnStatChanged -= HandleStatChanged;
    }

    private void Start()
    {
        NotifyHealthChanged();
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

    private void ResolveStats()
    {
        if (characterStats != null)
            return;

        characterStats = GetComponent<CharacterStats>();
        if (characterStats == null)
            characterStats = GetComponentInParent<CharacterStats>();
    }

}
