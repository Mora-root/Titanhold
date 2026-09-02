using System;
using System.Collections.Generic;

namespace Titanhold.Session
{
    public sealed class GameSessionService
    {
        public const int DefaultMaximumParticipantCount = 8;

        private readonly int maximumParticipantCount;

        public GameSessionService(
            int maximumParticipantCount = DefaultMaximumParticipantCount)
        {
            if (maximumParticipantCount < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumParticipantCount),
                    "Maximum participant count must be positive.");
            }

            this.maximumParticipantCount = maximumParticipantCount;
            State = new GameSessionState();
        }

        public GameSessionState State { get; }
        public int MaximumParticipantCount => maximumParticipantCount;

        public event Action<GameSessionState> StateChanged;

        public GameSessionCommandResult TryBeginRun(RunLaunchCommand command)
        {
            if (State.Phase != GameSessionPhase.Hub)
                return Fail(GameSessionError.InvalidPhase);

            GameSessionError validationError = ValidateLaunch(command);
            if (validationError != GameSessionError.None)
                return Fail(validationError);

            string runSessionId = Guid.NewGuid().ToString("N");
            GameSessionPhase previousPhase = State.Phase;
            State.BeginRunTransition(
                new RunSessionDescriptor(runSessionId, command));
            NotifyStateChanged();
            return Succeed(previousPhase);
        }

        public GameSessionCommandResult TryActivateRun(string runSessionId)
        {
            if (State.Phase != GameSessionPhase.TransitionToRun)
                return Fail(GameSessionError.InvalidPhase, runSessionId);

            GameSessionError idError = ValidateActiveRunId(runSessionId);
            if (idError != GameSessionError.None)
                return Fail(idError, runSessionId);

            GameSessionPhase previousPhase = State.Phase;
            State.ActivateRun();
            NotifyStateChanged();
            return Succeed(previousPhase);
        }

        public GameSessionCommandResult TryCancelRunTransition(
            string runSessionId)
        {
            if (State.Phase != GameSessionPhase.TransitionToRun)
                return Fail(GameSessionError.InvalidPhase, runSessionId);

            GameSessionError idError = ValidateActiveRunId(runSessionId);
            if (idError != GameSessionError.None)
                return Fail(idError, runSessionId);

            GameSessionPhase previousPhase = State.Phase;
            State.CancelRunTransition();
            NotifyStateChanged();
            return GameSessionCommandResult.Succeeded(
                previousPhase,
                State.Phase,
                runSessionId);
        }

        public GameSessionCommandResult TryConcludeRun(RunResultSummary result)
        {
            if (State.Phase != GameSessionPhase.Run)
            {
                return Fail(
                    GameSessionError.InvalidPhase,
                    result?.RunSessionId);
            }

            if (result == null || !result.IsValid)
                return Fail(GameSessionError.InvalidRunResult);

            GameSessionError idError = ValidateActiveRunId(result.RunSessionId);
            if (idError != GameSessionError.None)
                return Fail(idError, result.RunSessionId);

            GameSessionPhase previousPhase = State.Phase;
            State.BeginHubTransition(result);
            NotifyStateChanged();
            return Succeed(previousPhase);
        }

        public GameSessionCommandResult TryEnterHub(string runSessionId)
        {
            if (State.Phase != GameSessionPhase.TransitionToHub)
                return Fail(GameSessionError.InvalidPhase, runSessionId);

            GameSessionError idError = ValidateActiveRunId(runSessionId);
            if (idError != GameSessionError.None)
                return Fail(idError, runSessionId);

            GameSessionPhase previousPhase = State.Phase;
            State.EnterHub();
            NotifyStateChanged();
            return GameSessionCommandResult.Succeeded(
                previousPhase,
                State.Phase,
                runSessionId);
        }

        private GameSessionError ValidateLaunch(RunLaunchCommand command)
        {
            if (command == null || string.IsNullOrWhiteSpace(command.DifficultyId))
                return GameSessionError.InvalidDifficulty;

            if (command.Participants == null || command.Participants.Count == 0)
                return GameSessionError.MissingParticipants;

            if (command.Participants.Count > maximumParticipantCount)
                return GameSessionError.ParticipantLimitExceeded;

            HashSet<string> playerIds = new(StringComparer.Ordinal);
            HashSet<string> characterIds = new(StringComparer.Ordinal);
            for (int i = 0; i < command.Participants.Count; i++)
            {
                RunParticipantSelection participant = command.Participants[i];
                if (!participant.IsValid)
                    return GameSessionError.InvalidParticipant;

                if (!playerIds.Add(participant.PlayerId))
                    return GameSessionError.DuplicatePlayer;

                if (!characterIds.Add(participant.CharacterId))
                    return GameSessionError.DuplicateCharacter;
            }

            return GameSessionError.None;
        }

        private GameSessionError ValidateActiveRunId(string runSessionId)
        {
            if (string.IsNullOrWhiteSpace(runSessionId))
                return GameSessionError.MissingRunSessionId;

            if (State.ActiveRun == null ||
                !string.Equals(
                    State.ActiveRun.RunSessionId,
                    runSessionId,
                    StringComparison.Ordinal))
            {
                return GameSessionError.RunSessionMismatch;
            }

            return GameSessionError.None;
        }

        private GameSessionCommandResult Succeed(GameSessionPhase previousPhase)
        {
            return GameSessionCommandResult.Succeeded(
                previousPhase,
                State.Phase,
                State.ActiveRun?.RunSessionId);
        }

        private GameSessionCommandResult Fail(
            GameSessionError error,
            string runSessionId = null)
        {
            return GameSessionCommandResult.Failed(
                error,
                State.Phase,
                runSessionId ?? State.ActiveRun?.RunSessionId);
        }

        private void NotifyStateChanged()
        {
            StateChanged?.Invoke(State);
        }
    }
}
