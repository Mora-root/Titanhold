using System;
using System.Collections.Generic;

namespace Titanhold.Run
{
    public sealed class RunStartReadinessService : IDisposable
    {
        public const int StartingAbilitySlotIndex = 0;

        private readonly RunAbilityLoadoutService loadout;
        private readonly Dictionary<string, RunParticipantStartReadinessState>
            participants = new(StringComparer.Ordinal);
        private bool subscribed;

        public RunStartReadinessService(
            RunAbilityLoadoutService loadout,
            IReadOnlyList<RunParticipantIdentity> participantRoster)
        {
            this.loadout = loadout ??
                throw new ArgumentNullException(nameof(loadout));
            if (participantRoster == null || participantRoster.Count == 0)
            {
                throw new ArgumentException(
                    "At least one run participant is required.",
                    nameof(participantRoster));
            }

            for (int i = 0; i < participantRoster.Count; i++)
            {
                RunParticipantIdentity identity = participantRoster[i];
                if (!identity.IsValid ||
                    !loadout.TryGetParticipant(
                        identity.PlayerId,
                        out RunParticipantAbilityState abilityState) ||
                    !string.Equals(
                        identity.CharacterId,
                        abilityState.CharacterId,
                        StringComparison.Ordinal) ||
                    !participants.TryAdd(
                        identity.PlayerId,
                        new RunParticipantStartReadinessState(identity)))
                {
                    throw new ArgumentException(
                        $"Run participant {i} is invalid, duplicated, or missing from the ability loadout.",
                        nameof(participantRoster));
                }
            }

            loadout.StateChanged += HandleLoadoutStateChanged;
            subscribed = true;
        }

        public int ParticipantCount => participants.Count;
        public int ConfirmedParticipantCount { get; private set; }
        public bool AllParticipantsConfirmed =>
            ConfirmedParticipantCount == ParticipantCount;
        public bool IsSealed { get; private set; }

        public event Action<RunParticipantStartReadinessState>
            ParticipantReadinessChanged;
        public event Action ReadinessSealed;

        public bool TryGetParticipant(
            string playerId,
            out RunParticipantStartReadinessState state)
        {
            state = null;
            string normalizedId = playerId?.Trim() ?? string.Empty;
            return normalizedId.Length > 0 &&
                   participants.TryGetValue(normalizedId, out state);
        }

        public RunStartReadinessResult TryConfirmStartingAbility(
            string playerId,
            string abilityId)
        {
            string normalizedPlayerId = playerId?.Trim() ?? string.Empty;
            string normalizedAbilityId = abilityId?.Trim() ?? string.Empty;
            if (!TryGetParticipant(
                    normalizedPlayerId,
                    out RunParticipantStartReadinessState state))
            {
                return RunStartReadinessResult.Failed(
                    RunStartReadinessError.ParticipantNotFound);
            }

            if (IsSealed)
            {
                return RunStartReadinessResult.Failed(
                    RunStartReadinessError.ReadinessAlreadySealed,
                    state);
            }

            if (normalizedAbilityId.Length == 0)
            {
                return RunStartReadinessResult.Failed(
                    RunStartReadinessError.InvalidAbilityId,
                    state);
            }

            if (!loadout.TryGetParticipant(
                    normalizedPlayerId,
                    out RunParticipantAbilityState abilityState) ||
                !abilityState.TryGetAbilitySlot(
                    StartingAbilitySlotIndex,
                    out string assignedAbilityId) ||
                !string.Equals(
                    assignedAbilityId,
                    normalizedAbilityId,
                    StringComparison.Ordinal))
            {
                return RunStartReadinessResult.Failed(
                    RunStartReadinessError.AbilityNotAssignedToStartSlot,
                    state);
            }

            if (state.IsConfirmed)
            {
                if (string.Equals(
                        state.StartingAbilityId,
                        normalizedAbilityId,
                        StringComparison.Ordinal))
                {
                    return RunStartReadinessResult.Succeeded(
                        state,
                        changed: false);
                }

                return RunStartReadinessResult.Failed(
                    RunStartReadinessError.StartingAbilityAlreadyConfirmed,
                    state);
            }

            state.Confirm(normalizedAbilityId);
            ConfirmedParticipantCount++;
            ParticipantReadinessChanged?.Invoke(state);
            return RunStartReadinessResult.Succeeded(state);
        }

        public RunStartReadinessResult TrySeal()
        {
            if (IsSealed)
                return RunStartReadinessResult.Succeeded(changed: false);

            if (!AllParticipantsConfirmed)
            {
                return RunStartReadinessResult.Failed(
                    RunStartReadinessError.NotAllParticipantsConfirmed);
            }

            IsSealed = true;
            Unsubscribe();
            ReadinessSealed?.Invoke();
            return RunStartReadinessResult.Succeeded();
        }

        public void Dispose()
        {
            Unsubscribe();
        }

        private void HandleLoadoutStateChanged(
            RunParticipantAbilityState abilityState)
        {
            if (IsSealed || abilityState == null ||
                !participants.TryGetValue(
                    abilityState.PlayerId,
                    out RunParticipantStartReadinessState readiness) ||
                !readiness.IsConfirmed)
            {
                return;
            }

            if (abilityState.TryGetAbilitySlot(
                    StartingAbilitySlotIndex,
                    out string assignedAbilityId) &&
                string.Equals(
                    assignedAbilityId,
                    readiness.StartingAbilityId,
                    StringComparison.Ordinal))
            {
                return;
            }

            readiness.ClearConfirmation();
            ConfirmedParticipantCount--;
            ParticipantReadinessChanged?.Invoke(readiness);
        }

        private void Unsubscribe()
        {
            if (!subscribed)
                return;

            loadout.StateChanged -= HandleLoadoutStateChanged;
            subscribed = false;
        }
    }
}
