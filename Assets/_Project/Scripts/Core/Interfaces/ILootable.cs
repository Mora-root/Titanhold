using UnityEngine;

public interface ILootable
{
    Transform LootPoint { get; }
    float PickupRange { get; }
    bool IsLootable { get; }

    void Pickup(GameObject picker);
}
