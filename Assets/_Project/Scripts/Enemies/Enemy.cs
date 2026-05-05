using System;
using UnityEngine;

/// <summary>
/// Represents a basic enemy with health, movement towards a target, and death.
/// </summary>
public class Enemy : MonoBehaviour, IDamageable, ITargetable
{
    [SerializeField] private EnemyConfig _enemyConfig;
    [SerializeField] private Transform _aimPoint;

    private Transform _transform;
    private float _currentHealth;

    public event Action<Enemy> OnDied;

    private void Awake()
    {
        _transform = transform;

        if (_enemyConfig == null)
        {
            Debug.LogError("EnemyConfig is not assigned on " + gameObject.name);
            enabled = false;
            return;
        }
        _currentHealth = _enemyConfig.MaxHealth;
    }

    private void Update()
    {
        _transform.position += _transform.forward * (_enemyConfig.MoveSpeed * Time.deltaTime);
    }

    public void TakeDamage(float damage)
    {
        _currentHealth -= damage;
        if (_currentHealth <= 0)
        {
            Die();
        }
    }
    
    private void Die()
    {
        OnDied?.Invoke(this);
        Destroy(gameObject);
    }
    public Transform GetTransform() => _transform;

    public Transform AimPoint => _aimPoint != null ? _aimPoint : _transform;
    public bool IsTargetable => enabled && gameObject.activeInHierarchy;
}
