using System;
using UnityEngine;

public sealed class PlayerGold : MonoBehaviour
{
    [SerializeField] private int amount;

    public int Amount => amount;

    public event Action<int> OnChanged;

    public void Add(int value)
    {
        if (value <= 0)
            return;

        amount += value;
        OnChanged?.Invoke(amount);
    }

    internal bool CanRestoreState(int restoredAmount)
    {
        return restoredAmount >= 0;
    }

    internal void RestoreState(int restoredAmount)
    {
        if (!CanRestoreState(restoredAmount))
            throw new ArgumentOutOfRangeException(nameof(restoredAmount));

        if (amount == restoredAmount)
            return;

        amount = restoredAmount;
        OnChanged?.Invoke(amount);
    }
}
