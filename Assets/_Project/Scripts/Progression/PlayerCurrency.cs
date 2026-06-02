using System;
using UnityEngine;

public sealed class PlayerCurrency : MonoBehaviour
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
}
