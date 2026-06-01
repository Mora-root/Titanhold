using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyDeathNotifier))]
public sealed class EnemyDeathDespawn : MonoBehaviour
{
    [SerializeField] private EnemyDeathNotifier deathNotifier;
    [SerializeField] private float despawnDelay = 3f;

    private bool despawnStarted;

    private void Awake()
    {
        deathNotifier ??= GetComponent<EnemyDeathNotifier>();
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
        if (despawnStarted)
            return;

        despawnStarted = true;
        StartCoroutine(DespawnAfterDelay());
    }

    private IEnumerator DespawnAfterDelay()
    {
        if (despawnDelay > 0f)
            yield return new WaitForSeconds(despawnDelay);

        Destroy(gameObject);
    }
}
