using UnityEngine;

public sealed class GoldLootReward : MonoBehaviour, ILootReward, IAmountLootReward
{
    [SerializeField] private int amount = 1;
    [SerializeField] private PlayerGold playerGold;

    public void SetAmount(int amount)
    {
        this.amount = amount;
    }

    public bool Collect(GameObject picker)
    {
        if (amount <= 0)
            return false;

        PlayerGold wallet = playerGold;

        if (wallet == null && picker != null)
            wallet = picker.GetComponent<PlayerGold>();

        wallet ??= FindAnyObjectByType<PlayerGold>();

        if (wallet == null)
            return false;

        wallet.Add(amount);
        return true;
    }
}
