using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Titanhold.Session
{
    public readonly struct RunParticipantSelection
    {
        public RunParticipantSelection(string playerId, string characterId)
        {
            PlayerId = playerId?.Trim() ?? string.Empty;
            CharacterId = characterId?.Trim() ?? string.Empty;
        }

        public string PlayerId { get; }
        public string CharacterId { get; }
        public bool IsValid =>
            !string.IsNullOrWhiteSpace(PlayerId) &&
            !string.IsNullOrWhiteSpace(CharacterId);
    }

    public sealed class RunLaunchCommand
    {
        private readonly ReadOnlyCollection<RunParticipantSelection> participants;

        public RunLaunchCommand(
            string difficultyId,
            int seed,
            IReadOnlyList<RunParticipantSelection> participants)
        {
            DifficultyId = difficultyId?.Trim() ?? string.Empty;
            Seed = seed;

            int count = participants?.Count ?? 0;
            RunParticipantSelection[] copy =
                new RunParticipantSelection[count];
            for (int i = 0; i < count; i++)
                copy[i] = participants[i];

            this.participants = Array.AsReadOnly(copy);
        }

        public string DifficultyId { get; }
        public int Seed { get; }
        public IReadOnlyList<RunParticipantSelection> Participants => participants;
    }

    public sealed class RunSessionDescriptor
    {
        private readonly ReadOnlyCollection<RunParticipantSelection> participants;

        internal RunSessionDescriptor(
            string runSessionId,
            RunLaunchCommand command)
        {
            RunSessionId = runSessionId;
            DifficultyId = command.DifficultyId;
            Seed = command.Seed;

            RunParticipantSelection[] copy =
                new RunParticipantSelection[command.Participants.Count];
            for (int i = 0; i < copy.Length; i++)
                copy[i] = command.Participants[i];

            participants = Array.AsReadOnly(copy);
        }

        public string RunSessionId { get; }
        public string DifficultyId { get; }
        public int Seed { get; }
        public IReadOnlyList<RunParticipantSelection> Participants => participants;
    }

    public sealed class RunResultSummary
    {
        public RunResultSummary(
            string runSessionId,
            RunOutcome outcome,
            int completedRoundCount)
        {
            RunSessionId = runSessionId?.Trim() ?? string.Empty;
            Outcome = outcome;
            CompletedRoundCount = completedRoundCount;
        }

        public string RunSessionId { get; }
        public RunOutcome Outcome { get; }
        public int CompletedRoundCount { get; }
        public bool IsValid =>
            !string.IsNullOrWhiteSpace(RunSessionId) &&
            CompletedRoundCount >= 0 &&
            Enum.IsDefined(typeof(RunOutcome), Outcome);
    }

    public sealed class GameSessionState
    {
        internal GameSessionState()
        {
            Phase = GameSessionPhase.Hub;
        }

        public GameSessionPhase Phase { get; private set; }
        public RunSessionDescriptor ActiveRun { get; private set; }
        public RunSessionDescriptor LastRun { get; private set; }
        public RunResultSummary LastRunResult { get; private set; }

        internal void BeginRunTransition(RunSessionDescriptor descriptor)
        {
            ActiveRun = descriptor;
            Phase = GameSessionPhase.TransitionToRun;
        }

        internal void ActivateRun()
        {
            Phase = GameSessionPhase.Run;
        }

        internal void CancelRunTransition()
        {
            ActiveRun = null;
            Phase = GameSessionPhase.Hub;
        }

        internal void BeginHubTransition(RunResultSummary result)
        {
            LastRun = ActiveRun;
            LastRunResult = result;
            Phase = GameSessionPhase.TransitionToHub;
        }

        internal void CancelHubTransition()
        {
            Phase = GameSessionPhase.Run;
        }

        internal void EnterHub()
        {
            ActiveRun = null;
            Phase = GameSessionPhase.Hub;
        }
    }
}
