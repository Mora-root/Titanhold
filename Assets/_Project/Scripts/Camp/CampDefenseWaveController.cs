using System;
using UnityEngine;

public enum CampDefenseWaveState
{
    Idle,
    Pending,
    Running,
    Completed,
    Failed
}

public sealed class CampDefenseWaveController : MonoBehaviour
{
    [SerializeField] private ThreatPendingState pendingState;
    [SerializeField] private CampCore campCore;
    [SerializeField] private CampDefenseEnemyRegistry enemyRegistry;

    public CampDefenseWaveState State { get; private set; } = CampDefenseWaveState.Idle;
    public bool IsPending => State == CampDefenseWaveState.Pending;
    public bool IsRunning => State == CampDefenseWaveState.Running;

    public event Action<CampDefenseWaveState> OnStateChanged;
    public event Action OnWaveStarted;
    public event Action OnWaveCompleted;
    public event Action OnWaveFailed;

    private void Awake()
    {
        pendingState ??= GetComponent<ThreatPendingState>();
        enemyRegistry ??= GetComponent<CampDefenseEnemyRegistry>();
        campCore ??= FindFirstObjectByType<CampCore>();
    }

    private void OnEnable()
    {
        if (pendingState != null)
        {
            pendingState.OnPendingStarted += HandlePendingStarted;
            pendingState.OnPendingCleared += HandlePendingCleared;
        }

        if (campCore != null)
        {
            campCore.OnCampCoreDestroyed += HandleCampCoreDestroyed;
        }

        if (enemyRegistry != null)
        {
            enemyRegistry.OnAllEnemiesDefeated += HandleAllEnemiesDefeated;
        }
    }

    private void OnDisable()
    {
        if (pendingState != null)
        {
            pendingState.OnPendingStarted -= HandlePendingStarted;
            pendingState.OnPendingCleared -= HandlePendingCleared;
        }

        if (campCore != null)
        {
            campCore.OnCampCoreDestroyed -= HandleCampCoreDestroyed;
        }

        if (enemyRegistry != null)
        {
            enemyRegistry.OnAllEnemiesDefeated -= HandleAllEnemiesDefeated;
        }
    }

    public bool StartWave()
    {
        if (State != CampDefenseWaveState.Pending)
            return false;

        if (pendingState == null || !pendingState.IsPending)
            return false;

        if (campCore == null || campCore.IsDestroyed)
            return false;

        SetState(CampDefenseWaveState.Running);
        OnWaveStarted?.Invoke();
        return true;
    }

    private void HandlePendingStarted()
    {
        if (State == CampDefenseWaveState.Running)
            return;

        SetState(CampDefenseWaveState.Pending);
    }

    private void HandlePendingCleared()
    {
        if (State != CampDefenseWaveState.Pending)
            return;

        SetState(CampDefenseWaveState.Idle);
    }

    private void HandleCampCoreDestroyed(CampCore core)
    {
        if (State != CampDefenseWaveState.Running)
            return;

        SetState(CampDefenseWaveState.Failed);
        OnWaveFailed?.Invoke();
    }

    private void HandleAllEnemiesDefeated()
    {
        if (State != CampDefenseWaveState.Running)
            return;

        SetState(CampDefenseWaveState.Completed);
        OnWaveCompleted?.Invoke();
    }

    private void SetState(CampDefenseWaveState state)
    {
        if (State == state)
            return;

        State = state;
        OnStateChanged?.Invoke(State);
    }
}
