using UnityEngine;

public sealed class CampDefenseResolutionController : MonoBehaviour
{
    [SerializeField] private CampDefenseResultState resultState;
    [SerializeField] private ThreatPendingState pendingState;
    [SerializeField] private ThreatMeter threatMeter;

    private void Awake()
    {
        resultState ??= GetComponent<CampDefenseResultState>();
        pendingState ??= GetComponent<ThreatPendingState>();
        threatMeter ??= GetComponent<ThreatMeter>();
    }

    private void OnEnable()
    {
        if (resultState != null)
        {
            resultState.OnDefenseSucceeded += HandleDefenseSucceeded;
            resultState.OnDefenseFailed += HandleDefenseFailed;
        }
    }

    private void OnDisable()
    {
        if (resultState != null)
        {
            resultState.OnDefenseSucceeded -= HandleDefenseSucceeded;
            resultState.OnDefenseFailed -= HandleDefenseFailed;
        }
    }

    private void HandleDefenseSucceeded()
    {
        pendingState?.ClearPending();
        threatMeter?.ResetThreat();
    }

    private void HandleDefenseFailed()
    {
        // Defeat resolves later through camp recovery/broken-state flow.
    }
}
