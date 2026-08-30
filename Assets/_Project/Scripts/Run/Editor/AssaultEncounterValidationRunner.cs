using System;
using Titanhold.Combat;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Run.Editor
{
    public static class AssaultEncounterValidationRunner
    {
        [MenuItem("Tools/Titanhold/Validate Assault Encounter")]
        public static void Validate()
        {
            try
            {
                ValidateStartRules();
                ValidateProgressiveSpawnLifecycle();
                ValidateNextRoundReplacement();
                Debug.Log("Assault Encounter validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"Assault Encounter validation failed: {exception}");
            }
        }

        private static void ValidateStartRules()
        {
            RunFlowService flow = CreateFlow();
            AssaultEncounterApplicationService encounter =
                new AssaultEncounterApplicationService(flow);
            AssaultEncounterId encounterId = new AssaultEncounterId("assault:round-1");

            Assert(
                encounter.TryBegin(new BeginAssaultEncounterCommand(
                    encounterId,
                    1,
                    3)).Error == AssaultEncounterError.InvalidPhase,
                "Encounter started before the Assault transition.");

            PrepareAssaultTransition(flow);

            Assert(
                encounter.TryBegin(new BeginAssaultEncounterCommand(
                    default,
                    1,
                    3)).Error == AssaultEncounterError.InvalidEncounterId,
                "Encounter accepted an invalid id.");
            Assert(
                encounter.TryBegin(new BeginAssaultEncounterCommand(
                    encounterId,
                    2,
                    3)).Error == AssaultEncounterError.InvalidExpectedRound,
                "Encounter accepted a stale round.");
            Assert(
                encounter.TryBegin(new BeginAssaultEncounterCommand(
                    encounterId,
                    1,
                    0)).Error == AssaultEncounterError.InvalidPlannedEnemyCount,
                "Encounter accepted an empty wave.");
            Assert(flow.State.Phase == RunPhase.TransitionToAssault,
                "Rejected starts mutated Run Flow.");
        }

        private static void ValidateProgressiveSpawnLifecycle()
        {
            RunFlowService flow = CreateFlow();
            PrepareAssaultTransition(flow);
            AssaultEncounterApplicationService encounter =
                new AssaultEncounterApplicationService(flow);
            AssaultEncounterId encounterId = new AssaultEncounterId("assault:round-1");
            int stateChangeCount = 0;
            encounter.StateChanged += _ => stateChangeCount++;

            AssaultEncounterResult started = encounter.TryBegin(
                new BeginAssaultEncounterCommand(encounterId, 1, 3));
            Assert(started.Success, "Valid encounter did not start.");
            Assert(flow.State.Phase == RunPhase.Assault,
                "Run Flow did not enter Assault.");

            CombatActorReference enemyOne = Enemy("enemy:assault-1");
            CombatActorReference enemyTwo = Enemy("enemy:assault-2");
            CombatActorReference enemyThree = Enemy("enemy:assault-3");
            Assert(encounter.TryRegisterSpawn(
                    new AssaultEnemyCommand(encounterId, enemyOne)).Success,
                "First enemy was not registered.");
            Assert(encounter.TryRegisterSpawn(
                    new AssaultEnemyCommand(encounterId, enemyTwo)).Success,
                "Second enemy was not registered.");
            Assert(encounter.TryRegisterSpawn(
                    new AssaultEnemyCommand(encounterId, enemyOne)).Error ==
                   AssaultEncounterError.DuplicateEnemy,
                "Duplicate enemy was accepted.");

            Assert(encounter.TryRegisterDefeat(
                    new AssaultEnemyCommand(encounterId, enemyOne)).Success,
                "First enemy defeat was not registered.");
            Assert(encounter.TryRegisterSpawn(
                    new AssaultEnemyCommand(encounterId, enemyOne)).Error ==
                   AssaultEncounterError.DuplicateEnemy,
                "Defeated enemy id was reused within the encounter.");
            Assert(encounter.TryRegisterDefeat(
                    new AssaultEnemyCommand(encounterId, enemyTwo)).Success,
                "Second enemy defeat was not registered.");
            Assert(flow.State.Phase == RunPhase.Assault,
                "Encounter completed during a progressive-spawn gap.");
            Assert(encounter.State.AliveEnemyCount == 0 &&
                   !encounter.State.IsSpawnSequenceCompleted,
                "Progressive-spawn gap state is inconsistent.");

            Assert(encounter.TryRegisterSpawn(
                    new AssaultEnemyCommand(encounterId, enemyThree)).Success,
                "Final planned enemy was not registered.");
            Assert(encounter.State.IsSpawnSequenceCompleted,
                "Spawn sequence did not close at the planned count.");
            Assert(encounter.TryRegisterSpawn(
                    new AssaultEnemyCommand(encounterId, Enemy("enemy:overflow"))).Error ==
                   AssaultEncounterError.SpawnLimitReached,
                "Encounter exceeded its planned spawn count.");

            AssaultEncounterResult completed = encounter.TryRegisterDefeat(
                new AssaultEnemyCommand(encounterId, enemyThree));
            Assert(completed.Success && completed.EncounterCompleted,
                "Final defeat did not complete the encounter.");
            Assert(encounter.State.IsCompleted &&
                   encounter.State.DefeatedEnemyCount == 3 &&
                   encounter.State.AliveEnemyCount == 0,
                "Completed encounter counters are inconsistent.");
            Assert(flow.State.Phase == RunPhase.Intermission,
                "Completed encounter did not enter Intermission.");
            Assert(stateChangeCount == 7,
                "Unexpected Assault encounter notification count.");
            Assert(encounter.TryRegisterDefeat(
                    new AssaultEnemyCommand(encounterId, enemyThree)).Error ==
                   AssaultEncounterError.EncounterNotActive,
                "Completed encounter accepted another defeat.");
        }

        private static void ValidateNextRoundReplacement()
        {
            RunFlowService flow = CreateFlow();
            AssaultEncounterApplicationService encounter =
                new AssaultEncounterApplicationService(flow);
            AssaultEncounterId firstId = new AssaultEncounterId("assault:round-1");
            CompleteSingleEnemyEncounter(flow, encounter, firstId, "enemy:round-1");

            Assert(flow.TryBeginReturnToExploration().Success,
                "Could not begin return after the first encounter.");
            Assert(flow.TryResumeExploration().Success,
                "Could not resume exploration after the first encounter.");
            Assert(flow.State.RoundNumber == 2,
                "Run Flow did not advance to round two.");

            PrepareAssaultTransition(flow);
            AssaultEncounterId secondId = new AssaultEncounterId("assault:round-2");
            Assert(encounter.TryBegin(
                    new BeginAssaultEncounterCommand(secondId, 2, 1)).Success,
                "Second-round encounter did not replace completed state.");
            Assert(encounter.State.EncounterId == secondId &&
                   encounter.State.RoundNumber == 2 &&
                   encounter.State.SpawnedEnemyCount == 0 &&
                   encounter.State.DefeatedEnemyCount == 0,
                "Second-round encounter retained stale counters.");
            Assert(encounter.TryRegisterSpawn(
                    new AssaultEnemyCommand(firstId, Enemy("enemy:stale"))).Error ==
                   AssaultEncounterError.StaleEncounter,
                "Second-round encounter accepted a stale command.");
        }

        private static void CompleteSingleEnemyEncounter(
            RunFlowService flow,
            AssaultEncounterApplicationService encounter,
            AssaultEncounterId encounterId,
            string enemyId)
        {
            PrepareAssaultTransition(flow);
            Assert(encounter.TryBegin(
                    new BeginAssaultEncounterCommand(
                        encounterId,
                        flow.State.RoundNumber,
                        1)).Success,
                "Single-enemy encounter did not start.");
            CombatActorReference enemy = Enemy(enemyId);
            Assert(encounter.TryRegisterSpawn(
                    new AssaultEnemyCommand(encounterId, enemy)).Success,
                "Single enemy was not registered.");
            Assert(encounter.TryRegisterDefeat(
                    new AssaultEnemyCommand(encounterId, enemy)).EncounterCompleted,
                "Single-enemy encounter did not complete.");
        }

        private static void PrepareAssaultTransition(RunFlowService flow)
        {
            ExplorationKillBatchResult threat = flow.TryRegisterExplorationKill(
                new ExplorationKillContribution(flow.State.MaxThreat, 0));
            Assert(threat.Success && threat.PortalOpened,
                "Could not prepare PortalOpen.");
            Assert(flow.TryBeginAssaultTransition().Success,
                "Could not prepare TransitionToAssault.");
        }

        private static RunFlowService CreateFlow()
        {
            return new RunFlowService(
                RunFlowConfiguration.CreateVerticalSliceDefaults());
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
