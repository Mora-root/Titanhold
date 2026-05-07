using System;
using UnityEngine;

/// <summary>
/// Represents a basic enemy with health and death.
/// </summary>
public class Enemy : MonoBehaviour, IDamageable, ITargetable
{
    [SerializeField] private EnemyConfig enemyConfig;
    [SerializeField] private Transform aimPoint;

    private float currentHealth;

    public event Action<Enemy> OnDied;

    private void Awake()
    {

        if (enemyConfig == null)
        {
            Debug.LogError("EnemyConfig is not assigned on " + gameObject.name);
            enabled = false;
            return;
        }
        currentHealth = enemyConfig.MaxHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    private void Die()
    {
        OnDied?.Invoke(this);
        Destroy(gameObject);
    }
    public Transform GetTransform() => transform;

    public Transform AimPoint => aimPoint != null ? aimPoint : transform;
    public bool IsTargetable => enabled && gameObject.activeInHierarchy;
}
