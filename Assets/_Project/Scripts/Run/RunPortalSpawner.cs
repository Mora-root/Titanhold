using System;
using UnityEngine;
using UnityEngine.AI;

namespace Titanhold.Run
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RunFlowRuntime))]
    public sealed class RunPortalSpawner : MonoBehaviour
    {
        [SerializeField] private RunFlowRuntime runFlowRuntime;
        [SerializeField] private RunPortalInteractable portalPrefab;
        [SerializeField] private Transform localPlayer;
        [SerializeField, Min(0f)] private float spawnDistance = 3f;
        [SerializeField, Min(0f)] private float navMeshSampleDistance = 3f;
        [SerializeField] private float heightOffset = 0.05f;

        private RunPortalInteractable activePortal;
        private bool missingPrefabReported;

        public RunPortalInteractable ActivePortal => activePortal;
        public bool HasActivePortal => activePortal != null;

        public event Action<RunPortalInteractable> PortalSpawned;
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
            RebindLocalPlayer();
            SynchronizeWithState();
        }

        private void OnDisable()
        {
            if (runFlowRuntime != null)
                runFlowRuntime.StateChanged -= HandleStateChanged;

            RemovePortal();
        }

        public void RebindLocalPlayer()
        {
            if (localPlayer == null)
            {
                PlayerBrain playerBrain = FindAnyObjectByType<PlayerBrain>();
                localPlayer = playerBrain != null ? playerBrain.transform : null;
            }
        }

        public void Configure(
            RunFlowRuntime runtime,
            RunPortalInteractable prefab,
            Transform player)
        {
            if (isActiveAndEnabled && runFlowRuntime != null)
                runFlowRuntime.StateChanged -= HandleStateChanged;

            RemovePortal();
            runFlowRuntime = runtime;
            portalPrefab = prefab;
            localPlayer = player;
            missingPrefabReported = false;

            if (isActiveAndEnabled && runFlowRuntime != null)
                runFlowRuntime.StateChanged += HandleStateChanged;

            SynchronizeWithState();
        }

        private void HandleStateChanged(RunFlowState state)
        {
            SynchronizeWithState(state);
        }

        private void SynchronizeWithState()
        {
            if (runFlowRuntime == null)
                return;

            SynchronizeWithState(runFlowRuntime.State);
        }

        private void SynchronizeWithState(RunFlowState state)
        {
            if (state.Phase == RunPhase.PortalOpen)
            {
                EnsurePortal(state.RoundNumber);
                return;
            }

            missingPrefabReported = false;
            RemovePortal();
        }

        private void EnsurePortal(int roundNumber)
        {
            if (activePortal != null)
                return;

            if (portalPrefab == null)
            {
                if (!missingPrefabReported)
                {
                    Debug.LogWarning(
                        $"{nameof(RunPortalSpawner)} cannot create a portal because its prefab is missing.",
                        this);
                    missingPrefabReported = true;
                }

                return;
            }

            RebindLocalPlayer();
            if (localPlayer == null)
                return;

            Vector3 position = ResolveSpawnPosition(localPlayer);
            activePortal = Instantiate(portalPrefab, position, portalPrefab.transform.rotation);
            activePortal.Initialize(runFlowRuntime, roundNumber);
            activePortal.gameObject.SetActive(true);
            PortalSpawned?.Invoke(activePortal);
        }

        private Vector3 ResolveSpawnPosition(Transform player)
        {
            Vector3 direction = Vector3.ProjectOnPlane(player.right, Vector3.up).normalized;
            if (direction.sqrMagnitude <= 0.0001f)
                direction = Vector3.right;

            Vector3 candidate = player.position + direction * spawnDistance;
            if (NavMesh.SamplePosition(
                    candidate,
                    out NavMeshHit hit,
                    navMeshSampleDistance,
                    NavMesh.AllAreas))
            {
                candidate = hit.position;
            }

            candidate.y += heightOffset;
            return candidate;
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
            if (runFlowRuntime == null)
                runFlowRuntime = GetComponent<RunFlowRuntime>();
        }

        private void OnValidate()
        {
            spawnDistance = Mathf.Max(0f, spawnDistance);
            navMeshSampleDistance = Mathf.Max(0f, navMeshSampleDistance);
        }
    }
}
