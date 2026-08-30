using System;
using System.Collections.Generic;
using Titanhold.Combat;
using UnityEngine;

namespace Titanhold.Run
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RunFlowRuntime))]
    public sealed class AssaultEnemyRegistry : MonoBehaviour
    {
        private readonly Dictionary<EnemyDeathNotifier, EnemyRegistration> registrations =
            new Dictionary<EnemyDeathNotifier, EnemyRegistration>();

        [SerializeField] private RunFlowRuntime runFlowRuntime;

        public int RegisteredEnemyCount => registrations.Count;

        public event Action<EnemyDeathNotifier, CombatActorReference> EnemyRegistered;
        public event Action<EnemyDeathNotifier, CombatActorReference> EnemyDefeated;
        public event Action<AssaultEncounterResult> EncounterCompleted;
        public event Action<EnemyDeathNotifier, AssaultEncounterResult> DefeatRejected;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnDisable()
        {
            ClearBindings();
        }

        public AssaultEnemyRegistryResult TryRegister(
            EnemyDeathNotifier notifier,
            AssaultEncounterId encounterId,
            CombatActorReference enemy)
        {
            if (notifier == null)
            {
                return AssaultEnemyRegistryResult.Failed(
                    AssaultEnemyRegistryError.InvalidNotifier);
            }

            if (registrations.ContainsKey(notifier))
            {
                return AssaultEnemyRegistryResult.Failed(
                    AssaultEnemyRegistryError.NotifierAlreadyRegistered);
            }

            ResolveReferences();
            if (runFlowRuntime == null)
            {
                return AssaultEnemyRegistryResult.Failed(
                    AssaultEnemyRegistryError.MissingRuntime);
            }

            AssaultEncounterResult encounterResult =
                runFlowRuntime.AssaultEncounter.TryRegisterSpawn(
                    new AssaultEnemyCommand(encounterId, enemy));
            if (!encounterResult.Success)
            {
                return AssaultEnemyRegistryResult.Failed(
                    AssaultEnemyRegistryError.ApplicationRejected,
                    encounterResult);
            }

            registrations.Add(
                notifier,
                new EnemyRegistration(encounterId, enemy));
            notifier.Died += HandleEnemyDied;
            EnemyRegistered?.Invoke(notifier, enemy);
            return AssaultEnemyRegistryResult.Succeeded(encounterResult);
        }

        public AssaultEnemyRegistryResult TryRegisterDefeat(
            EnemyDeathNotifier notifier)
        {
            if (notifier == null)
            {
                return AssaultEnemyRegistryResult.Failed(
                    AssaultEnemyRegistryError.InvalidNotifier);
            }

            if (!registrations.TryGetValue(
                    notifier,
                    out EnemyRegistration registration))
            {
                return AssaultEnemyRegistryResult.Failed(
                    AssaultEnemyRegistryError.NotifierNotRegistered);
            }

            notifier.Died -= HandleEnemyDied;
            registrations.Remove(notifier);

            if (runFlowRuntime == null)
            {
                AssaultEncounterResult missingRuntimeResult = default;
                DefeatRejected?.Invoke(notifier, missingRuntimeResult);
                return AssaultEnemyRegistryResult.Failed(
                    AssaultEnemyRegistryError.MissingRuntime,
                    missingRuntimeResult);
            }

            AssaultEncounterResult encounterResult =
                runFlowRuntime.AssaultEncounter.TryRegisterDefeat(
                    new AssaultEnemyCommand(
                        registration.EncounterId,
                        registration.Enemy));
            if (!encounterResult.Success)
            {
                DefeatRejected?.Invoke(notifier, encounterResult);
                return AssaultEnemyRegistryResult.Failed(
                    AssaultEnemyRegistryError.ApplicationRejected,
                    encounterResult);
            }

            EnemyDefeated?.Invoke(notifier, registration.Enemy);
            if (encounterResult.EncounterCompleted)
                EncounterCompleted?.Invoke(encounterResult);

            return AssaultEnemyRegistryResult.Succeeded(encounterResult);
        }

        private void HandleEnemyDied(EnemyDeathNotifier notifier)
        {
            TryRegisterDefeat(notifier);
        }

        private void ClearBindings()
        {
            foreach (EnemyDeathNotifier notifier in registrations.Keys)
            {
                if (notifier != null)
                    notifier.Died -= HandleEnemyDied;
            }

            registrations.Clear();
        }

        private void ResolveReferences()
        {
            if (runFlowRuntime == null)
                runFlowRuntime = GetComponent<RunFlowRuntime>();
        }

        private readonly struct EnemyRegistration
        {
            public EnemyRegistration(
                AssaultEncounterId encounterId,
                CombatActorReference enemy)
            {
                EncounterId = encounterId;
                Enemy = enemy;
            }

            public AssaultEncounterId EncounterId { get; }
            public CombatActorReference Enemy { get; }
        }
    }
}
