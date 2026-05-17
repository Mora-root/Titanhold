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

        OnDamage?.Invoke(damage);

        Debug.Log(gameObject.name + "Health = " + CurrentHealth.ToString());

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        OnDeath?.Invoke();
    }
}
