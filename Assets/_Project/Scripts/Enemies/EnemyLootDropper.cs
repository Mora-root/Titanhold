using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyDeathNotifier))]
public sealed class EnemyLootDropper : MonoBehaviour
{
    [SerializeField] private EnemyDeathNotifier deathNotifier;
    [SerializeField] private GameObject pickupPrefab;
    [SerializeField] private Transform dropPoint;
    [SerializeField] private float dropChance = 1f;

    private void Awake()
    {
        deathNotifier ??= GetComponent<EnemyDeathNotifier>();
    }

    private void OnEnable()
    {
        if (deathNotifier != null)
            deathNotifier.Died += HandleEnemyDied;
    }

    private void OnDisable()
    {
        if (deathNotifier != null)
            deathNotifier.Died -= HandleEnemyDied;
    }

    private void HandleEnemyDied(EnemyDeathNotifier notifier)
    {
        if (pickupPrefab == null)
            return;

        float chance = Mathf.Clamp01(dropChance);
        if (Random.value > chance)
            return;

        Vector3 position = dropPoint != null ? dropPoint.position : transform.position;
        Instantiate(pickupPrefab, position, Quaternion.identity);
    }
}
