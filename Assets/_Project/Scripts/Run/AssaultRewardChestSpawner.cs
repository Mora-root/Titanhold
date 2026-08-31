using System;
using System.Collections.Generic;
using UnityEngine;

namespace Titanhold.Run
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RunFlowRuntime))]
    [RequireComponent(typeof(AssaultEnemyRegistry))]
    [RequireComponent(typeof(AssaultTargetRegistry))]
    public sealed class AssaultRewardChestSpawner : MonoBehaviour
    {
        [SerializeField] private RunFlowRuntime runFlowRuntime;
        [SerializeField] private AssaultEnemyRegistry enemyRegistry;
        [SerializeField] private AssaultTargetRegistry targetRegistry;
        [SerializeField] private LootTable rewardTable;
        [SerializeField] private AssaultRewardChestInteractable chestPrefab;
        [SerializeField] private Transform spawnPoint;

        private AssaultRewardChestInteractable activeChest;
        private bool missingConfigurationReported;

        public AssaultRewardChestInteractable ActiveChest => activeChest;
        public bool HasActiveChest => activeChest != null;

        public event Action<AssaultRewardResult> RewardPrepared;
        public event Action<AssaultRewardChestInteractable> ChestSpawned;
        public event Action ChestRemoved;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            if (enemyRegistry != null)
                enemyRegistry.EncounterCompleted += HandleEncounterCompleted;
            if (runFlowRuntime != null)
                runFlowRuntime.StateChanged += HandleRunFlowStateChanged;
        }

        private void Start()
        {
            SynchronizeWithState();
        }

        private void OnDisable()
        {
            if (enemyRegistry != null)
                enemyRegistry.EncounterCompleted -= HandleEncounterCompleted;
            if (runFlowRuntime != null)
                runFlowRuntime.StateChanged -= HandleRunFlowStateChanged;

            RemoveChest();
        }

        private void HandleEncounterCompleted(AssaultEncounterResult result)
        {
            ResolveReferences();
            if (runFlowRuntime == null ||
                !result.Success ||
                !result.EncounterCompleted)
            {
                return;
            }

            AssaultEncounterState encounter =
                runFlowRuntime.AssaultEncounter.State;
            if (!encounter.IsCompleted ||
                runFlowRuntime.State.Phase != RunPhase.Intermission)
            {
                return;
            }

            if (runFlowRuntime.AssaultReward.State.HasReward)
            {
                SynchronizeWithState();
                return;
            }

            if (rewardTable == null)
            {
                ReportMissingConfiguration();
                return;
            }

            int rollSeed = UnityEngine.Random.Range(1, int.MaxValue);
            List<LootDropResult> drops = rewardTable.Roll(
                new System.Random(rollSeed));
            AssaultRewardResult rewardResult =
                runFlowRuntime.AssaultReward.TryPrepare(
                    new PrepareAssaultRewardCommand(
                        encounter.EncounterId,
                        encounter.RoundNumber,
                        rollSeed,
                        drops));
            RewardPrepared?.Invoke(rewardResult);

            if (!rewardResult.Success)
            {
                Debug.LogWarning(
                    $"{nameof(AssaultRewardChestSpawner)} could not prepare the reward: {rewardResult.Error}.",
                    this);
                return;
            }

            SynchronizeWithState();
        }

        private void HandleRunFlowStateChanged(RunFlowState state)
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
            if (state.Phase == RunPhase.Intermission &&
                runFlowRuntime.AssaultReward.State.HasReward &&
                !runFlowRuntime.AssaultReward.State.IsClaimed)
            {
                EnsureChest(runFlowRuntime.AssaultReward.State);
                return;
            }

            missingConfigurationReported = false;
            RemoveChest();
        }

        private void EnsureChest(AssaultRewardState rewardState)
        {
            if (activeChest != null &&
                activeChest.EncounterId == rewardState.EncounterId &&
                activeChest.ExpectedRound == rewardState.RoundNumber)
            {
                return;
            }

            RemoveChest();
            ResolveReferences();
            if (chestPrefab == null ||
                spawnPoint == null ||
                targetRegistry == null)
            {
                ReportMissingConfiguration();
                return;
            }

            activeChest = Instantiate(
                chestPrefab,
                spawnPoint.position,
                spawnPoint.rotation);
            activeChest.Initialize(
                runFlowRuntime,
                targetRegistry,
                rewardState.EncounterId,
                rewardState.RoundNumber);
            activeChest.gameObject.SetActive(true);
            ChestSpawned?.Invoke(activeChest);
        }

        private void RemoveChest()
        {
            if (activeChest == null)
                return;

            Destroy(activeChest.gameObject);
            activeChest = null;
            ChestRemoved?.Invoke();
        }

        private void ReportMissingConfiguration()
        {
            if (missingConfigurationReported)
                return;

            Debug.LogWarning(
                $"{nameof(AssaultRewardChestSpawner)} cannot create a reward because its wiring is incomplete.",
                this);
            missingConfigurationReported = true;
        }

        private void ResolveReferences()
        {
            runFlowRuntime ??= GetComponent<RunFlowRuntime>();
            enemyRegistry ??= GetComponent<AssaultEnemyRegistry>();
            targetRegistry ??= GetComponent<AssaultTargetRegistry>();
        }
    }
}
