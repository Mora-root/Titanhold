using System.Collections.Generic;
using UnityEngine;

[System.Obsolete("Use EnemyLootTableDropper with the unified LootTable.")]
[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyDeathNotifier))]
public sealed class EnemyItemLootTableDropper : MonoBehaviour
{
    [SerializeField] private EnemyDeathNotifier deathNotifier;
    [SerializeField] private ItemLootTable lootTable;
    [SerializeField] private GameObject pickupPrefab;
    [SerializeField] private Transform dropPoint;
    [SerializeField] private float dropRadius = 0.35f;
    [SerializeField] private float dropSpawnHeight = 1.2f;

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
        if (lootTable == null || pickupPrefab == null)
            return;

        List<ItemStack> drops = lootTable.Roll(CreateRandom());
        for (int i = 0; i < drops.Count; i++)
            SpawnDrop(drops[i]);
    }

    private void SpawnDrop(ItemStack stack)
    {
        if (stack == null || stack.Definition == null)
            return;

        Vector3 basePosition = dropPoint != null ? dropPoint.position : transform.position;
        Vector2 circle = Random.insideUnitCircle * dropRadius;
        Vector3 landingPosition = basePosition + new Vector3(circle.x, 0f, circle.y);
        Vector3 startPosition = basePosition + Vector3.up * dropSpawnHeight;
        bool hasMotion = pickupPrefab.GetComponent<LootDropMotion>() != null;
        Vector3 spawnPosition = hasMotion ? startPosition : landingPosition;

        GameObject pickup = Instantiate(pickupPrefab, spawnPosition, Quaternion.identity);
        PlayerInventoryItemStackLootReward reward = pickup.AddComponent<PlayerInventoryItemStackLootReward>();
        reward.SetStack(stack);

        LootLabelTarget labelTarget = pickup.GetComponent<LootLabelTarget>();
        if (labelTarget == null)
            labelTarget = pickup.AddComponent<LootLabelTarget>();

        labelTarget.Refresh();

        GeneratedItemPickupView view = pickup.GetComponentInChildren<GeneratedItemPickupView>();
        if (view != null)
            view.Refresh();

        LootDropMotion motion = pickup.GetComponent<LootDropMotion>();
        if (motion != null)
            motion.Play(startPosition, landingPosition);
    }

    private static System.Random CreateRandom()
    {
        return new System.Random(Random.Range(int.MinValue, int.MaxValue));
    }
}
