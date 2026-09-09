using System;
using Titanhold.Enemies;
using UnityEngine;

namespace Titanhold.Run
{
    [DisallowMultipleComponent]
    public sealed class RunFlowRuntime : MonoBehaviour
    {
        [Header("Vertical Slice Configuration")]
        [SerializeField, Min(0.01f)] private float maxThreat = 100f;
        [SerializeField, Min(1)] private int instabilityPointsPerLevel = 10;
        [SerializeField, Min(0f)] private float enemyHealthBonusPerRound = 0.20f;
        [SerializeField, Min(0f)] private float enemyDamageBonusPerRound = 0.10f;
        [SerializeField, Min(0f)] private float assaultHealthBonusPerLevel = 0.10f;
        [SerializeField, Min(0f)] private float assaultDamageBonusPerLevel = 0.05f;
        [SerializeField, Min(1)] private int regularRoundCount = 3;
        [SerializeField, Min(1)] private int startingRound = 1;
        [SerializeField] private EnemyDefinitionCatalog enemyDefinitionCatalog;

        private RunFlowService service;
        private ExplorationKillApplicationService killApplication;
        private RunPortalEntryApplicationService portalEntry;
        private AssaultEncounterApplicationService assaultEncounter;
        private AssaultRewardApplicationService assaultReward;

        public RunFlowService Service
        {
            get
            {
                EnsureInitialized();
                return service;
            }
        }

        public ExplorationKillApplicationService KillApplication
        {
            get
            {
                EnsureInitialized();
                return killApplication;
            }
        }

        public RunPortalEntryApplicationService PortalEntry
        {
            get
            {
                EnsureInitialized();
                return portalEntry;
            }
        }

        public AssaultEncounterApplicationService AssaultEncounter
        {
            get
            {
                EnsureInitialized();
                return assaultEncounter;
            }
        }

        public AssaultRewardApplicationService AssaultReward
        {
            get
            {
                EnsureInitialized();
                return assaultReward;
            }
        }

        public RunFlowState State => Service.State;
        public EnemyDefinitionCatalog EnemyDefinitions =>
            enemyDefinitionCatalog;

        public event Action<RunFlowState> StateChanged;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void Start()
        {
            InitializeAuthoredEnemyDefinitions();
        }

        private void OnDestroy()
        {
            assaultReward?.Dispose();

            if (service != null)
                service.StateChanged -= HandleStateChanged;
        }

        public void EnsureInitialized()
        {
            if (service != null)
                return;

            RunFlowConfiguration configuration = new RunFlowConfiguration(
                maxThreat,
                instabilityPointsPerLevel,
                enemyHealthBonusPerRound,
                enemyDamageBonusPerRound,
                assaultHealthBonusPerLevel,
                assaultDamageBonusPerLevel,
                regularRoundCount,
                startingRound);
            service = new RunFlowService(configuration);
            killApplication = new ExplorationKillApplicationService(service);
            portalEntry = new RunPortalEntryApplicationService(service);
            assaultEncounter = new AssaultEncounterApplicationService(service);
            assaultReward = new AssaultRewardApplicationService(service);
            service.StateChanged += HandleStateChanged;
        }

        private void InitializeAuthoredEnemyDefinitions()
        {
            if (enemyDefinitionCatalog == null ||
                !enemyDefinitionCatalog.IsValid)
            {
                string error = enemyDefinitionCatalog != null
                    ? enemyDefinitionCatalog.ValidationError
                    : "catalog is missing";
                Debug.LogError(
                    $"Could not initialize authored enemies: {error}.",
                    this);
                return;
            }

            GameObject[] roots = gameObject.scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                EnemyDefinitionBinding[] bindings =
                    roots[rootIndex]
                        .GetComponentsInChildren<EnemyDefinitionBinding>(true);
                for (int bindingIndex = 0;
                     bindingIndex < bindings.Length;
                     bindingIndex++)
                {
                    EnemyDefinitionBinding binding = bindings[bindingIndex];
                    if (binding.HasInitialized)
                        continue;

                    EnemyDefinitionInitializationResult result =
                        binding.TryInitialize(enemyDefinitionCatalog);
                    if (!result.Success)
                    {
                        Debug.LogError(
                            $"Could not initialize authored enemy " +
                            $"'{binding.gameObject.name}' from definition " +
                            $"'{binding.EnemyId}': {result.Error} " +
                            $"({result.ApplicationError}).",
                            binding);
                    }
                }
            }
        }

        private void HandleStateChanged(RunFlowState state)
        {
            StateChanged?.Invoke(state);
        }

        private void OnValidate()
        {
            maxThreat = Mathf.Max(0.01f, maxThreat);
            instabilityPointsPerLevel = Mathf.Max(1, instabilityPointsPerLevel);
            enemyHealthBonusPerRound = Mathf.Max(0f, enemyHealthBonusPerRound);
            enemyDamageBonusPerRound = Mathf.Max(0f, enemyDamageBonusPerRound);
            assaultHealthBonusPerLevel = Mathf.Max(0f, assaultHealthBonusPerLevel);
            assaultDamageBonusPerLevel = Mathf.Max(0f, assaultDamageBonusPerLevel);
            regularRoundCount = Mathf.Clamp(regularRoundCount, 1, int.MaxValue - 1);
            startingRound = Mathf.Min(startingRound, regularRoundCount + 1);
            startingRound = Mathf.Max(1, startingRound);
        }
    }
}
