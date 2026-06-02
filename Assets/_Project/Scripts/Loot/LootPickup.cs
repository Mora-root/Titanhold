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

    public void Pickup(GameObject picker)
    {
        if (!isLootable)
            return;

        isLootable = false;
        Destroy(gameObject);
    }
}
