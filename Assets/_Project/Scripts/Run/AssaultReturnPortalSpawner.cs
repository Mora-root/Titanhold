using System;
using UnityEngine;

namespace Titanhold.Run
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RunFlowRuntime))]
    [RequireComponent(typeof(AssaultArenaTransitionController))]
    [RequireComponent(typeof(AssaultTargetRegistry))]
    public sealed class AssaultReturnPortalSpawner : MonoBehaviour
    {
        [SerializeField] private RunFlowRuntime runFlowRuntime;
        [SerializeField] private AssaultArenaTransitionController transitionController;
        [SerializeField] private AssaultTargetRegistry targetRegistry;
        [SerializeField] private AssaultReturnPortalInteractable portalPrefab;
        [SerializeField] private Transform spawnPoint;

        private AssaultReturnPortalInteractable activePortal;
        private bool missingConfigurationReported;

        public AssaultReturnPortalInteractable ActivePortal => activePortal;
        public bool HasActivePortal => activePortal != null;

        public event Action<AssaultReturnPortalInteractable> PortalSpawned;
        public event Action PortalRemoved;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            if (runFlowRuntime != null)
                runFlowRuntime.StateChanged += HandleStateChanged;
        }

        private void Start()
        {
            SynchronizeWithState();
        }

        private void OnDisable()
        {
            if (runFlowRuntime != null)
                runFlowRuntime.StateChanged -= HandleStateChanged;

            RemovePortal();
        }

        private void HandleStateChanged(RunFlowState state)
        {
            SynchronizeWithState(state);
        }

        private void SynchronizeWithState()
        {
            if (runFlowRuntime != null)
                SynchronizeWithState(runFlowRuntime.State);
        }

        private void SynchronizeWithState(RunFlowState state)
        {
            if (state.Phase == RunPhase.Intermission)
            {
                EnsurePortal(state.RoundNumber);
                return;
            }

            missingConfigurationReported = false;
            RemovePortal();
        }

        private void EnsurePortal(int roundNumber)
        {
            if (activePortal != null &&
                activePortal.ExpectedRound == roundNumber)
            {
                return;
            }

            RemovePortal();
            ResolveReferences();
            if (portalPrefab == null ||
                spawnPoint == null ||
                transitionController == null ||
                targetRegistry == null)
            {
                if (!missingConfigurationReported)
                {
                    Debug.LogWarning(
                        $"{nameof(AssaultReturnPortalSpawner)} cannot create a portal because its wiring is incomplete.",
                        this);
                    missingConfigurationReported = true;
                }

                return;
            }

            activePortal = Instantiate(
                portalPrefab,
                spawnPoint.position,
                spawnPoint.rotation);
            activePortal.Initialize(
                runFlowRuntime,
                transitionController,
                targetRegistry,
                roundNumber);
            activePortal.gameObject.SetActive(true);
            PortalSpawned?.Invoke(activePortal);
        }

        private void RemovePortal()
        {
            if (activePortal == null)
                return;

            Destroy(activePortal.gameObject);
            activePortal = null;
            PortalRemoved?.Invoke();
        }

        private void ResolveReferences()
        {
            runFlowRuntime ??= GetComponent<RunFlowRuntime>();
            transitionController ??=
                GetComponent<AssaultArenaTransitionController>();
            targetRegistry ??= GetComponent<AssaultTargetRegistry>();
        }
    }
}
