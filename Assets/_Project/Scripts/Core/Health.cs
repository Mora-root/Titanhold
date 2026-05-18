using System;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;

    public float CurrentHealth { get; private set; }
    public bool IsAlive => CurrentHealth > 0;

    public event Action OnDeath;
    public event Action<float> OnDamage;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (!IsAlive) return;

        CurrentHealth -= damage;

        Debug.Log($"{gameObject.name} took {damage} damage. Current health: {CurrentHealth}");

        OnDamage?.Invoke(damage);

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        CurrentHealth = 0;
        OnDeath?.Invoke();
    }
}
