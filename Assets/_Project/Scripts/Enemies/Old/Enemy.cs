using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

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
        Debug.Log("Enemy take damage " + damage);
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Enemy die");

        OnDied?.Invoke(this);

        enabled = false;
        var agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.enabled = false;
        }
        var movement = GetComponent<EnemyAgentMovement>();
        if (movement != null)
        {
            movement.enabled = false;
        }
        var collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }
        var animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }

        if (aimPoint != null && aimPoint != transform)
        {
            Destroy(aimPoint.gameObject);
            aimPoint = null;
        }

        float deathAnimLength = 1.5f;
        Destroy(gameObject, deathAnimLength);

    }
    public Transform GetTransform() => transform;

    public Transform AimPoint => aimPoint != null ? aimPoint : transform;
    public bool IsTargetable => enabled && gameObject.activeInHierarchy;
}
