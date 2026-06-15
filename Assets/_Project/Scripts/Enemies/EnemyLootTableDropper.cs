using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyDeathNotifier))]
public sealed class EnemyLootTableDropper : MonoBehaviour
{
    [SerializeField] private EnemyDeathNotifier deathNotifier;
    [SerializeField] private LootTable lootTable;
    [SerializeField] private GameObject itemPickupPrefab;
    [SerializeField] private GameObject goldPickupPrefab;
    [SerializeField] private Transform dropPoint;
    [SerializeField] private float dropRadius = 0.35f;
    [SerializeField] private float dropSpawnHeight = 1.2f;

    [Header("Ground Snap")]
    [SerializeField] private bool snapToGround = true;
    [SerializeField] private LayerMask groundMask = Physics.DefaultRaycastLayers;
    [SerializeField] private float groundProbeHeight = 3f;
    [SerializeField] private float groundProbeDistance = 8f;
    [SerializeField] private float landingYOffset = 0.05f;

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
        if (lootTable == null)
            return;

        List<LootDropResult> drops = lootTable.Roll(CreateRandom());
        for (int i = 0; i < drops.Count; i++)
            SpawnDrop(drops[i]);
    }

    private void SpawnDrop(LootDropResult drop)
    {
        switch (drop.Kind)
        {
            case LootDropKind.Item:
                SpawnItemDrop(drop.Stack);
                break;

            case LootDropKind.Gold:
                SpawnGoldDrop(drop.GoldAmount);
                break;
        }
    }

    private void SpawnItemDrop(ItemStack stack)
    {
        if (itemPickupPrefab == null || stack == null || stack.Definition == null)
            return;

        GameObject pickup = SpawnPickup(itemPickupPrefab);
        if (pickup == null)
            return;

        PlayerInventoryItemStackLootReward reward = pickup.AddComponent<PlayerInventoryItemStackLootReward>();
        reward.SetStack(stack);

        LootLabelTarget labelTarget = pickup.GetComponent<LootLabelTarget>();
        if (labelTarget == null)
            labelTarget = pickup.AddComponent<LootLabelTarget>();

        labelTarget.Refresh();

        GeneratedItemPickupView view = pickup.GetComponentInChildren<GeneratedItemPickupView>();
        if (view != null)
            view.Refresh();
    }

    private void SpawnGoldDrop(int amount)
    {
        if (goldPickupPrefab == null || amount <= 0)
            return;

        GameObject pickup = SpawnPickup(goldPickupPrefab);
        if (pickup == null)
            return;

        IAmountLootReward[] amountRewards = pickup.GetComponents<IAmountLootReward>();
        for (int i = 0; i < amountRewards.Length; i++)
            amountRewards[i].SetAmount(amount);
    }

    private GameObject SpawnPickup(GameObject prefab)
    {
        Vector3 basePosition = dropPoint != null ? dropPoint.position : transform.position;
        Vector2 circle = Random.insideUnitCircle * dropRadius;
        Vector3 landingPosition = ResolveLandingPosition(basePosition + new Vector3(circle.x, 0f, circle.y));
        Vector3 startPosition = basePosition + Vector3.up * dropSpawnHeight;
        bool hasMotion = prefab.GetComponent<LootDropMotion>() != null;
        Vector3 spawnPosition = hasMotion ? startPosition : landingPosition;

        GameObject pickup = Instantiate(prefab, spawnPosition, Quaternion.identity);

        LootDropMotion motion = pickup.GetComponent<LootDropMotion>();
        if (motion != null)
            motion.Play(startPosition, landingPosition);

        return pickup;
    }

    private Vector3 ResolveLandingPosition(Vector3 position)
    {
        if (!snapToGround)
            return position;

        float safeProbeHeight = Mathf.Max(0f, groundProbeHeight);
        float safeProbeDistance = Mathf.Max(0f, groundProbeDistance);
        Vector3 rayOrigin = position + Vector3.up * safeProbeHeight;
        float rayDistance = safeProbeHeight + safeProbeDistance;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayDistance, groundMask, QueryTriggerInteraction.Ignore))
            return hit.point + Vector3.up * landingYOffset;

        return position;
    }

    private static System.Random CreateRandom()
    {
        return new System.Random(Random.Range(int.MinValue, int.MaxValue));
    }
}
