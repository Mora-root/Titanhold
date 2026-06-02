using UnityEngine;

public sealed class CrystalShardsLootReward : MonoBehaviour, ILootReward, IAmountLootReward
{
    [SerializeField] private int amount = 1;
    [SerializeField] private PlayerCurrency playerCurrency;

    public void SetAmount(int amount)
    {
        this.amount = amount;
    }

    public bool Collect(GameObject picker)
    {
        if (amount <= 0)
            return false;

        PlayerCurrency currency = playerCurrency;

        if (currency == null && picker != null)
            currency = picker.GetComponent<PlayerCurrency>();

        currency ??= FindAnyObjectByType<PlayerCurrency>();

        if (currency == null)
            return false;

        currency.Add(amount);
        return true;
    }
}
