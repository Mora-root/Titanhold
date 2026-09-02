using System;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Session.Editor
{
    public static class GameSessionValidationRunner
    {
        [MenuItem("Tools/Titanhold/Validate Game Session Foundation")]
        public static void Validate()
        {
            try
            {
                ValidateSoloLifecycle();
                ValidateMultiplayerReadyLaunch();
                ValidateRejectedCommands();
                Debug.Log("Game Session foundation validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Game Session foundation validation failed: {exception}");
            }
        }

        private static void ValidateSoloLifecycle()
        {
            GameSessionService service = new();
            int stateChangeCount = 0;
            service.StateChanged += _ => stateChangeCount++;

            RunLaunchCommand launch = new(
                "difficulty:prototype",
                12345,
                new[]
                {
                    new RunParticipantSelection(
                        "player:local",
                        "character:warrior")
                });
            GameSessionCommandResult begin = service.TryBeginRun(launch);
            Assert(begin.Success &&
                   service.State.Phase == GameSessionPhase.TransitionToRun &&
                   service.State.ActiveRun != null &&
                   service.State.ActiveRun.RunSessionId == begin.RunSessionId,
                "Valid solo launch did not begin a run transition.");

            string runId = begin.RunSessionId;
            GameSessionCommandResult activate = service.TryActivateRun(runId);
            Assert(activate.Success && service.State.Phase == GameSessionPhase.Run,
                "Run scene activation was rejected.");

            RunResultSummary result = new(runId, RunOutcome.Victory, 4);
            GameSessionCommandResult conclude = service.TryConcludeRun(result);
            Assert(conclude.Success &&
                   service.State.Phase == GameSessionPhase.TransitionToHub &&
                   ReferenceEquals(service.State.LastRunResult, result),
                "Valid run result did not begin the Hub transition.");

            GameSessionCommandResult enterHub = service.TryEnterHub(runId);
            Assert(enterHub.Success &&
                   service.State.Phase == GameSessionPhase.Hub &&
                   service.State.ActiveRun == null &&
                   service.State.LastRun != null &&
                   service.State.LastRun.RunSessionId == runId &&
                   service.State.LastRun.DifficultyId == "difficulty:prototype" &&
                   ReferenceEquals(service.State.LastRunResult, result) &&
                   stateChangeCount == 4,
                "Hub entry did not close the active run cleanly.");
        }

        private static void ValidateMultiplayerReadyLaunch()
        {
            GameSessionService service = new(maximumParticipantCount: 8);
            RunParticipantSelection[] participants =
                new RunParticipantSelection[8];
            for (int i = 0; i < participants.Length; i++)
            {
                participants[i] = new RunParticipantSelection(
                    $"player:{i}",
                    $"character:{i}");
            }

            RunLaunchCommand launch = new("difficulty:prototype", 7, participants);
            participants[0] = default;
            GameSessionCommandResult result = service.TryBeginRun(launch);
            Assert(result.Success &&
                   service.State.ActiveRun.Participants.Count == 8 &&
                   service.State.ActiveRun.Participants[0].IsValid,
                "Launch command did not preserve an immutable eight-player roster.");
        }

        private static void ValidateRejectedCommands()
        {
            GameSessionService service = new(maximumParticipantCount: 2);
            GameSessionCommandResult missingParticipants = service.TryBeginRun(
                new RunLaunchCommand("difficulty:prototype", 1, Array.Empty<RunParticipantSelection>()));
            Assert(!missingParticipants.Success &&
                   missingParticipants.Error == GameSessionError.MissingParticipants,
                "Launch without participants was accepted.");

            GameSessionCommandResult duplicatePlayer = service.TryBeginRun(
                new RunLaunchCommand(
                    "difficulty:prototype",
                    1,
                    new[]
                    {
                        new RunParticipantSelection("player:one", "character:one"),
                        new RunParticipantSelection("player:one", "character:two")
                    }));
            Assert(!duplicatePlayer.Success &&
                   duplicatePlayer.Error == GameSessionError.DuplicatePlayer,
                "Launch with a duplicate player was accepted.");

            GameSessionCommandResult tooMany = service.TryBeginRun(
                new RunLaunchCommand(
                    "difficulty:prototype",
                    1,
                    new[]
                    {
                        new RunParticipantSelection("player:one", "character:one"),
                        new RunParticipantSelection("player:two", "character:two"),
                        new RunParticipantSelection("player:three", "character:three")
                    }));
            Assert(!tooMany.Success &&
                   tooMany.Error == GameSessionError.ParticipantLimitExceeded,
                "Launch above the participant limit was accepted.");

            GameSessionCommandResult begin = service.TryBeginRun(
                new RunLaunchCommand(
                    "difficulty:prototype",
                    1,
                    new[]
                    {
                        new RunParticipantSelection("player:one", "character:one")
                    }));
            Assert(begin.Success, "Rejected-command setup could not begin a run.");

            GameSessionCommandResult wrongActivation =
                service.TryActivateRun("run:wrong");
            Assert(!wrongActivation.Success &&
                   wrongActivation.Error == GameSessionError.RunSessionMismatch &&
                   service.State.Phase == GameSessionPhase.TransitionToRun,
                "Mismatched run activation changed session state.");

            GameSessionCommandResult cancel =
                service.TryCancelRunTransition(begin.RunSessionId);
            Assert(cancel.Success && service.State.Phase == GameSessionPhase.Hub,
                "Run transition could not be cancelled safely.");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
