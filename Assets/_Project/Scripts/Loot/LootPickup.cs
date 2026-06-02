using UnityEngine;

public sealed class LootPickup : MonoBehaviour, ISelectable, ILootable
{
    [SerializeField] private Transform lootPoint;
    [SerializeField] private float pickupRange = 1.5f;
    [SerializeField] private bool isLootable = true;
    [SerializeField] private int amount = 1;
    [SerializeField] private PlayerCurrency playerCurrency;

    public bool IsSelectable => isLootable;

    public Transform LootPoint => lootPoint != null ? lootPoint : transform;
    public float PickupRange => pickupRange;
    public bool IsLootable => isLootable;

    public void OnSelected() { }

    public void OnDeselected() { }

    public void Pickup(GameObject picker)
    {
        if (!isLootable)
            return;

        PlayerCurrency currency = playerCurrency;

        if (currency == null && picker != null)
            currency = picker.GetComponent<PlayerCurrency>();

        currency ??= FindAnyObjectByType<PlayerCurrency>();

        if (currency == null)
            return;

        currency.Add(amount);
        isLootable = false;
        Destroy(gameObject);
    }
}
