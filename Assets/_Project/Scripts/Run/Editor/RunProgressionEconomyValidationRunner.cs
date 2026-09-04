using System;
using System.Collections.Generic;
using Titanhold.Combat;
using Titanhold.Progression;
using Titanhold.Session;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Run.Editor
{
    public static class RunProgressionEconomyValidationRunner
    {
        [MenuItem("Tools/Titanhold/Validate Run Progression Economy")]
        public static void ValidateFromMenu()
        {
            try
            {
                ValidateRunProgression();
                ValidateProgressionDefinition();
                ValidateCombatExperienceRouting();
                ValidateAccountCrystals();
                ValidateConclusionRewards();
                Debug.Log("Run Progression Economy validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Run Progression Economy validation failed: {exception}");
            }
        }

        private static void ValidateCombatExperienceRouting()
        {
            GameObject player = null;
            GameObject adapterObject = null;
            GameObject firstEnemy = null;
            GameObject secondEnemy = null;
            GameObject rejectedEnemy = null;
            GameObject goldPickup = null;
            GameObject pickerProxy = null;
            try
            {
                player = new GameObject("RunProgressionValidation_Player");
                PlayerInventory inventory =
                    player.AddComponent<PlayerInventory>();
                PlayerEquipmentRuntime equipment =
                    player.AddComponent<PlayerEquipmentRuntime>();
                PlayerExperience experience =
                    player.AddComponent<PlayerExperience>();
                PlayerGold gold = player.AddComponent<PlayerGold>();
                PlayerCombat combat = player.AddComponent<PlayerCombat>();
                inventory.EnsureInitialized();
                equipment.SetPlayerInventory(inventory);

                RunSceneParticipantBinding binding = new(
                    "player:validation",
                    "character:validation",
                    inventory,
                    equipment,
                    experience,
                    gold);
                RunProgressionService progression = new(
                    new RunExperienceCurve(new[] { 20, 50 }));
                Assert(progression.TryRegisterParticipant(
                           new RunParticipantIdentity(
                               binding.PlayerId,
                               binding.CharacterId)).Success,
                    "Could not prepare combat experience participant.");

                adapterObject = new GameObject(
                    "RunProgressionValidation_Adapter");
                RunProgressionCombatAdapter adapter =
                    adapterObject.AddComponent<RunProgressionCombatAdapter>();
                Assert(adapter.TryInitialize(
                           progression,
                           new[] { binding },
                           sessionBacked: false),
                    "Could not initialize combat experience adapter.");

                firstEnemy = CreateRewardTarget(
                    "RunProgressionValidation_EnemyOne",
                    10,
                    out RunProgressionValidationDamageable firstTarget);
                secondEnemy = CreateRewardTarget(
                    "RunProgressionValidation_EnemyTwo",
                    15,
                    out RunProgressionValidationDamageable secondTarget);

                CombatActorReference playerActor = combat.ActorReference;
                CombatExecutionId executionId = CombatExecutionId.New();
                CombatExecutionReport report = new(
                    executionId,
                    new List<DamageTargetResolution>
                    {
                        CreateKilledResolution(
                            firstTarget,
                            executionId,
                            playerActor),
                        CreateKilledResolution(
                            secondTarget,
                            executionId,
                            playerActor)
                    });
                Assert(adapter.TryApplyReport(
                           binding.PlayerId,
                           playerActor,
                           report,
                           out RunProgressionResult award) &&
                       award.Success &&
                       award.ExperienceApplied == 25 &&
                       award.LevelsGained == 1 &&
                       award.State.Level == 2 &&
                       award.State.Experience == 5,
                    "Combat report did not award summed run experience.");
                Assert(!adapter.TryApplyReport(
                           binding.PlayerId,
                           playerActor,
                           report,
                           out _),
                    "Combat report awarded experience more than once.");

                rejectedEnemy = CreateRewardTarget(
                    "RunProgressionValidation_RejectedEnemy",
                    30,
                    out RunProgressionValidationDamageable rejectedTarget);
                CombatActorReference otherActor = new(
                    "player:other-combat-actor",
                    CombatActorKind.Player);
                CombatExecutionId rejectedExecution = CombatExecutionId.New();
                CombatExecutionReport rejectedReport = new(
                    rejectedExecution,
                    new[]
                    {
                        CreateKilledResolution(
                            rejectedTarget,
                            rejectedExecution,
                            otherActor)
                    });
                Assert(!adapter.TryApplyReport(
                           binding.PlayerId,
                           playerActor,
                           rejectedReport,
                           out _) &&
                       award.State.Level == 2 &&
                       award.State.Experience == 5,
                    "Combat experience accepted a mismatched combat actor.");

                pickerProxy = new GameObject(
                    "RunProgressionValidation_PickerProxy");
                pickerProxy.transform.SetParent(player.transform);
                goldPickup = new GameObject(
                    "RunProgressionValidation_GoldPickup");
                GoldLootReward goldReward =
                    goldPickup.AddComponent<GoldLootReward>();
                goldReward.SetAmount(17);
                Assert(goldReward.Collect(pickerProxy) &&
                       progression.TryGetParticipant(
                           binding.PlayerId,
                           out RunParticipantProgressionState goldState) &&
                       goldState.Gold == 17 &&
                       gold.Amount == 0,
                    "Gold pickup did not route to temporary run gold.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(goldPickup);
                UnityEngine.Object.DestroyImmediate(pickerProxy);
                UnityEngine.Object.DestroyImmediate(adapterObject);
                UnityEngine.Object.DestroyImmediate(player);
                UnityEngine.Object.DestroyImmediate(firstEnemy);
                UnityEngine.Object.DestroyImmediate(secondEnemy);
                UnityEngine.Object.DestroyImmediate(rejectedEnemy);
            }
        }

        private static GameObject CreateRewardTarget(
            string objectName,
            int experienceAmount,
            out RunProgressionValidationDamageable damageable)
        {
            GameObject target = new(objectName);
            EnemyRewardSource reward =
                target.AddComponent<EnemyRewardSource>();
            reward.ConfigureForEditor(experienceAmount);
            damageable =
                target.AddComponent<RunProgressionValidationDamageable>();
            return target;
        }

        private static DamageTargetResolution CreateKilledResolution(
            RunProgressionValidationDamageable target,
            CombatExecutionId executionId,
            CombatActorReference source)
        {
            DamageRequest request = new(
                executionId,
                source,
                rawDamage: 10f,
                DamageCause.BasicAttack);
            DeathContext death = new(request, appliedDamage: 10f);
            DamageResult result = DamageResult.Applied(
                request,
                healthBefore: 10f,
                healthAfter: 0f,
                appliedDamage: 10f,
                killed: true,
                death);
            return new DamageTargetResolution(target, result);
        }

        private static void ValidateProgressionDefinition()
        {
            RunProgressionDefinition definition =
                ScriptableObject.CreateInstance<RunProgressionDefinition>();
            try
            {
                definition.ConfigureForEditor(
                    configuredMaximumLevel: 4,
                    configuredBaseExperience: 100,
                    configuredExperienceIncrease: 50);
                Assert(definition.TryBuildCurve(
                           out RunExperienceCurve curve,
                           out string error) &&
                       string.IsNullOrEmpty(error) &&
                       curve.MaximumLevel == 4 &&
                       curve.TryGetRequirement(1, out int first) &&
                       first == 100 &&
                       curve.TryGetRequirement(3, out int third) &&
                       third == 200,
                    "Run progression definition did not build its configured curve.");

                definition.ConfigureForEditor(
                    configuredMaximumLevel: 1,
                    configuredBaseExperience: 100,
                    configuredExperienceIncrease: 50);
                Assert(!definition.TryBuildCurve(out _, out _),
                    "Run progression definition accepted an invalid maximum level.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        private static void ValidateRunProgression()
        {
            RunExperienceCurve curve = new(new[] { 10, 20, 40 });
            RunProgressionService service = new(curve, 2);
            int changeCount = 0;
            service.StateChanged += _ => changeCount++;

            RunProgressionResult registration =
                service.TryRegisterParticipant(
                    new RunParticipantIdentity(
                        "player:one",
                        "character:one"));
            Assert(registration.Success &&
                   registration.State.Level == 1 &&
                   registration.State.Experience == 0 &&
                   registration.State.Gold == 0,
                "Run participant registration failed.");

            RunProgressionResult experience =
                service.TryGrantExperience(" player:one ", 35);
            Assert(experience.Success &&
                   experience.LevelsGained == 2 &&
                   experience.ExperienceApplied == 35 &&
                   experience.State.Level == 3 &&
                   experience.State.Experience == 5,
                "Run experience did not cross levels deterministically.");
            Assert(service.TryGetExperienceRequirement(
                       "player:one",
                       out int nextRequirement) &&
                   nextRequirement == 40,
                "Run progression did not expose the next level requirement.");

            RunProgressionResult addGold =
                service.TryAddGold("player:one", 100);
            RunProgressionResult spendGold =
                service.TrySpendGold("player:one", 60);
            RunProgressionResult rejectedSpend =
                service.TrySpendGold("player:one", 50);
            Assert(addGold.Success && spendGold.Success &&
                   spendGold.State.Gold == 40 &&
                   !rejectedSpend.Success &&
                   rejectedSpend.Error ==
                       RunProgressionError.InsufficientGold &&
                   rejectedSpend.State.Gold == 40,
                "Run gold transaction was not atomic.");

            Assert(!service.TryRegisterParticipant(
                       new RunParticipantIdentity(
                           "player:one",
                           "character:two")).Success &&
                   !service.TryRegisterParticipant(
                       new RunParticipantIdentity(
                           "player:two",
                           "character:one")).Success,
                "Run progression accepted duplicate participant identities.");

            Assert(changeCount == 4,
                "Rejected progression commands emitted state changes.");
        }

        private static void ValidateAccountCrystals()
        {
            AccountCrystalWallet wallet = new();
            int changeCount = 0;
            wallet.AmountChanged += _ => changeCount++;

            CrystalWalletResult restore = wallet.TryRestore(25);
            CrystalWalletResult add = wallet.TryAdd(10);
            CrystalWalletResult spend = wallet.TrySpend(20);
            CrystalWalletResult rejected = wallet.TrySpend(16);

            Assert(restore.Success && add.Success && spend.Success &&
                   wallet.Amount == 15 &&
                   !rejected.Success &&
                   rejected.Error ==
                       CrystalWalletError.InsufficientCrystals &&
                   changeCount == 3,
                "Account crystal transactions are inconsistent.");
        }

        private static void ValidateConclusionRewards()
        {
            RunConclusionRewardCalculator calculator = new(
                new RunConclusionRewardConfiguration(
                    characterExperiencePerCompletedRound: 100,
                    crystalsPerCompletedRound: 5,
                    victoryCharacterExperienceBonus: 200,
                    victoryCrystalBonus: 10));

            RunConclusionRewardResult defeat = calculator.Calculate(
                new RunResultSummary(
                    "run:defeat",
                    RunOutcome.Defeat,
                    completedRoundCount: 2),
                difficultyMultiplierPercent: 125);
            Assert(defeat.Success &&
                   defeat.CharacterExperience == 250 &&
                   defeat.Crystals == 12,
                "Defeat rewards did not use completed rounds and deterministic rounding.");

            RunConclusionRewardResult victory = calculator.Calculate(
                new RunResultSummary(
                    "run:victory",
                    RunOutcome.Victory,
                    completedRoundCount: 4),
                difficultyMultiplierPercent: 150);
            Assert(victory.Success &&
                   victory.CharacterExperience == 900 &&
                   victory.Crystals == 45,
                "Victory rewards did not include the completion bonus.");

            RunConclusionRewardResult invalid = calculator.Calculate(
                null,
                difficultyMultiplierPercent: 100);
            Assert(!invalid.Success &&
                   invalid.Error ==
                       RunConclusionRewardError.InvalidRunResult,
                "Conclusion reward calculator accepted an invalid result.");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }

    public sealed class RunProgressionValidationDamageable :
        MonoBehaviour,
        global::IDamageable
    {
        public void TakeDamage(float damage)
        {
        }
    }
}
