using System;
using System.Collections.Generic;
using Titanhold.Run;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Session.Editor
{
    public static class RunSessionConclusionValidationRunner
    {
        [MenuItem("Tools/Titanhold/Validate Run Session Conclusion")]
        public static void Validate()
        {
            GameObject player = null;
            try
            {
                player = CreatePlayerRuntime();
                GameSessionRuntime runtime = new(
                    new EmptyResolver(),
                    CreateRewardPolicy());
                GameSessionCommandResult begin =
                    runtime.GameSession.TryBeginRun(
                        new RunLaunchCommand(
                            "difficulty:prototype",
                            17,
                            new[]
                            {
                                new RunParticipantSelection(
                                    "player:local",
                                    "character:warrior")
                            }));
                Assert(begin.Success, "Could not begin validation run.");
                Assert(runtime.GameSession.TryActivateRun(
                        begin.RunSessionId).Success,
                    "Could not activate validation run.");

                PlayerInventory inventory =
                    player.GetComponent<PlayerInventory>();
                RunSceneParticipantBinding binding = new(
                    "player:local",
                    "character:warrior",
                    inventory,
                    player.GetComponent<PlayerEquipmentRuntime>(),
                    player.GetComponent<PlayerExperience>(),
                    player.GetComponent<PlayerGold>());
                RunSessionConclusionApplicationService service = new();
                RunFlowService runFlow = CreateCompletedBossRun();

                RunSessionConclusionResult result = service.TryConclude(
                    runtime,
                    runFlow.State,
                    new[] { binding });
                Assert(result.Success &&
                       runtime.StoredCharacterCount == 1 &&
                       runtime.GameSession.State.Phase ==
                           GameSessionPhase.TransitionToHub &&
                       runtime.GameSession.State.LastRunResult != null &&
                       runtime.GameSession.State.LastRunResult.Outcome ==
                           RunOutcome.Victory &&
                       runtime.GameSession.State.LastRunResult.CompletedRoundCount == 4 &&
                       runtime.GameSession.State.LastRunResult.CharacterExperienceAwarded ==
                           600 &&
                       runtime.GameSession.State.LastRunResult.CrystalsAwarded == 30 &&
                       runtime.AccountCrystals.Amount == 30 &&
                       runtime.TryGetCharacterSnapshot(
                           "character:warrior",
                           out CharacterSnapshot rewardedSnapshot) &&
                       rewardedSnapshot.Level == 4 &&
                       rewardedSnapshot.Experience == 150,
                    $"Victory conclusion failed: {result.Error} {result.Detail}");

                Assert(runtime.GameSession.TryCancelHubTransition(
                           result.RunSessionId).Success,
                    "Could not prepare conclusion retry validation.");
                RunSessionConclusionResult retry = service.TryConclude(
                    runtime,
                    runFlow.State,
                    new[] { binding });
                Assert(retry.Success &&
                       runtime.AccountCrystals.Amount == 30 &&
                       runtime.TryGetCharacterSnapshot(
                           "character:warrior",
                           out CharacterSnapshot retrySnapshot) &&
                       retrySnapshot.Level == 4 &&
                       retrySnapshot.Experience == 150,
                    "Retrying a settled conclusion awarded rewards twice.");

                GameSessionRuntime rejectedRuntime = new(
                    new EmptyResolver(),
                    CreateRewardPolicy());
                GameSessionCommandResult rejectedBegin =
                    rejectedRuntime.GameSession.TryBeginRun(
                        new RunLaunchCommand(
                            "difficulty:prototype",
                            18,
                            new[]
                            {
                                new RunParticipantSelection(
                                    "player:local",
                                    "character:warrior")
                            }));
                rejectedRuntime.GameSession.TryActivateRun(
                    rejectedBegin.RunSessionId);
                RunSessionConclusionResult rejected =
                    service.TryConclude(
                        rejectedRuntime,
                        runFlow.State,
                        new RunSceneParticipantBinding[] { null });
                Assert(!rejected.Success &&
                       rejected.Error ==
                           RunSessionConclusionError.InvalidParticipantBinding &&
                       rejectedRuntime.StoredCharacterCount == 0 &&
                       rejectedRuntime.GameSession.State.Phase ==
                           GameSessionPhase.Run,
                    "Rejected conclusion partially changed session state.");

                ValidateDefeatConclusion(service, player);
                ValidateAbandonedConclusion(service, player);
                ValidateMultiplayerRewardDistribution(service);

                Debug.Log("Run Session conclusion validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Run Session conclusion validation failed: {exception}");
            }
            finally
            {
                if (player != null)
                    UnityEngine.Object.DestroyImmediate(player);
            }
        }

        private static RunFlowService CreateCompletedBossRun()
        {
            RunFlowService runFlow = new(
                new RunFlowConfiguration(
                    100f,
                    10,
                    0.20f,
                    0.10f,
                    0.10f,
                    0.05f,
                    regularRoundCount: 3,
                    startingRound: 4));
            Assert(runFlow.TryRegisterExplorationKill(
                    new ExplorationKillContribution(100f, 0)).Success,
                "Could not fill the validation run meter.");
            Assert(runFlow.TryBeginAssaultTransition().Success &&
                   runFlow.TryStartAssault().Success &&
                   runFlow.TryCompleteAssault().Success &&
                   runFlow.TryCompleteRun().Success,
                "Could not complete the validation boss encounter.");
            return runFlow;
        }

        private static void ValidateDefeatConclusion(
            RunSessionConclusionApplicationService service,
            GameObject player)
        {
            GameSessionRuntime runtime = new(
                new EmptyResolver(),
                CreateRewardPolicy());
            GameSessionCommandResult begin =
                runtime.GameSession.TryBeginRun(
                    new RunLaunchCommand(
                        "difficulty:prototype",
                        19,
                        new[]
                        {
                            new RunParticipantSelection(
                                "player:local",
                                "character:warrior")
                        }));
            runtime.GameSession.TryActivateRun(begin.RunSessionId);

            RunFlowService runFlow = new(
                new RunFlowConfiguration(
                    100f,
                    10,
                    0.20f,
                    0.10f,
                    0.10f,
                    0.05f,
                    regularRoundCount: 3,
                    startingRound: 3));
            Assert(runFlow.TryFailRun().Success,
                "Could not fail the validation run.");

            RunSceneParticipantBinding binding = new(
                "player:local",
                "character:warrior",
                player.GetComponent<PlayerInventory>(),
                player.GetComponent<PlayerEquipmentRuntime>(),
                player.GetComponent<PlayerExperience>(),
                player.GetComponent<PlayerGold>());
            RunSessionConclusionResult result = service.TryConclude(
                runtime,
                runFlow.State,
                new[] { binding });
            Assert(result.Success &&
                   runtime.GameSession.State.LastRunResult.Outcome ==
                       RunOutcome.Defeat &&
                   runtime.GameSession.State.LastRunResult.CompletedRoundCount == 2 &&
                   runtime.AccountCrystals.Amount == 10,
                $"Defeat conclusion failed: {result.Error} {result.Detail}");
        }

        private static void ValidateAbandonedConclusion(
            RunSessionConclusionApplicationService service,
            GameObject player)
        {
            GameSessionRuntime runtime = new(
                new EmptyResolver(),
                CreateRewardPolicy());
            GameSessionCommandResult begin =
                runtime.GameSession.TryBeginRun(
                    new RunLaunchCommand(
                        "difficulty:prototype",
                        20,
                        new[]
                        {
                            new RunParticipantSelection(
                                "player:local",
                                "character:warrior")
                        }));
            runtime.GameSession.TryActivateRun(begin.RunSessionId);

            RunFlowService runFlow = new(
                new RunFlowConfiguration(
                    100f,
                    10,
                    0.20f,
                    0.10f,
                    0.10f,
                    0.05f,
                    regularRoundCount: 3,
                    startingRound: 3));
            Assert(runFlow.TryAbandonRun().Success,
                "Could not abandon the validation run.");

            RunSceneParticipantBinding binding = new(
                "player:local",
                "character:warrior",
                player.GetComponent<PlayerInventory>(),
                player.GetComponent<PlayerEquipmentRuntime>(),
                player.GetComponent<PlayerExperience>(),
                player.GetComponent<PlayerGold>());
            RunSessionConclusionResult result = service.TryConclude(
                runtime,
                runFlow.State,
                new[] { binding });
            Assert(result.Success &&
                   runtime.GameSession.State.LastRunResult.Outcome ==
                       RunOutcome.Abandoned &&
                   runtime.GameSession.State.LastRunResult.CompletedRoundCount == 2 &&
                   runtime.AccountCrystals.Amount == 10,
                $"Abandoned conclusion failed: {result.Error} {result.Detail}");
        }

        private static void ValidateMultiplayerRewardDistribution(
            RunSessionConclusionApplicationService service)
        {
            GameObject firstPlayer = null;
            GameObject secondPlayer = null;
            try
            {
                firstPlayer = CreatePlayerRuntime(
                    "RunSessionConclusion_FirstPlayer");
                secondPlayer = CreatePlayerRuntime(
                    "RunSessionConclusion_SecondPlayer");
                GameSessionRuntime runtime = new(
                    new EmptyResolver(),
                    CreateRewardPolicy());
                GameSessionCommandResult begin =
                    runtime.GameSession.TryBeginRun(
                        new RunLaunchCommand(
                            "difficulty:prototype",
                            21,
                            new[]
                            {
                                new RunParticipantSelection(
                                    "player:first",
                                    "character:first"),
                                new RunParticipantSelection(
                                    "player:second",
                                    "character:second")
                            }));
                Assert(begin.Success &&
                       runtime.GameSession.TryActivateRun(
                           begin.RunSessionId).Success,
                    "Could not start multiplayer reward validation.");

                RunFlowService runFlow = new(
                    new RunFlowConfiguration(
                        100f,
                        10,
                        0.20f,
                        0.10f,
                        0.10f,
                        0.05f,
                        regularRoundCount: 3,
                        startingRound: 2));
                Assert(runFlow.TryFailRun().Success,
                    "Could not fail multiplayer reward validation run.");

                RunSessionConclusionResult result = service.TryConclude(
                    runtime,
                    runFlow.State,
                    new[]
                    {
                        CreateBinding(
                            firstPlayer,
                            "player:first",
                            "character:first"),
                        CreateBinding(
                            secondPlayer,
                            "player:second",
                            "character:second")
                    });
                Assert(result.Success &&
                       runtime.StoredCharacterCount == 2 &&
                       runtime.AccountCrystals.Amount == 5 &&
                       HasProgressionSnapshot(
                           runtime,
                           "character:first",
                           expectedLevel: 2,
                           expectedExperience: 0) &&
                       HasProgressionSnapshot(
                           runtime,
                           "character:second",
                           expectedLevel: 2,
                           expectedExperience: 0),
                    "Character rewards were not distributed per participant " +
                    "while account crystals were awarded once.");
            }
            finally
            {
                if (firstPlayer != null)
                    UnityEngine.Object.DestroyImmediate(firstPlayer);

                if (secondPlayer != null)
                    UnityEngine.Object.DestroyImmediate(secondPlayer);
            }
        }

        private static bool HasProgressionSnapshot(
            GameSessionRuntime runtime,
            string characterId,
            int expectedLevel,
            int expectedExperience)
        {
            return runtime.TryGetCharacterSnapshot(
                       characterId,
                       out CharacterSnapshot snapshot) &&
                   snapshot.Level == expectedLevel &&
                   snapshot.Experience == expectedExperience;
        }

        private static RunSceneParticipantBinding CreateBinding(
            GameObject player,
            string playerId,
            string characterId)
        {
            return new RunSceneParticipantBinding(
                playerId,
                characterId,
                player.GetComponent<PlayerInventory>(),
                player.GetComponent<PlayerEquipmentRuntime>(),
                player.GetComponent<PlayerExperience>(),
                player.GetComponent<PlayerGold>());
        }

        private static GameObject CreatePlayerRuntime(
            string objectName = "RunSessionConclusion_Player")
        {
            GameObject player = new(objectName);
            PlayerInventory inventory = player.AddComponent<PlayerInventory>();
            PlayerEquipmentRuntime equipment =
                player.AddComponent<PlayerEquipmentRuntime>();
            player.AddComponent<PlayerExperience>();
            player.AddComponent<PlayerGold>();
            player.AddComponent<PlayerInfo>();
            inventory.EnsureInitialized();
            equipment.SetPlayerInventory(inventory);
            return player;
        }

        private static RunConclusionRewardPolicy CreateRewardPolicy()
        {
            return new RunConclusionRewardPolicy(
                new RunConclusionRewardConfiguration(100, 5, 200, 10),
                new Dictionary<string, int>
                {
                    { "difficulty:prototype", 100 }
                });
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private sealed class EmptyResolver : IItemDefinitionResolver
        {
            public bool TryResolve(
                string definitionId,
                out ItemDefinition definition)
            {
                definition = null;
                return false;
            }
        }
    }
}
