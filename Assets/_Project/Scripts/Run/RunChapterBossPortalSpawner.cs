using System;
using UnityEngine;
using UnityEngine.AI;

namespace Titanhold.Run
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RunChapterFlowRuntime))]
    public sealed class RunChapterBossPortalSpawner : MonoBehaviour
    {
        [SerializeField] private RunChapterFlowRuntime chapterFlowRuntime;
        [SerializeField] private RunChapterBossPortalInteractable portalPrefab;
        [SerializeField] private Transform localPlayer;
        [SerializeField, Min(0f)] private float spawnDistance = 3f;
        [SerializeField, Min(0f)] private float navMeshSampleDistance = 3f;
        [SerializeField] private float heightOffset = 0.05f;

        private RunChapterBossPortalInteractable activePortal;
        private bool missingPrefabReported;

        public RunChapterFlowRuntime ChapterFlowRuntime => chapterFlowRuntime;
        public RunChapterBossPortalInteractable PortalPrefab => portalPrefab;
        public Transform LocalPlayer => localPlayer;
        public RunChapterBossPortalInteractable ActivePortal => activePortal;
        public bool HasActivePortal => activePortal != null;

        public event Action<RunChapterBossPortalInteractable> PortalSpawned;
        public event Action PortalRemoved;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            RunChapterFlowRuntime configuredRuntime,
            RunChapterBossPortalInteractable configuredPrefab,
            Transform configuredLocalPlayer)
        {
            chapterFlowRuntime = configuredRuntime;
            portalPrefab = configuredPrefab;
            localPlayer = configuredLocalPlayer;
        }
#endif

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            if (chapterFlowRuntime != null)
                chapterFlowRuntime.StateChanged += HandleStateChanged;
        }

        private void Start()
        {
            RebindLocalPlayer();
            SynchronizeWithState();
        }

        private void OnDisable()
        {
            if (chapterFlowRuntime != null)
                chapterFlowRuntime.StateChanged -= HandleStateChanged;

            RemovePortal();
        }

        public void RebindLocalPlayer()
        {
            if (localPlayer != null)
                return;

            PlayerBrain playerBrain = FindAnyObjectByType<PlayerBrain>();
            localPlayer = playerBrain != null ? playerBrain.transform : null;
        }

        private void HandleStateChanged(RunChapterFlowState state)
        {
            SynchronizeWithState();
        }

        private void SynchronizeWithState()
        {
            if (chapterFlowRuntime == null)
                return;

            RunChapterFlowPresentationSnapshot snapshot =
                chapterFlowRuntime.CapturePresentationSnapshot();
            if (snapshot.IsBossPortalAvailable)
            {
                EnsurePortal(snapshot.ChapterNumber);
                return;
            }

            missingPrefabReported = false;
            RemovePortal();
        }

        private void EnsurePortal(int chapterNumber)
        {
            if (activePortal != null)
                return;

            if (portalPrefab == null)
            {
                if (!missingPrefabReported)
                {
                    Debug.LogWarning(
                        $"{nameof(RunChapterBossPortalSpawner)} cannot create " +
                        "a portal because its prefab is missing.",
                        this);
                    missingPrefabReported = true;
                }

                return;
            }

            RebindLocalPlayer();
            if (localPlayer == null)
                return;

            Vector3 position = ResolveSpawnPosition(localPlayer);
            Quaternion rotation = ResolveSpawnRotation(
                localPlayer,
                position);
            activePortal = Instantiate(portalPrefab, position, rotation);
            activePortal.Initialize(chapterFlowRuntime, chapterNumber);
            activePortal.gameObject.SetActive(true);
            PortalSpawned?.Invoke(activePortal);
        }

        private Vector3 ResolveSpawnPosition(Transform player)
        {
            Vector3 direction = Vector3.ProjectOnPlane(
                -player.right,
                Vector3.up).normalized;
            if (direction.sqrMagnitude <= 0.0001f)
                direction = Vector3.left;

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

        private Quaternion ResolveSpawnRotation(
            Transform player,
            Vector3 portalPosition)
        {
            Vector3 directionToPlayer = Vector3.ProjectOnPlane(
                player.position - portalPosition,
                Vector3.up);
            if (directionToPlayer.sqrMagnitude <= 0.0001f)
                return portalPrefab.transform.rotation;

            return Quaternion.LookRotation(
                directionToPlayer.normalized,
                Vector3.up);
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
            chapterFlowRuntime ??= GetComponent<RunChapterFlowRuntime>();
        }

        private void OnValidate()
        {
            spawnDistance = Mathf.Max(0f, spawnDistance);
            navMeshSampleDistance = Mathf.Max(0f, navMeshSampleDistance);
        }
    }
}
