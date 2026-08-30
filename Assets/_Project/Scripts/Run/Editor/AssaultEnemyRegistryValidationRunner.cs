using System;
using Titanhold.Combat;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Run.Editor
{
    public static class AssaultEnemyRegistryValidationRunner
    {
        [MenuItem("Tools/Titanhold/Validate Assault Enemy Registry")]
        public static void Validate()
        {
            try
            {
                ValidateRegistrationAndDeathForwarding();
                Debug.Log("Assault Enemy Registry validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"Assault Enemy Registry validation failed: {exception}");
            }
        }

        private static void ValidateRegistrationAndDeathForwarding()
        {
            GameObject runtimeObject = new GameObject("AssaultRegistry_Runtime");
            GameObject firstEnemyObject = new GameObject("AssaultRegistry_EnemyOne");
            GameObject duplicateEnemyObject = new GameObject("AssaultRegistry_DuplicateEnemy");
            GameObject secondEnemyObject = new GameObject("AssaultRegistry_EnemyTwo");

            try
            {
                RunFlowRuntime runtime = runtimeObject.AddComponent<RunFlowRuntime>();
                AssaultEnemyRegistry registry =
                    runtimeObject.AddComponent<AssaultEnemyRegistry>();
                AssaultEncounterId encounterId = BeginEncounter(runtime, 2);
                EnemyDeathNotifier firstNotifier =
                    CreateDeathNotifier(firstEnemyObject);
                EnemyDeathNotifier duplicateNotifier =
                    CreateDeathNotifier(duplicateEnemyObject);
                EnemyDeathNotifier secondNotifier =
                    CreateDeathNotifier(secondEnemyObject);
                CombatActorReference firstEnemy = Enemy("enemy:registry-1");
                CombatActorReference secondEnemy = Enemy("enemy:registry-2");
                int registeredCount = 0;
                int defeatedCount = 0;
                int completedCount = 0;
                int rejectedDefeatCount = 0;
                registry.EnemyRegistered += (_, _) => registeredCount++;
                registry.EnemyDefeated += (_, _) => defeatedCount++;
                registry.EncounterCompleted += _ => completedCount++;
                registry.DefeatRejected += (_, _) => rejectedDefeatCount++;

                AssaultEnemyRegistryResult firstRegistration = registry.TryRegister(
                    firstNotifier,
                    encounterId,
                    firstEnemy);
                Assert(firstRegistration.Success,
                    "First runtime enemy was not registered.");
                Assert(registry.TryRegister(
                        firstNotifier,
                        encounterId,
                        secondEnemy).Error ==
                       AssaultEnemyRegistryError.NotifierAlreadyRegistered,
                    "The same notifier was registered twice.");

                AssaultEnemyRegistryResult duplicateActor = registry.TryRegister(
                    duplicateNotifier,
                    encounterId,
                    firstEnemy);
                Assert(!duplicateActor.Success &&
                       duplicateActor.Error == AssaultEnemyRegistryError.ApplicationRejected &&
                       duplicateActor.EncounterResult.Error ==
                       AssaultEncounterError.DuplicateEnemy,
                    "Duplicate actor id bypassed the application service.");

                Assert(registry.TryRegisterDefeat(firstNotifier).Success,
                    "First death was not accepted by the registry.");
                Assert(runtime.State.Phase == RunPhase.Assault,
                    "Assault completed before every planned enemy spawned.");
                Assert(registry.RegisteredEnemyCount == 0 &&
                       runtime.AssaultEncounter.State.DefeatedEnemyCount == 1,
                    "First death was not forwarded exactly once.");

                AssaultEnemyRegistryResult secondRegistration = registry.TryRegister(
                    secondNotifier,
                    encounterId,
                    secondEnemy);
                Assert(secondRegistration.Success,
                    "Second runtime enemy was not registered.");
                Assert(registry.TryRegisterDefeat(secondNotifier).Success,
                    "Second death was not accepted by the registry.");

                Assert(runtime.State.Phase == RunPhase.Intermission,
                    "Final runtime enemy death did not enter Intermission.");
                Assert(runtime.AssaultEncounter.State.IsCompleted &&
                       registry.RegisteredEnemyCount == 0,
                    "Runtime registry retained completed encounter state.");
                Assert(registeredCount == 2 &&
                       defeatedCount == 2 &&
                       completedCount == 1 &&
                       rejectedDefeatCount == 0,
                    "Runtime registry emitted unexpected events.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(secondEnemyObject);
                UnityEngine.Object.DestroyImmediate(duplicateEnemyObject);
                UnityEngine.Object.DestroyImmediate(firstEnemyObject);
                UnityEngine.Object.DestroyImmediate(runtimeObject);
            }
        }

        private static AssaultEncounterId BeginEncounter(
            RunFlowRuntime runtime,
            int plannedEnemyCount)
        {
            ExplorationKillBatchResult threshold = runtime.Service.TryRegisterExplorationKill(
                new ExplorationKillContribution(runtime.State.MaxThreat, 0));
            Assert(threshold.Success && threshold.PortalOpened,
                "Could not prepare PortalOpen.");
            Assert(runtime.Service.TryBeginAssaultTransition().Success,
                "Could not prepare TransitionToAssault.");

            AssaultEncounterId encounterId =
                new AssaultEncounterId("assault:registry-validation");
            Assert(runtime.AssaultEncounter.TryBegin(
                    new BeginAssaultEncounterCommand(
                        encounterId,
                        runtime.State.RoundNumber,
                        plannedEnemyCount)).Success,
                "Could not start the registry validation encounter.");
            return encounterId;
        }

        private static EnemyDeathNotifier CreateDeathNotifier(GameObject enemyObject)
        {
            EnemyDeathNotifier notifier =
                enemyObject.AddComponent<EnemyDeathNotifier>();
            Health health = enemyObject.GetComponent<Health>();
            health.RestoreFull();
            return notifier;
        }

        private static CombatActorReference Enemy(string actorId)
        {
            return new CombatActorReference(actorId, CombatActorKind.Enemy);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
