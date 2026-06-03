using UnityEngine;

public sealed class LootPickup : MonoBehaviour, ISelectable, ILootable
{
    [SerializeField] private Transform lootPoint;
    [SerializeField] private float pickupRange = 1.5f;
    [SerializeField] private bool isLootable = true;

    public bool IsSelectable => isLootable;

    public Transform LootPoint => lootPoint != null ? lootPoint : transform;
    public float PickupRange => pickupRange;
    public bool IsLootable => isLootable;

    public void OnSelected() { }

    public void OnDeselected() { }

    public void SetLootable(bool value)
    {
        isLootable = value;
    }

    public void Pickup(GameObject picker)
    {
        if (!isLootable)
            return;

        ILootReward[] rewards = GetComponents<ILootReward>();
        if (rewards == null || rewards.Length == 0)
            return;

        bool collectedAny = false;

        foreach (ILootReward reward in rewards)
        {
            if (reward != null && reward.Collect(picker))
                collectedAny = true;
        }

        if (!collectedAny)
            return;

        isLootable = false;
        Destroy(gameObject);
    }
}
