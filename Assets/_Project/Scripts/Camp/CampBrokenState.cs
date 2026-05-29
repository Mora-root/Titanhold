using System;
using UnityEngine;

public sealed class CampBrokenState : MonoBehaviour
{
    [SerializeField] private CampDefenseResultState resultState;
    [SerializeField] private ThreatPendingState pendingState;
    [SerializeField] private ThreatMeter threatMeter;
    [SerializeField] private CampCore campCore;

    public bool IsBroken { get; private set; }

    public event Action OnCampBroken;
    public event Action OnCampRestored;

    private void Awake()
    {
        resultState ??= GetComponent<CampDefenseResultState>();
        pendingState ??= GetComponent<ThreatPendingState>();
        threatMeter ??= GetComponent<ThreatMeter>();
        campCore ??= FindAnyObjectByType<CampCore>();
    }

    private void OnEnable()
    {
        if (resultState != null)
        {
            resultState.OnDefenseFailed += HandleDefenseFailed;
        }
    }

    private void OnDisable()
    {
        if (resultState != null)
        {
            resultState.OnDefenseFailed -= HandleDefenseFailed;
        }
    }

    public void RestoreCamp()
    {
        if (!IsBroken)
            return;

        IsBroken = false;
        campCore?.Health?.RestoreFull();
        pendingState?.ClearPending();
        threatMeter?.ResetThreat();
        OnCampRestored?.Invoke();
    }

    public void BreakCamp()
    {
        if (IsBroken)
            return;

        IsBroken = true;
        OnCampBroken?.Invoke();
    }

    private void HandleDefenseFailed()
    {
        BreakCamp();
    }
}
