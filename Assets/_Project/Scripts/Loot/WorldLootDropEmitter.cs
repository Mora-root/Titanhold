using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class WorldLootDropEmitter : MonoBehaviour
{
    [SerializeField] private GameObject itemPickupPrefab;
    [SerializeField] private GameObject goldPickupPrefab;
    [SerializeField] private Transform dropPoint;
    [SerializeField, Min(0f)] private float dropRadius = 1.25f;
    [SerializeField, Min(0f)] private float dropSpawnHeight = 1.2f;

    [Header("Ground Snap")]
    [SerializeField] private bool snapToGround = true;
    [SerializeField] private LayerMask groundMask = Physics.DefaultRaycastLayers;
    [SerializeField, Min(0f)] private float groundProbeHeight = 3f;
    [SerializeField, Min(0f)] private float groundProbeDistance = 8f;
    [SerializeField] private float landingYOffset = 0.05f;

    public bool CanEmit(
        IReadOnlyList<LootDropResult> drops,
        out WorldLootEmissionError error)
    {
        if (drops == null || drops.Count == 0)
        {
            error = WorldLootEmissionError.EmptyDrops;
            return false;
        }

        for (int i = 0; i < drops.Count; i++)
        {
            LootDropResult drop = drops[i];
            switch (drop.Kind)
            {
                case LootDropKind.Item:
                    if (drop.Stack == null ||
                        drop.Stack.Definition == null ||
                        drop.Stack.Amount <= 0)
                    {
                        error = WorldLootEmissionError.InvalidDrop;
                        return false;
                    }

                    if (itemPickupPrefab == null)
                    {
                        error = WorldLootEmissionError.MissingItemPickupPrefab;
                        return false;
                    }
                    break;

                case LootDropKind.Gold:
                    if (drop.GoldAmount <= 0)
                    {
                        error = WorldLootEmissionError.InvalidDrop;
                        return false;
                    }

                    if (goldPickupPrefab == null)
                    {
                        error = WorldLootEmissionError.MissingGoldPickupPrefab;
                        return false;
                    }

                    if (goldPickupPrefab.GetComponents<IAmountLootReward>().Length == 0)
                    {
                        error = WorldLootEmissionError.InvalidGoldPickupPrefab;
                        return false;
                    }
                    break;

                default:
                    error = WorldLootEmissionError.InvalidDrop;
                    return false;
            }
        }

        error = WorldLootEmissionError.None;
        return true;
    }

    public WorldLootEmissionResult TryEmit(
        IReadOnlyList<LootDropResult> drops)
    {
        if (!CanEmit(drops, out WorldLootEmissionError error))
            return WorldLootEmissionResult.Failed(error);

        for (int i = 0; i < drops.Count; i++)
        {
            LootDropResult drop = drops[i];
            if (drop.Kind == LootDropKind.Item)
                SpawnItemDrop(drop.Stack);
            else
                SpawnGoldDrop(drop.GoldAmount);
        }

        return WorldLootEmissionResult.Succeeded(drops.Count);
    }

    private void SpawnItemDrop(ItemStack stack)
    {
        GameObject pickup = SpawnPickup(itemPickupPrefab);
        PlayerInventoryItemStackLootReward reward =
            pickup.GetComponent<PlayerInventoryItemStackLootReward>();
        reward ??= pickup.AddComponent<PlayerInventoryItemStackLootReward>();
        reward.SetStack(stack);

        LootLabelTarget labelTarget = pickup.GetComponent<LootLabelTarget>();
        labelTarget ??= pickup.AddComponent<LootLabelTarget>();
        labelTarget.Refresh();

        GeneratedItemPickupView view =
            pickup.GetComponentInChildren<GeneratedItemPickupView>();
        view?.Refresh();
    }

    private void SpawnGoldDrop(int amount)
    {
        GameObject pickup = SpawnPickup(goldPickupPrefab);
        IAmountLootReward[] amountRewards =
            pickup.GetComponents<IAmountLootReward>();
        for (int i = 0; i < amountRewards.Length; i++)
            amountRewards[i].SetAmount(amount);
    }

    private GameObject SpawnPickup(GameObject prefab)
    {
        Vector3 basePosition =
            dropPoint != null ? dropPoint.position : transform.position;
        Vector2 circle = Random.insideUnitCircle * dropRadius;
        Vector3 landingPosition = ResolveLandingPosition(
            basePosition + new Vector3(circle.x, 0f, circle.y));
        Vector3 startPosition = basePosition + Vector3.up * dropSpawnHeight;
        bool hasMotion = prefab.GetComponent<LootDropMotion>() != null;
        Vector3 spawnPosition = hasMotion ? startPosition : landingPosition;

        GameObject pickup = Instantiate(
            prefab,
            spawnPosition,
            Quaternion.identity);
        LootDropMotion motion = pickup.GetComponent<LootDropMotion>();
        motion?.Play(startPosition, landingPosition);
        return pickup;
    }

    private Vector3 ResolveLandingPosition(Vector3 position)
    {
        if (!snapToGround)
            return position;

        Vector3 rayOrigin = position + Vector3.up * groundProbeHeight;
        float rayDistance = groundProbeHeight + groundProbeDistance;
        if (Physics.Raycast(
                rayOrigin,
                Vector3.down,
                out RaycastHit hit,
                rayDistance,
                groundMask,
                QueryTriggerInteraction.Ignore))
        {
            return hit.point + Vector3.up * landingYOffset;
        }

        return position;
    }

    private void OnValidate()
    {
        dropRadius = Mathf.Max(0f, dropRadius);
        dropSpawnHeight = Mathf.Max(0f, dropSpawnHeight);
        groundProbeHeight = Mathf.Max(0f, groundProbeHeight);
        groundProbeDistance = Mathf.Max(0f, groundProbeDistance);
    }
}
