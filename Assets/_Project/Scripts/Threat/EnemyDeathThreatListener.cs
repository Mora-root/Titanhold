using UnityEngine;

public sealed class EnemyDeathThreatListener : MonoBehaviour
{
    [SerializeField] private ThreatMeter threatMeter;
    [SerializeField] private float threatAmountPerEnemy = 10f;

    private EnemyDeathNotifier[] notifiers;

    private void Awake()
    {
        if (threatMeter == null)
        {
            threatMeter = GetComponent<ThreatMeter>();
        }

        notifiers = FindObjectsByType<EnemyDeathNotifier>(FindObjectsSortMode.None);
    }

    private void OnEnable()
    {
        if (notifiers == null)
            return;

        foreach (EnemyDeathNotifier notifier in notifiers)
        {
            if (notifier != null)
            {
                notifier.Died += HandleEnemyDied;
            }
        }
    }

    private void OnDisable()
    {
        if (notifiers == null)
            return;

        foreach (EnemyDeathNotifier notifier in notifiers)
        {
            if (notifier != null)
            {
                notifier.Died -= HandleEnemyDied;
            }
        }
    }

    private void HandleEnemyDied(EnemyDeathNotifier notifier)
    {
        if (threatMeter == null)
            return;

        threatMeter.AddThreat(threatAmountPerEnemy);
    }
}
