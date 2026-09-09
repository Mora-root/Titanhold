using UnityEngine;

namespace Titanhold.Enemies
{
    [DisallowMultipleComponent]
    public sealed class EnemyBaseStatsReceiver :
        MonoBehaviour,
        IEnemyArchetypeReceiver
    {
        [SerializeField] private CharacterStats characterStats;
        [SerializeField] private Health health;
        [SerializeField] private EnemyCombat combat;
        [SerializeField] private EnemyMovement movement;
        [SerializeField] private EnemySensor sensor;

        private readonly EnemyBaseStatsApplicationService application = new();

        public bool HasRequiredReferences =>
            characterStats != null &&
            health != null &&
            combat != null &&
            movement != null &&
            sensor != null;
        public bool HasAppliedDefinition { get; private set; }
        public string AppliedEnemyId { get; private set; } = string.Empty;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            CharacterStats configuredCharacterStats,
            Health configuredHealth,
            EnemyCombat configuredCombat,
            EnemyMovement configuredMovement,
            EnemySensor configuredSensor)
        {
            characterStats = configuredCharacterStats;
            health = configuredHealth;
            combat = configuredCombat;
            movement = configuredMovement;
            sensor = configuredSensor;
        }
#endif

        private void Awake()
        {
            ResolveReferences();
        }

        public EnemyBaseStatsApplicationResult TryApply(
            EnemyArchetype archetype)
        {
            ResolveReferences();
            EnemyBaseStatsApplicationResult result = application.TryApply(
                archetype,
                new UnityGateway(
                    characterStats,
                    health,
                    combat,
                    movement,
                    sensor));
            if (!result.Success)
                return result;

            HasAppliedDefinition = true;
            AppliedEnemyId = result.EnemyId;
            return result;
        }

        private void ResolveReferences()
        {
            characterStats ??= GetComponent<CharacterStats>();
            health ??= GetComponent<Health>();
            combat ??= GetComponent<EnemyCombat>();
            movement ??= GetComponent<EnemyMovement>();
            sensor ??= GetComponent<EnemySensor>();
        }

        private sealed class UnityGateway : IEnemyBaseStatsGateway
        {
            private readonly CharacterStats characterStats;
            private readonly Health health;
            private readonly EnemyCombat combat;
            private readonly EnemyMovement movement;
            private readonly EnemySensor sensor;

            public UnityGateway(
                CharacterStats characterStats,
                Health health,
                EnemyCombat combat,
                EnemyMovement movement,
                EnemySensor sensor)
            {
                this.characterStats = characterStats;
                this.health = health;
                this.combat = combat;
                this.movement = movement;
                this.sensor = sensor;
            }

            public bool IsReady =>
                characterStats != null &&
                health != null &&
                combat != null &&
                movement != null &&
                sensor != null;

            public void SetBaseStat(StatType statType, float value)
            {
                characterStats.Block.SetBaseValue(statType, value);
            }

            public void ConfigureCombat(float baseAttacksPerSecond)
            {
                combat.UseDefinitionStats(
                    characterStats,
                    baseAttacksPerSecond);
            }

            public void ConfigureMovement(float rotationSpeed)
            {
                movement.UseDefinitionStats(characterStats, rotationSpeed);
            }

            public void ConfigureDetection(float detectionRange)
            {
                sensor.SetDetectionRange(detectionRange);
            }

            public void RestoreFullHealth()
            {
                health.RestoreFull();
            }
        }
    }
}
