using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyDeathNotifier))]
public sealed class EnemyLootDropper : MonoBehaviour
{
    [System.Serializable]
    private struct DropEntry
    {
        public GameObject PickupPrefab;
        public float DropChance;
        public int MinAmount;
        public int MaxAmount;
    }

    [SerializeField] private EnemyDeathNotifier deathNotifier;
    [SerializeField] private Transform dropPoint;
    [SerializeField] private DropEntry[] drops;
    [SerializeField] private float dropRadius = 0.35f;

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
        if (drops == null)
            return;

        foreach (DropEntry entry in drops)
        {
            if (entry.PickupPrefab == null)
                continue;

            float chance = Mathf.Clamp01(entry.DropChance);
            if (Random.value > chance)
                continue;

            int min = Mathf.Max(0, entry.MinAmount);
            int max = Mathf.Max(min, entry.MaxAmount);
            int amount = Random.Range(min, max + 1);

            Vector3 basePosition = dropPoint != null ? dropPoint.position : transform.position;
            Vector2 circle = Random.insideUnitCircle * dropRadius;
            Vector3 position = basePosition + new Vector3(circle.x, 0f, circle.y);

            GameObject pickup = Instantiate(entry.PickupPrefab, position, Quaternion.identity);
            IAmountLootReward[] amountRewards = pickup.GetComponents<IAmountLootReward>();

            foreach (IAmountLootReward amountReward in amountRewards)
            {
                amountReward.SetAmount(amount);
            }
        }
    }
}
