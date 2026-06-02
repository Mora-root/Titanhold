using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyDeathNotifier))]
public sealed class EnemyRewardSource : MonoBehaviour
{
    [SerializeField] private EnemyDeathNotifier deathNotifier;
    [SerializeField] private PlayerExperience playerExperience;
    [SerializeField] private int experienceAmount = 10;

    private void Awake()
    {
        deathNotifier ??= GetComponent<EnemyDeathNotifier>();
        playerExperience ??= FindAnyObjectByType<PlayerExperience>();
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
        // MVP: any death of an enemy with this component grants XP.
        // Future: require player-attributed DeathContext before granting XP.
        if (playerExperience == null)
            return;

        playerExperience.AddExperience(experienceAmount);
    }
}
