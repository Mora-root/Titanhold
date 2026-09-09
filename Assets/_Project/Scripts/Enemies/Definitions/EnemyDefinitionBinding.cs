using UnityEngine;

namespace Titanhold.Enemies
{
    [DisallowMultipleComponent]
    public sealed class EnemyDefinitionBinding : MonoBehaviour
    {
        [SerializeField] private string enemyId;
        [SerializeField] private EnemyBaseStatsReceiver baseStatsReceiver;

        private readonly EnemyDefinitionInitializationService initialization =
            new();

        public string EnemyId => enemyId?.Trim() ?? string.Empty;
        public bool HasInitialized { get; private set; }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string configuredEnemyId,
            EnemyBaseStatsReceiver configuredBaseStatsReceiver)
        {
            enemyId = configuredEnemyId;
            baseStatsReceiver = configuredBaseStatsReceiver;
        }
#endif

        private void Awake()
        {
            ResolveReferences();
        }

        public EnemyDefinitionInitializationResult TryInitialize(
            IEnemyDefinitionResolver resolver)
        {
            ResolveReferences();
            EnemyDefinitionInitializationResult result =
                initialization.TryInitialize(
                    enemyId,
                    resolver,
                    baseStatsReceiver);
            if (result.Success)
                HasInitialized = true;

            return result;
        }

        private void ResolveReferences()
        {
            baseStatsReceiver ??= GetComponent<EnemyBaseStatsReceiver>();
        }
    }
}
