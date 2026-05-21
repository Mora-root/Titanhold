using System;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;

    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }

    public bool IsAlive => CurrentHealth > 0f;

    public event Action<float> OnDamage;
    public event Action<float, float> OnHealthChanged;
    public event Action OnDeath;

    private bool isDead;

    private void Awake()
    {
        CurrentHealth = maxHealth;
        isDead = false;
    }

    private void Start()
    {
        NotifyHealthChanged();
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;
        if (damage <= 0f) return;

        CurrentHealth -= damage;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, MaxHealth);

        OnDamage?.Invoke(damage);
        NotifyHealthChanged();

        Debug.Log($"{gameObject.name} took {damage} damage. HP: {CurrentHealth}/{MaxHealth}");

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
        if (isDead) return;

        CurrentHealth = MaxHealth;
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
}
