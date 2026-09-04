using Titanhold.Run;
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

        RunProgressionParticipantGateway runProgression = picker != null
            ? picker.GetComponentInParent<RunProgressionParticipantGateway>()
            : null;
        if (runProgression != null)
            return runProgression.TryAddGold(amount, out _);

        PlayerGold wallet = playerGold;

        if (wallet == null && picker != null)
            wallet = picker.GetComponent<PlayerGold>();

        if (wallet == null && picker != null)
            wallet = picker.GetComponentInParent<PlayerGold>();

        if (wallet == null)
            return false;

        wallet.Add(amount);
        return true;
    }
}
