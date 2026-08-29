using UnityEngine;

public sealed class AutoPickupCollector : MonoBehaviour
{
    [SerializeField] private GameObject pickerOverride;
    [SerializeField] private bool collectGold = true;
    [SerializeField] private bool collectItems = false;

    private GameObject Picker => pickerOverride != null ? pickerOverride : gameObject;

    private void OnTriggerEnter(Collider other)
    {
        TryCollect(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryCollect(other);
    }

    private void TryCollect(Collider other)
    {
        if (other == null)
            return;

        LootPickup pickup = other.GetComponentInParent<LootPickup>();
        if (pickup == null || !pickup.IsLootable)
            return;

        if (!CanAutoCollect(pickup))
            return;

        pickup.Pickup(Picker);
    }

    private bool CanAutoCollect(LootPickup pickup)
    {
        ILootReward[] rewards = pickup.GetComponents<ILootReward>();
        if (rewards == null || rewards.Length == 0)
            return false;

        bool hasAllowedReward = false;

        foreach (ILootReward reward in rewards)
        {
            if (reward == null)
                continue;

            if (reward is GoldLootReward)
            {
                if (!collectGold)
                    return false;

                hasAllowedReward = true;
                continue;
            }

            if (reward is PlayerInventoryLootReward || reward is PlayerInventoryItemStackLootReward)
            {
                if (!collectItems)
                    return false;

                hasAllowedReward = true;
                continue;
            }

            return false;
        }

        return hasAllowedReward;
    }
}
