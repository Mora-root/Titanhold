using System;
using UnityEngine;

public sealed class ThreatPendingState : MonoBehaviour
{
    [SerializeField] private ThreatMeter threatMeter;

    public bool IsPending { get; private set; }

    public event Action OnPendingStarted;
    public event Action OnPendingCleared;

    private void Awake()
    {
        if (threatMeter == null)
        {
            threatMeter = GetComponent<ThreatMeter>();
        }
    }

    private void OnEnable()
    {
        if (threatMeter != null)
        {
            threatMeter.OnThreatFull += StartPending;
        }
    }

    private void OnDisable()
    {
        if (threatMeter != null)
        {
            threatMeter.OnThreatFull -= StartPending;
        }
    }

    public void ClearPending()
    {
        if (!IsPending)
            return;

        IsPending = false;
        OnPendingCleared?.Invoke();
    }

    private void StartPending()
    {
        if (IsPending)
            return;

        IsPending = true;
        OnPendingStarted?.Invoke();
    }
}
