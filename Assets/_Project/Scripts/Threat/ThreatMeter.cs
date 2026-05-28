using System;
using UnityEngine;

public sealed class ThreatMeter : MonoBehaviour
{
    [SerializeField] private float maxThreat = 100f;

    public float CurrentThreat { get; private set; }
    public float MaxThreat => maxThreat;
    public bool IsFull => CurrentThreat >= MaxThreat;

    public event Action<float, float> OnThreatChanged;
    public event Action OnThreatFull;

    public void AddThreat(float amount)
    {
        if (amount <= 0f)
            return;

        bool wasFull = IsFull;
        float previousThreat = CurrentThreat;

        CurrentThreat = Mathf.Clamp(CurrentThreat + amount, 0f, MaxThreat);

        if (Mathf.Approximately(CurrentThreat, previousThreat))
            return;

        OnThreatChanged?.Invoke(CurrentThreat, MaxThreat);

        if (!wasFull && IsFull)
        {
            OnThreatFull?.Invoke();
        }
    }

    public void ResetThreat()
    {
        if (Mathf.Approximately(CurrentThreat, 0f))
            return;

        CurrentThreat = 0f;
        OnThreatChanged?.Invoke(CurrentThreat, MaxThreat);
    }
}
