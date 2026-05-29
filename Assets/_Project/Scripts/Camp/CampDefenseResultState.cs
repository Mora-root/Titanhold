using System;
using UnityEngine;

public enum CampDefenseResult
{
    None,
    Victory,
    Defeat
}

public sealed class CampDefenseResultState : MonoBehaviour
{
    [SerializeField] private CampDefenseWaveController waveController;

    public CampDefenseResult LastResult { get; private set; } = CampDefenseResult.None;

    public event Action OnDefenseSucceeded;
    public event Action OnDefenseFailed;

    private void Awake()
    {
        waveController ??= GetComponent<CampDefenseWaveController>();
    }

    private void OnEnable()
    {
        if (waveController != null)
        {
            waveController.OnWaveStarted += HandleWaveStarted;
            waveController.OnWaveCompleted += HandleWaveCompleted;
            waveController.OnWaveFailed += HandleWaveFailed;
        }
    }

    private void OnDisable()
    {
        if (waveController != null)
        {
            waveController.OnWaveStarted -= HandleWaveStarted;
            waveController.OnWaveCompleted -= HandleWaveCompleted;
            waveController.OnWaveFailed -= HandleWaveFailed;
        }
    }

    private void HandleWaveStarted()
    {
        LastResult = CampDefenseResult.None;
    }

    private void HandleWaveCompleted()
    {
        LastResult = CampDefenseResult.Victory;
        OnDefenseSucceeded?.Invoke();
    }

    private void HandleWaveFailed()
    {
        LastResult = CampDefenseResult.Defeat;
        OnDefenseFailed?.Invoke();
    }
}
