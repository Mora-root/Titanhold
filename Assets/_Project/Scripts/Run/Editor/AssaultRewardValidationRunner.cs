using System;
using Titanhold.Combat;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Run.Editor
{
    public static class AssaultRewardValidationRunner
    {
        [MenuItem("Tools/Titanhold/Validate Assault Reward")]
        public static void Validate()
        {
            try
            {
                ValidateRewardLifecycle();
                Debug.Log("Assault Reward validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Assault Reward validation failed: {exception}");
            }
        }

        private static void ValidateRewardLifecycle()
        {
            RunFlowService flow = new RunFlowService(
                new RunFlowConfiguration(1f, 10, 0.1f, 0.05f));
            AssaultEncounterApplicationService encounter =
                new AssaultEncounterApplicationService(flow);
            using AssaultRewardApplicationService reward =
                new AssaultRewardApplicationService(flow);
            AssaultEncounterId encounterId =
                new AssaultEncounterId("assault:reward-validation");
            LootDropResult[] drops = { LootDropResult.Gold(15) };

            Assert(
                reward.TryPrepare(new PrepareAssaultRewardCommand(
                    encounterId,
                    1,
                    12345,
                    drops)).Error == AssaultRewardError.InvalidPhase,
                "Reward was prepared before Intermission.");

            CompleteSingleEnemyEncounter(flow, encounter, encounterId);
            Assert(flow.State.Phase == RunPhase.Intermission,
                "Reward validation did not reach Intermission.");
            Assert(
                reward.TryPrepare(new PrepareAssaultRewardCommand(
                    encounterId,
                    1,
                    12345,
                    Array.Empty<LootDropResult>())).Error ==
                AssaultRewardError.InvalidDrops,
                "Empty reward was accepted.");

            int stateChanges = 0;
            reward.StateChanged += _ => stateChanges++;
            AssaultRewardResult prepared = reward.TryPrepare(
                new PrepareAssaultRewardCommand(
                    encounterId,
                    1,
                    12345,
                    drops));
            Assert(prepared.Success &&
                   reward.State.HasReward &&
                   !reward.State.IsClaimed &&
                   reward.State.RollSeed == 12345 &&
                   reward.State.Drops.Count == 1 &&
                   reward.State.Drops[0].GoldAmount == 15,
                "Valid reward snapshot was not prepared.");
            Assert(
                reward.TryPrepare(new PrepareAssaultRewardCommand(
                    encounterId,
                    1,
                    54321,
                    drops)).Error == AssaultRewardError.RewardAlreadyPrepared,
                "Prepared reward was replaced and could be rerolled.");

            CombatActorReference enemy = new CombatActorReference(
                "enemy:reward-validation",
                CombatActorKind.Enemy);
            Assert(
                reward.TryClaim(new ClaimAssaultRewardCommand(
                    encounterId,
                    1,
                    enemy)).Error == AssaultRewardError.InvalidClaimant,
                "Enemy claimed a player reward.");

            CombatActorReference player = new CombatActorReference(
                "player:reward-validation",
                CombatActorKind.Player);
            AssaultRewardResult claimed = reward.TryClaim(
                new ClaimAssaultRewardCommand(encounterId, 1, player));
            Assert(claimed.Success &&
                   reward.State.IsClaimed &&
                   reward.State.ClaimedBy == player,
                "Player did not claim the prepared reward.");
            Assert(
                reward.TryClaim(new ClaimAssaultRewardCommand(
                    encounterId,
                    1,
                    player)).Error == AssaultRewardError.RewardAlreadyClaimed,
                "Reward was claimed more than once.");
            Assert(stateChanges == 2,
                "Unexpected reward notification count before round advance.");

            Assert(flow.TryBeginReturnToExploration().Success,
                "Could not begin return after claiming the reward.");
            Assert(flow.TryResumeExploration().Success,
                "Could not resume exploration after claiming the reward.");
            Assert(!reward.State.HasReward &&
                   reward.State.Drops.Count == 0 &&
                   stateChanges == 3,
                "Previous-round reward was not cleared.");
        }

        private static void CompleteSingleEnemyEncounter(
            RunFlowService flow,
            AssaultEncounterApplicationService encounter,
            AssaultEncounterId encounterId)
        {
            Assert(flow.TryRegisterExplorationKill(
                    new ExplorationKillContribution(1f, 0)).Success,
                "Could not fill Threat for reward validation.");
            Assert(flow.TryBeginAssaultTransition().Success,
                "Could not begin Assault transition for reward validation.");
            Assert(encounter.TryBegin(new BeginAssaultEncounterCommand(
                    encounterId,
                    1,
                    1)).Success,
                "Could not begin encounter for reward validation.");

            CombatActorReference enemy = new CombatActorReference(
                "enemy:reward-validation-wave",
                CombatActorKind.Enemy);
            Assert(encounter.TryRegisterSpawn(
                    new AssaultEnemyCommand(encounterId, enemy)).Success,
                "Could not register reward-validation enemy.");
            Assert(encounter.TryRegisterDefeat(
                    new AssaultEnemyCommand(encounterId, enemy)).Success,
                "Could not complete reward-validation encounter.");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
