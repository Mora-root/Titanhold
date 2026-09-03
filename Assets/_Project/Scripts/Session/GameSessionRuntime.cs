using System;
using System.Collections.Generic;

namespace Titanhold.Session
{
    public sealed class GameSessionRuntime
    {
        private readonly Dictionary<string, CharacterSnapshot>
            characterSnapshots = new(StringComparer.Ordinal);

        public GameSessionRuntime(
            IItemDefinitionResolver itemDefinitions,
            int maximumParticipantCount =
                GameSessionService.DefaultMaximumParticipantCount)
        {
            ItemDefinitions = itemDefinitions ??
                throw new ArgumentNullException(nameof(itemDefinitions));
            GameSession = new GameSessionService(maximumParticipantCount);
            CharacterSnapshots = new CharacterSnapshotService();
        }

        public GameSessionService GameSession { get; }
        public CharacterSnapshotService CharacterSnapshots { get; }
        public IItemDefinitionResolver ItemDefinitions { get; }
        public int StoredCharacterCount => characterSnapshots.Count;

        public event Action<string, CharacterSnapshot> CharacterSnapshotChanged;

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
    }
}
