using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Titanhold.UI.Run
{
    public sealed class RunUpgradeHudEntry
    {
        public RunUpgradeHudEntry(
            string upgradeId,
            string displayName,
            int stackCount)
        {
            UpgradeId = upgradeId?.Trim() ?? string.Empty;
            DisplayName = displayName?.Trim() ?? string.Empty;
            StackCount = stackCount;
        }

        public string UpgradeId { get; }
        public string DisplayName { get; }
        public int StackCount { get; }
    }

    public sealed class RunUpgradeHudModel
    {
        private readonly ReadOnlyCollection<RunUpgradeHudEntry> entries;

        public RunUpgradeHudModel(
            string playerId,
            IReadOnlyList<RunUpgradeHudEntry> entries)
        {
            PlayerId = playerId?.Trim() ?? string.Empty;

            int count = entries?.Count ?? 0;
            RunUpgradeHudEntry[] copy = new RunUpgradeHudEntry[count];
            for (int i = 0; i < copy.Length; i++)
                copy[i] = entries[i];

            this.entries = Array.AsReadOnly(copy);
        }

        public string PlayerId { get; }
        public IReadOnlyList<RunUpgradeHudEntry> Entries => entries;
    }

    public enum RunUpgradeHudPresentationError
    {
        None,
        MissingParticipant,
        InvalidSelectionHistory,
        DefinitionNotFound,
        PresentationNotFound
    }
}
