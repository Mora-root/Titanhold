namespace Titanhold.Session
{
    public enum CharacterSnapshotError
    {
        None,
        MissingCharacterId,
        SnapshotNotFound,
        MissingRuntimeSource,
        MissingDefinitionResolver,
        UnsupportedSchemaVersion,
        InvalidProgression,
        InvalidInventorySlot,
        DuplicateInventorySlot,
        InvalidEquipmentSlot,
        DuplicateEquipmentSlot,
        MissingItemDefinitionId,
        UnresolvedItemDefinition,
        ItemDefinitionMismatch,
        InvalidItemStack,
        InvalidItemInstance,
        InvalidGeneratedModifier,
        DuplicateItemInstanceId,
        InvalidEquipmentLoadout
    }

    public readonly struct CharacterSnapshotCaptureResult
    {
        private CharacterSnapshotCaptureResult(
            bool success,
            CharacterSnapshotError error,
            CharacterSnapshot snapshot,
            string detail)
        {
            Success = success;
            Error = error;
            Snapshot = snapshot;
            Detail = detail ?? string.Empty;
        }

        public bool Success { get; }
        public CharacterSnapshotError Error { get; }
        public CharacterSnapshot Snapshot { get; }
        public string Detail { get; }

        public static CharacterSnapshotCaptureResult Succeeded(
            CharacterSnapshot snapshot)
        {
            return new CharacterSnapshotCaptureResult(
                true,
                CharacterSnapshotError.None,
                snapshot,
                string.Empty);
        }

        public static CharacterSnapshotCaptureResult Failed(
            CharacterSnapshotError error,
            string detail = null)
        {
            return new CharacterSnapshotCaptureResult(
                false,
                error,
                null,
                detail);
        }
    }

    public readonly struct CharacterSnapshotRestoreResult
    {
        private CharacterSnapshotRestoreResult(
            bool success,
            CharacterSnapshotError error,
            string detail)
        {
            Success = success;
            Error = error;
            Detail = detail ?? string.Empty;
        }

        public bool Success { get; }
        public CharacterSnapshotError Error { get; }
        public string Detail { get; }

        public static CharacterSnapshotRestoreResult Succeeded()
        {
            return new CharacterSnapshotRestoreResult(
                true,
                CharacterSnapshotError.None,
                string.Empty);
        }

        public static CharacterSnapshotRestoreResult Failed(
            CharacterSnapshotError error,
            string detail = null)
        {
            return new CharacterSnapshotRestoreResult(false, error, detail);
        }
    }
}
