using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyDeathNotifier))]
public sealed class EnemyThreatSource : MonoBehaviour
{
    [SerializeField] private EnemyDeathNotifier deathNotifier;
    [SerializeField] private ThreatMeter threatMeter;
    [SerializeField] private CampBrokenState campBrokenState;
    [SerializeField] private float threatAmount = 10f;

    private void Awake()
    {
        deathNotifier ??= GetComponent<EnemyDeathNotifier>();
        threatMeter ??= FindAnyObjectByType<ThreatMeter>();
        campBrokenState ??= FindAnyObjectByType<CampBrokenState>();
    }

    private void OnEnable()
    {
        if (deathNotifier != null)
        {
            deathNotifier.Died += HandleEnemyDied;
        }
    }

    private void OnDisable()
    {
        if (deathNotifier != null)
        {
            deathNotifier.Died -= HandleEnemyDied;
        }
    }

    private void HandleEnemyDied(EnemyDeathNotifier notifier)
    {
        // MVP: any death of an enemy with this component grants threat.
        // Future: require player-attributed DeathContext before granting threat.
        if (campBrokenState != null && campBrokenState.IsBroken)
            return;

        if (threatMeter == null)
            return;

        threatMeter.AddThreat(threatAmount);
    }
}
