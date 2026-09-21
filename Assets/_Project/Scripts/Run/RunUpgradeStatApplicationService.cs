using System;
using System.Collections.Generic;

namespace Titanhold.Run
{
    public interface IRunUpgradeStatModifierGateway
    {
        bool TryReplace(
            IReadOnlyList<StatModifierAssignment> modifiers);
    }

    public enum RunUpgradeStatApplicationError
    {
        None,
        ServiceDisposed,
        InvalidPlayerId,
        ParticipantNotFound,
        InvalidGateway,
        ParticipantAlreadyBound,
        ParticipantNotBound,
        DefinitionNotFound,
        DefinitionInvalid,
        GatewayRejected
    }

    public readonly struct RunUpgradeStatApplicationResult
    {
        private RunUpgradeStatApplicationResult(
            bool success,
            int appliedModifierCount,
            RunUpgradeStatApplicationError error)
        {
            Success = success;
            AppliedModifierCount = appliedModifierCount;
            Error = error;
        }

        public bool Success { get; }
        public int AppliedModifierCount { get; }
        public RunUpgradeStatApplicationError Error { get; }

        internal static RunUpgradeStatApplicationResult Succeeded(
            int appliedModifierCount)
        {
            return new RunUpgradeStatApplicationResult(
                true,
                appliedModifierCount,
                RunUpgradeStatApplicationError.None);
        }

        internal static RunUpgradeStatApplicationResult Failed(
            RunUpgradeStatApplicationError error)
        {
            return new RunUpgradeStatApplicationResult(false, 0, error);
        }
    }

    public sealed class CharacterStatsRunUpgradeModifierGateway :
        IRunUpgradeStatModifierGateway
    {
        private readonly CharacterStats stats;

        public CharacterStatsRunUpgradeModifierGateway(CharacterStats stats)
        {
            this.stats = stats ??
                throw new ArgumentNullException(nameof(stats));
        }

        public bool TryReplace(
            IReadOnlyList<StatModifierAssignment> modifiers)
        {
            return stats.TryReplaceModifiersFromSourceKind(
                StatModifierSourceKind.RunUpgrade,
                modifiers);
        }
    }

    public sealed class RunUpgradeStatApplicationService : IDisposable
    {
        private static readonly StatModifierAssignment[] EmptyModifiers =
            Array.Empty<StatModifierAssignment>();

        private readonly RunUpgradeChoiceService choices;
        private readonly IRunUpgradeDefinitionResolver definitions;
        private readonly Dictionary<string, IRunUpgradeStatModifierGateway>
            gateways = new(StringComparer.Ordinal);
        private bool disposed;

        public RunUpgradeStatApplicationService(
            RunUpgradeChoiceService choices,
            IRunUpgradeDefinitionResolver definitions)
        {
            this.choices = choices ??
                throw new ArgumentNullException(nameof(choices));
            this.definitions = definitions ??
                throw new ArgumentNullException(nameof(definitions));
            choices.ChoiceResolved += HandleChoiceResolved;
        }

        public int BoundParticipantCount => gateways.Count;

        public event Action<string, RunUpgradeStatApplicationResult>
            ApplicationFailed;

        public RunUpgradeStatApplicationResult TryBindParticipant(
            string playerId,
            IRunUpgradeStatModifierGateway gateway)
        {
            if (disposed)
            {
                return RunUpgradeStatApplicationResult.Failed(
                    RunUpgradeStatApplicationError.ServiceDisposed);
            }

            string normalizedPlayerId = playerId?.Trim() ?? string.Empty;
            if (normalizedPlayerId.Length == 0)
            {
                return RunUpgradeStatApplicationResult.Failed(
                    RunUpgradeStatApplicationError.InvalidPlayerId);
            }

            if (gateway == null)
            {
                return RunUpgradeStatApplicationResult.Failed(
                    RunUpgradeStatApplicationError.InvalidGateway);
            }

            if (!choices.TryGetParticipant(
                    normalizedPlayerId,
                    out RunParticipantUpgradeState participant))
            {
                return RunUpgradeStatApplicationResult.Failed(
                    RunUpgradeStatApplicationError.ParticipantNotFound);
            }

            if (gateways.TryGetValue(
                    normalizedPlayerId,
                    out IRunUpgradeStatModifierGateway current))
            {
                return ReferenceEquals(current, gateway)
                    ? TrySynchronize(normalizedPlayerId)
                    : RunUpgradeStatApplicationResult.Failed(
                        RunUpgradeStatApplicationError
                            .ParticipantAlreadyBound);
            }

            RunUpgradeStatApplicationResult result = TryApply(
                normalizedPlayerId,
                participant,
                gateway);
            if (result.Success)
                gateways.Add(normalizedPlayerId, gateway);

            return result;
        }

        public RunUpgradeStatApplicationResult TrySynchronize(
            string playerId)
        {
            if (disposed)
            {
                return RunUpgradeStatApplicationResult.Failed(
                    RunUpgradeStatApplicationError.ServiceDisposed);
            }

            string normalizedPlayerId = playerId?.Trim() ?? string.Empty;
            if (normalizedPlayerId.Length == 0)
            {
                return RunUpgradeStatApplicationResult.Failed(
                    RunUpgradeStatApplicationError.InvalidPlayerId);
            }

            if (!gateways.TryGetValue(
                    normalizedPlayerId,
                    out IRunUpgradeStatModifierGateway gateway))
            {
                return RunUpgradeStatApplicationResult.Failed(
                    RunUpgradeStatApplicationError.ParticipantNotBound);
            }

            if (!choices.TryGetParticipant(
                    normalizedPlayerId,
                    out RunParticipantUpgradeState participant))
            {
                return RunUpgradeStatApplicationResult.Failed(
                    RunUpgradeStatApplicationError.ParticipantNotFound);
            }

            return TryApply(normalizedPlayerId, participant, gateway);
        }

        public RunUpgradeStatApplicationResult TryUnbindParticipant(
            string playerId)
        {
            if (disposed)
            {
                return RunUpgradeStatApplicationResult.Failed(
                    RunUpgradeStatApplicationError.ServiceDisposed);
            }

            string normalizedPlayerId = playerId?.Trim() ?? string.Empty;
            if (normalizedPlayerId.Length == 0)
            {
                return RunUpgradeStatApplicationResult.Failed(
                    RunUpgradeStatApplicationError.InvalidPlayerId);
            }

            if (!gateways.TryGetValue(
                    normalizedPlayerId,
                    out IRunUpgradeStatModifierGateway gateway))
            {
                return RunUpgradeStatApplicationResult.Failed(
                    RunUpgradeStatApplicationError.ParticipantNotBound);
            }

            if (!gateway.TryReplace(EmptyModifiers))
            {
                return RunUpgradeStatApplicationResult.Failed(
                    RunUpgradeStatApplicationError.GatewayRejected);
            }

            gateways.Remove(normalizedPlayerId);
            return RunUpgradeStatApplicationResult.Succeeded(0);
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            choices.ChoiceResolved -= HandleChoiceResolved;
            foreach (IRunUpgradeStatModifierGateway gateway in
                     gateways.Values)
            {
                gateway.TryReplace(EmptyModifiers);
            }

            gateways.Clear();
        }

        private RunUpgradeStatApplicationResult TryApply(
            string playerId,
            RunParticipantUpgradeState participant,
            IRunUpgradeStatModifierGateway gateway)
        {
            RunUpgradeStatApplicationResult build = TryBuildAssignments(
                playerId,
                participant,
                out StatModifierAssignment[] assignments);
            if (!build.Success)
                return build;

            return gateway.TryReplace(assignments)
                ? RunUpgradeStatApplicationResult.Succeeded(
                    assignments.Length)
                : RunUpgradeStatApplicationResult.Failed(
                    RunUpgradeStatApplicationError.GatewayRejected);
        }

        private RunUpgradeStatApplicationResult TryBuildAssignments(
            string playerId,
            RunParticipantUpgradeState participant,
            out StatModifierAssignment[] assignments)
        {
            List<StatModifierAssignment> result = new();
            HashSet<string> visitedUpgradeIds =
                new(StringComparer.Ordinal);
            IReadOnlyList<string> history = participant.SelectionHistory;
            for (int historyIndex = 0;
                 historyIndex < history.Count;
                 historyIndex++)
            {
                string upgradeId = history[historyIndex];
                if (!visitedUpgradeIds.Add(upgradeId))
                    continue;

                if (!definitions.TryResolve(
                        upgradeId,
                        out IRunUpgradeDefinition definition))
                {
                    assignments = null;
                    return RunUpgradeStatApplicationResult.Failed(
                        RunUpgradeStatApplicationError.DefinitionNotFound);
                }

                if (!definition.TryValidate(out _))
                {
                    assignments = null;
                    return RunUpgradeStatApplicationResult.Failed(
                        RunUpgradeStatApplicationError.DefinitionInvalid);
                }

                if (definition is not IRunStatUpgradeDefinition statUpgrade)
                    continue;

                int stackCount = participant.GetStackCount(upgradeId);
                if (stackCount <= 0)
                {
                    assignments = null;
                    return RunUpgradeStatApplicationResult.Failed(
                        RunUpgradeStatApplicationError.DefinitionInvalid);
                }

                for (int stackIndex = 0;
                     stackIndex < stackCount;
                     stackIndex++)
                {
                    for (int modifierIndex = 0;
                         modifierIndex < statUpgrade.Modifiers.Count;
                         modifierIndex++)
                    {
                        StatModifierSource source =
                            StatModifierSource.ForRunUpgrade(
                                $"{playerId}|{upgradeId}|{stackIndex}|{modifierIndex}");
                        result.Add(new StatModifierAssignment(
                            source,
                            statUpgrade.Modifiers[modifierIndex]
                                .ToRuntimeModifier()));
                    }
                }
            }

            assignments = result.ToArray();
            return RunUpgradeStatApplicationResult.Succeeded(
                assignments.Length);
        }

        private void HandleChoiceResolved(
            RunUpgradeChoiceState choice,
            string selectedUpgradeId,
            RunParticipantUpgradeState participant)
        {
            if (disposed || choice == null ||
                !gateways.ContainsKey(choice.PlayerId))
            {
                return;
            }

            RunUpgradeStatApplicationResult result =
                TrySynchronize(choice.PlayerId);
            if (!result.Success)
                ApplicationFailed?.Invoke(choice.PlayerId, result);
        }
    }
}
