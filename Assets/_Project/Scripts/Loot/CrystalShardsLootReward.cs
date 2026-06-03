using UnityEngine;

public sealed class CrystalShardsLootReward : MonoBehaviour, ILootReward, IAmountLootReward
{
    [SerializeField] private int amount = 1;
    [SerializeField] private PlayerLootInventory playerLootInventory;

    public void SetAmount(int amount)
    {
        this.amount = amount;
    }

    public bool Collect(GameObject picker)
    {
        if (amount <= 0)
            return false;

        PlayerLootInventory storage = playerLootInventory;

        if (storage == null && picker != null)
            storage = picker.GetComponent<PlayerLootInventory>();

        storage ??= FindAnyObjectByType<PlayerLootInventory>();

        if (storage == null)
            return false;

        storage.AddCrystalShards(amount);
        return true;
    }
}
