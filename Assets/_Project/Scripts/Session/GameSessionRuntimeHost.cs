using UnityEngine;
using Titanhold.Combat.Abilities;
using Titanhold.Run;

namespace Titanhold.Session
{
    [DisallowMultipleComponent]
    public sealed class GameSessionRuntimeHost : MonoBehaviour
    {
        [SerializeField] private ItemDefinitionCatalog itemDefinitions;
        [SerializeField] private AbilityDefinitionCatalog abilityDefinitions;
        [SerializeField]
        private RunStartingAbilityPoolCatalog startingAbilityPools;
        [SerializeField]
        private RunCombatResourceLoadoutCatalog combatResourceLoadouts;
        [SerializeField] private RunProgressionDefinition runProgression;
        [SerializeField]
        private RunConclusionRewardDefinition conclusionRewards;

        private static GameSessionRuntimeHost activeHost;

        public GameSessionRuntime Runtime { get; private set; }
        public bool IsInitialized => Runtime != null;
        public ItemDefinitionCatalog ItemDefinitions => itemDefinitions;
        public AbilityDefinitionCatalog AbilityDefinitions => abilityDefinitions;
        public RunStartingAbilityPoolCatalog StartingAbilityPools =>
            startingAbilityPools;
        public RunCombatResourceLoadoutCatalog CombatResourceLoadouts =>
            combatResourceLoadouts;
        public RunProgressionDefinition RunProgression => runProgression;
        public RunConclusionRewardDefinition ConclusionRewards =>
            conclusionRewards;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            ItemDefinitionCatalog definitions,
            AbilityDefinitionCatalog abilities,
            RunStartingAbilityPoolCatalog abilityPools,
            RunProgressionDefinition progression,
            RunConclusionRewardDefinition rewards)
        {
            itemDefinitions = definitions;
            abilityDefinitions = abilities;
            startingAbilityPools = abilityPools;
            runProgression = progression;
            conclusionRewards = rewards;
        }

        public void ConfigureCombatResourceLoadoutsForEditor(
            RunCombatResourceLoadoutCatalog loadouts)
        {
            combatResourceLoadouts = loadouts;
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            activeHost = null;
        }

        private void Awake()
        {
            if (!Application.isPlaying)
                return;

            if (activeHost != null && activeHost != this)
            {
                Destroy(gameObject);
                return;
            }

            if (transform.parent != null)
            {
                Debug.LogError(
                    $"{nameof(GameSessionRuntimeHost)} must be placed on a root GameObject.",
                    this);
                enabled = false;
                return;
            }

            if (itemDefinitions == null)
            {
                Debug.LogError(
                    $"{nameof(GameSessionRuntimeHost)} requires an item definition catalog.",
                    this);
                enabled = false;
                return;
            }

            if (!itemDefinitions.IsValid)
            {
                Debug.LogError(itemDefinitions.ValidationError, itemDefinitions);
                enabled = false;
                return;
            }

            if (abilityDefinitions == null || !abilityDefinitions.IsValid)
            {
                Debug.LogError(
                    abilityDefinitions != null
                        ? abilityDefinitions.ValidationError
                        : $"{nameof(GameSessionRuntimeHost)} requires an ability definition catalog.",
                    abilityDefinitions != null ? abilityDefinitions : this);
                enabled = false;
                return;
            }

            if (startingAbilityPools != null &&
                (!startingAbilityPools.IsValid ||
                 startingAbilityPools.AbilityCatalog != abilityDefinitions))
            {
                string detail = !startingAbilityPools.IsValid
                    ? startingAbilityPools.ValidationError
                    : "The starting ability pool catalog references a different " +
                      "ability definition catalog.";
                Debug.LogError(
                    $"{nameof(GameSessionRuntimeHost)} has invalid starting " +
                    $"ability pools: {detail}",
                    startingAbilityPools);
                enabled = false;
                return;
            }

            if (combatResourceLoadouts != null &&
                !combatResourceLoadouts.IsValid)
            {
                Debug.LogError(
                    $"{nameof(GameSessionRuntimeHost)} has invalid combat " +
                    $"resource loadouts: " +
                    combatResourceLoadouts.ValidationError,
                    combatResourceLoadouts);
                enabled = false;
                return;
            }

            if (runProgression == null || !runProgression.IsValid)
            {
                Debug.LogError(
                    $"{nameof(GameSessionRuntimeHost)} requires a valid run progression definition.",
                    this);
                enabled = false;
                return;
            }

            RunConclusionRewardPolicy rewardPolicy = null;
            string rewardError = "Definition reference is missing.";
            if (conclusionRewards == null ||
                !conclusionRewards.TryBuildPolicy(
                    out rewardPolicy,
                    out rewardError))
            {
                Debug.LogError(
                    $"{nameof(GameSessionRuntimeHost)} requires valid conclusion rewards: " +
                    rewardError,
                    this);
                enabled = false;
                return;
            }

            Runtime = new GameSessionRuntime(
                itemDefinitions,
                rewardPolicy,
                runExperienceCurve: runProgression.BuildCurve(),
                startingAbilityPools: startingAbilityPools,
                combatResourceLoadouts: combatResourceLoadouts);
            activeHost = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (activeHost == this)
                activeHost = null;
        }
    }
}
