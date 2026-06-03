using System;
using UnityEngine;

public sealed class PlayerLootInventory : MonoBehaviour
{
    [SerializeField] private int crystalShards;

    public int CrystalShards => crystalShards;

    public event Action<int> OnCrystalShardsChanged;

    public void AddCrystalShards(int amount)
    {
        if (amount <= 0)
            return;

        crystalShards += amount;
        OnCrystalShardsChanged?.Invoke(crystalShards);
    }
}
