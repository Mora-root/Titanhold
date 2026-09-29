using System;
using System.Collections.Generic;
using Titanhold.Run;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Session.Editor
{
    public static class RunChapterCompletionValidationRunner
    {
        [MenuItem("Tools/Titanhold/Validate Run Chapter Completion")]
        public static void Validate()
        {
            GameObject player = null;
            try
            {
                player = CreatePlayerRuntime();
                GameSessionRuntime runtime = new(
                    new EmptyResolver(),
                    CreateLegacyRewardPolicy());
                GameSessionCommandResult begin =
                    runtime.GameSession.TryBeginRun(
                        new RunLaunchCommand(
                            "difficulty:prototype",
                            31,
                            new[]
                            {
                                new RunParticipantSelection(
                                    "player:local",
                                    "character:warrior")
                            }));
                Assert(begin.Success &&
                       runtime.GameSession.TryActivateRun(
                           begin.RunSessionId).Success,
                    "Could not start the chapter completion run.");

                RunChapterFlowService chapterFlow =
                    CreateRewardPhaseChapter();
                RunSceneParticipantBinding binding = new(
                    "player:local",
                    "character:warrior",
                    player.GetComponent<PlayerInventory>(),
                    player.GetComponent<PlayerEquipmentRuntime>(),
                    player.GetComponent<PlayerExperience>(),
                    player.GetComponent<PlayerGold>());
                RunChapterCompletionReward reward = new(
                    "reward:chapter:1:prototype",
                    1,
                    1200,
                    60);
                RunChapterCompletionApplicationService service = new();
                RunChapterCompletionCommand command = new(
                    "event:chapter-completion:validation",
                    begin.RunSessionId,
                    "player:local",
                    1);

                RunChapterCompletionResult result = service.TryConfirm(
                    runtime,
                    chapterFlow,
                    new[] { binding },
                    reward,
                    command);
                Assert(result.Success &&
                       chapterFlow.State.Phase ==
                           RunChapterPhase.Completed &&
                       runtime.GameSession.State.Phase ==
                           GameSessionPhase.TransitionToHub &&
                       runtime.AccountCrystals.Amount == 60 &&
                       runtime.TryGetCharacterSnapshot(
                           "character:warrior",
                           out CharacterSnapshot snapshot) &&
                       snapshot.Level == 6 &&
                       snapshot.Experience == 200 &&
                       result.Summary.CompletedRoundCount == 0 &&
                       result.Summary.CompletedChapterNumber == 1 &&
                       result.Summary.CharacterExperienceAwarded == 1200 &&
                       result.Summary.CrystalsAwarded == 60,
                    $"Chapter completion failed: {result.Error} " +
                    result.Detail);

                Assert(runtime.GameSession.TryCancelHubTransition(
                           begin.RunSessionId).Success,
                    "Could not prepare the retry check.");
                RunChapterCompletionResult retry =
                    service.TryPrepareHubTransition(
                        runtime,
                        chapterFlow,
                        reward,
                        begin.RunSessionId);
                Assert(retry.Success &&
                       retry.Replayed &&
                       runtime.GameSession.State.Phase ==
                           GameSessionPhase.TransitionToHub &&
                       runtime.AccountCrystals.Amount == 60 &&
                       runtime.TryGetCharacterSnapshot(
                           "character:warrior",
                           out CharacterSnapshot retrySnapshot) &&
                       retrySnapshot.Level == 6 &&
                       retrySnapshot.Experience == 200,
                    "Retry duplicated chapter completion rewards.");

                Debug.Log(
                    "Run Chapter Completion validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "Run Chapter Completion validation failed: " +
                    exception);
            }
            finally
            {
                if (player != null)
                    UnityEngine.Object.DestroyImmediate(player);
            }
        }

        private static RunChapterFlowService CreateRewardPhaseChapter()
        {
            RunChapterFlowService service = new(
                RunChapterFlowConfiguration.CreatePrototypeDefaults());
            Assert(service.TryAddProgress(100f, 1d).Success &&
                   service.TryEnterBossPortal(2d).Success &&
                   service.TryStartBoss().Success &&
                   service.TryDefeatBoss().Success,
                "Could not reach chapter Reward phase.");
            return service;
        }

        private static GameObject CreatePlayerRuntime()
        {
            GameObject player = new("RunChapterCompletion_Player");
            PlayerInventory inventory =
                player.AddComponent<PlayerInventory>();
            PlayerEquipmentRuntime equipment =
                player.AddComponent<PlayerEquipmentRuntime>();
            player.AddComponent<PlayerExperience>();
            player.AddComponent<PlayerGold>();
            player.AddComponent<PlayerInfo>();
            inventory.EnsureInitialized();
            equipment.SetPlayerInventory(inventory);
            return player;
        }

        private static RunConclusionRewardPolicy CreateLegacyRewardPolicy()
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
