using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Health))]
public sealed class CampCore : MonoBehaviour
{
    [SerializeField] private Health health;

    public Health Health => health;
    public bool IsDestroyed => health != null && !health.IsAlive;

    public event Action<CampCore> OnCampCoreDestroyed;

    private void Awake()
    {
        health ??= GetComponent<Health>();
    }

    private void OnEnable()
    {
        health ??= GetComponent<Health>();

        if (health != null)
        {
            health.OnDeath += HandleDeath;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnDeath -= HandleDeath;
        }
    }

    private void HandleDeath()
    {
        OnCampCoreDestroyed?.Invoke(this);
    }
}
