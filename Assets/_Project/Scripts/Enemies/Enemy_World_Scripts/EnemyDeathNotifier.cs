using System;
using Titanhold.Combat;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Health))]
public sealed class EnemyDeathNotifier : MonoBehaviour
{
    private Health health;

    public event Action<EnemyDeathNotifier> Died;
    public event Action<EnemyDeathNotifier, DeathContext> DiedWithContext;

    public DeathContext LastDeathContext { get; private set; }

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        health ??= GetComponent<Health>();
        LastDeathContext = default;

        if (health != null)
        {
            health.OnDeathContext += HandleDeath;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnDeathContext -= HandleDeath;
        }
    }

    private void HandleDeath(DeathContext context)
    {
        LastDeathContext = context;
        DiedWithContext?.Invoke(this, context);
        Died?.Invoke(this);
    }
}
