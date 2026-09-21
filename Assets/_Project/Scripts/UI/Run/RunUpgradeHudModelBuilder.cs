using System;
using System.Collections.Generic;
using Titanhold.Run;

namespace Titanhold.UI.Run
{
    public sealed class RunUpgradeHudModelBuilder
    {
        private readonly IRunUpgradeDefinitionResolver definitions;

        public RunUpgradeHudModelBuilder(
            IRunUpgradeDefinitionResolver definitions)
        {
            this.definitions = definitions ??
                throw new ArgumentNullException(nameof(definitions));
        }

        public bool TryBuild(
            RunParticipantUpgradeState participant,
            out RunUpgradeHudModel model,
            out RunUpgradeHudPresentationError error)
        {
            model = null;
            error = RunUpgradeHudPresentationError.None;
            if (participant == null)
            {
                error = RunUpgradeHudPresentationError.MissingParticipant;
                return false;
            }

            List<RunUpgradeHudEntry> entries = new();
            HashSet<string> seen = new(StringComparer.Ordinal);
            for (int i = 0; i < participant.SelectionHistory.Count; i++)
            {
                string upgradeId =
                    participant.SelectionHistory[i]?.Trim() ?? string.Empty;
                if (upgradeId.Length == 0)
                {
                    error =
                        RunUpgradeHudPresentationError.InvalidSelectionHistory;
                    return false;
                }

                if (!seen.Add(upgradeId))
                    continue;

                int stackCount = participant.GetStackCount(upgradeId);
                if (stackCount <= 0)
                {
                    error =
                        RunUpgradeHudPresentationError.InvalidSelectionHistory;
                    return false;
                }

                if (!definitions.TryResolve(
                        upgradeId,
                        out IRunUpgradeDefinition definition))
                {
                    error = RunUpgradeHudPresentationError.DefinitionNotFound;
                    return false;
                }

                if (definition is not IRunUpgradePresentationDefinition
                    presentation)
                {
                    error =
                        RunUpgradeHudPresentationError.PresentationNotFound;
                    return false;
                }

                string displayName = presentation.DisplayName?.Trim() ??
                    string.Empty;
                entries.Add(new RunUpgradeHudEntry(
                    upgradeId,
                    displayName.Length > 0 ? displayName : upgradeId,
                    stackCount));
            }

            model = new RunUpgradeHudModel(participant.PlayerId, entries);
            return true;
        }
    }
}
