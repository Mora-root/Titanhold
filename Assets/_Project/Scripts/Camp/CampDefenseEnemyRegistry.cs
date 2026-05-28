using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class CampDefenseEnemyRegistry : MonoBehaviour
{
    private readonly HashSet<EnemyDeathNotifier> aliveEnemies = new HashSet<EnemyDeathNotifier>();
    private int totalRegistered;

    public int AliveEnemyCount => aliveEnemies.Count;
    public bool HasAliveEnemies => aliveEnemies.Count > 0;

    public event Action<EnemyDeathNotifier> OnEnemyRegistered;
    public event Action<EnemyDeathNotifier> OnEnemyDefeated;
    public event Action OnAllEnemiesDefeated;

    private void OnDisable()
    {
        Clear();
    }

    public bool Register(EnemyDeathNotifier notifier)
    {
        if (notifier == null)
            return false;

        if (!aliveEnemies.Add(notifier))
            return false;

        totalRegistered++;
        notifier.Died += HandleEnemyDied;
        OnEnemyRegistered?.Invoke(notifier);
        return true;
    }

    public bool Unregister(EnemyDeathNotifier notifier)
    {
        if (notifier == null)
            return false;

        if (!aliveEnemies.Remove(notifier))
            return false;

        notifier.Died -= HandleEnemyDied;
        return true;
    }

    public void Clear()
    {
        foreach (EnemyDeathNotifier notifier in aliveEnemies)
        {
            if (notifier != null)
            {
                notifier.Died -= HandleEnemyDied;
            }
        }

        aliveEnemies.Clear();
        totalRegistered = 0;
    }

    private void HandleEnemyDied(EnemyDeathNotifier notifier)
    {
        if (!Unregister(notifier))
            return;

        OnEnemyDefeated?.Invoke(notifier);

        if (totalRegistered > 0 && AliveEnemyCount == 0)
        {
            OnAllEnemiesDefeated?.Invoke();
        }
    }
}
