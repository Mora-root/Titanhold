using System;
using System.Collections.Generic;
using Titanhold.Progression;
using Titanhold.Run;

namespace Titanhold.Session
{
    public sealed class GameSessionRuntime
    {
        private readonly Dictionary<string, CharacterSnapshot>
            characterSnapshots = new(StringComparer.Ordinal);
        private readonly RunExperienceCurve runExperienceCurve;
        private RunResultSummary settledRunResult;
        private readonly int maximumParticipantCount;
        private string activeProgressionRunSessionId = string.Empty;

        public GameSessionRuntime(
            IItemDefinitionResolver itemDefinitions,
            RunConclusionRewardPolicy conclusionRewards,
            int maximumParticipantCount =
                GameSessionService.DefaultMaximumParticipantCount,
            RunExperienceCurve runExperienceCurve = null)
        {
            ItemDefinitions = itemDefinitions ??
                throw new ArgumentNullException(nameof(itemDefinitions));
            ConclusionRewards = conclusionRewards ??
                throw new ArgumentNullException(nameof(conclusionRewards));
            this.maximumParticipantCount = maximumParticipantCount;
            this.runExperienceCurve = runExperienceCurve ??
                new RunExperienceCurve(Array.Empty<int>());
            GameSession = new GameSessionService(maximumParticipantCount);
            CharacterSnapshots = new CharacterSnapshotService();
            AccountCrystals = new AccountCrystalWallet();
            GameSession.StateChanged += HandleGameSessionStateChanged;
        }

        public GameSessionService GameSession { get; }
        public CharacterSnapshotService CharacterSnapshots { get; }
        public AccountCrystalWallet AccountCrystals { get; }
        public RunConclusionRewardPolicy ConclusionRewards { get; }
        public IItemDefinitionResolver ItemDefinitions { get; }
        public int StoredCharacterCount => characterSnapshots.Count;
        public RunProgressionService ActiveRunProgression { get; private set; }

        public event Action<string, CharacterSnapshot> CharacterSnapshotChanged;
        public event Action<string, RunProgressionService>
            ActiveRunProgressionChanged;

        public bool TryGetActiveRunProgression(
            string runSessionId,
            out RunProgressionService progression)
        {
            string normalizedId = runSessionId?.Trim() ?? string.Empty;
            if (normalizedId.Length == 0 ||
                ActiveRunProgression == null ||
                !string.Equals(
                    normalizedId,
                    activeProgressionRunSessionId,
                    StringComparison.Ordinal))
            {
                progression = null;
                return false;
            }

            progression = ActiveRunProgression;
            return true;
        }

        public bool TryGetCharacterSnapshot(
            string characterId,
            out CharacterSnapshot snapshot)
        {
            string normalizedId = characterId?.Trim() ?? string.Empty;
            if (normalizedId.Length == 0)
            {
                snapshot = null;
                return false;
            }

            return characterSnapshots.TryGetValue(normalizedId, out snapshot);
        }

        public CharacterSnapshotCaptureResult TryCaptureCharacter(
            string characterId,
            PlayerInventory inventory,
            PlayerEquipmentRuntime equipment,
            PlayerExperience experience,
            PlayerGold gold)
        {
            CharacterSnapshotCaptureResult result = CharacterSnapshots.TryCapture(
                characterId,
                inventory,
                equipment,
                experience,
                gold);
            if (!result.Success)
                return result;

            string normalizedId = result.Snapshot.CharacterId;
            characterSnapshots[normalizedId] = result.Snapshot;
            CharacterSnapshotChanged?.Invoke(normalizedId, result.Snapshot);
            return result;
        }

        public CharacterSnapshotRestoreResult TryRestoreCharacter(
            string characterId,
            PlayerInventory inventory,
            PlayerEquipmentRuntime equipment,
            PlayerExperience experience,
            PlayerGold gold)
        {
            string normalizedId = characterId?.Trim() ?? string.Empty;
            if (normalizedId.Length == 0)
            {
                return CharacterSnapshotRestoreResult.Failed(
                    CharacterSnapshotError.MissingCharacterId);
            }

            if (!characterSnapshots.TryGetValue(
                    normalizedId,
                    out CharacterSnapshot snapshot))
            {
                return CharacterSnapshotRestoreResult.Failed(
                    CharacterSnapshotError.SnapshotNotFound,
                    $"No snapshot is stored for character '{normalizedId}'.");
            }

            return CharacterSnapshots.TryRestore(
                snapshot,
                ItemDefinitions,
                inventory,
                equipment,
                experience,
                gold);
        }

        internal bool TryStoreCharacterSnapshots(
            IReadOnlyList<CharacterSnapshot> snapshots,
            out string error)
        {
            error = string.Empty;
            if (snapshots == null || snapshots.Count == 0)
            {
                error = "No character snapshots were supplied.";
                return false;
            }

            HashSet<string> characterIds = new(StringComparer.Ordinal);
            for (int i = 0; i < snapshots.Count; i++)
            {
                CharacterSnapshot snapshot = snapshots[i];
                if (snapshot == null ||
                    string.IsNullOrWhiteSpace(snapshot.CharacterId) ||
                    snapshot.SchemaVersion != CharacterSnapshot.CurrentSchemaVersion)
                {
                    error = $"Character snapshot {i} is invalid.";
                    return false;
                }

                if (!characterIds.Add(snapshot.CharacterId))
                {
                    error =
                        $"Character '{snapshot.CharacterId}' occurs more than once.";
                    return false;
                }
            }

            for (int i = 0; i < snapshots.Count; i++)
            {
                CharacterSnapshot snapshot = snapshots[i];
                characterSnapshots[snapshot.CharacterId] = snapshot;
            }

            for (int i = 0; i < snapshots.Count; i++)
            {
                CharacterSnapshot snapshot = snapshots[i];
                CharacterSnapshotChanged?.Invoke(
                    snapshot.CharacterId,
                    snapshot);
            }

            return true;
        }

        internal bool TryGetSettledRunResult(
            string runSessionId,
            out RunResultSummary result)
        {
            string normalizedId = runSessionId?.Trim() ?? string.Empty;
            result = settledRunResult;
            return result != null &&
                   string.Equals(
                       result.RunSessionId,
                       normalizedId,
                       StringComparison.Ordinal);
        }

        internal bool TryRecordSettledRunResult(RunResultSummary result)
        {
            if (result == null || !result.IsValid ||
                GameSession.State.ActiveRun == null ||
                !string.Equals(
                    result.RunSessionId,
                    GameSession.State.ActiveRun.RunSessionId,
                    StringComparison.Ordinal))
            {
                return false;
            }

            if (settledRunResult != null)
            {
                return string.Equals(
                           settledRunResult.RunSessionId,
                           result.RunSessionId,
                           StringComparison.Ordinal) &&
                       settledRunResult.Outcome == result.Outcome &&
                       settledRunResult.CompletedRoundCount ==
                           result.CompletedRoundCount &&
                       settledRunResult.CharacterExperienceAwarded ==
                           result.CharacterExperienceAwarded &&
                       settledRunResult.CrystalsAwarded ==
                           result.CrystalsAwarded;
            }

            settledRunResult = result;
            return true;
        }

        private void HandleGameSessionStateChanged(GameSessionState state)
        {
            if (state == null)
                return;

            if (state.Phase == GameSessionPhase.TransitionToRun)
            {
                settledRunResult = null;
                CreateRunProgression(state.ActiveRun);
                return;
            }

            if (state.Phase == GameSessionPhase.Hub)
                ClearRunProgression();
        }

        private void CreateRunProgression(RunSessionDescriptor descriptor)
        {
            if (descriptor == null ||
                string.IsNullOrWhiteSpace(descriptor.RunSessionId))
            {
                ClearRunProgression();
                return;
            }

            RunProgressionService progression = new(
                runExperienceCurve,
                maximumParticipantCount);
            for (int i = 0; i < descriptor.Participants.Count; i++)
            {
                RunParticipantSelection participant =
                    descriptor.Participants[i];
                RunProgressionResult registration =
                    progression.TryRegisterParticipant(
                        new RunParticipantIdentity(
                            participant.PlayerId,
                            participant.CharacterId));
                if (!registration.Success)
                {
                    throw new InvalidOperationException(
                        "Validated run participant could not be registered " +
                        $"for progression: {registration.Error}.");
                }
            }

            activeProgressionRunSessionId = descriptor.RunSessionId;
            ActiveRunProgression = progression;
            ActiveRunProgressionChanged?.Invoke(
                activeProgressionRunSessionId,
                ActiveRunProgression);
        }

        private void ClearRunProgression()
        {
            if (ActiveRunProgression == null)
                return;

            string clearedRunSessionId = activeProgressionRunSessionId;
            activeProgressionRunSessionId = string.Empty;
            ActiveRunProgression = null;
            ActiveRunProgressionChanged?.Invoke(
                clearedRunSessionId,
                null);
        }
    }
}
