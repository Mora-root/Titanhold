using System;
using System.Collections.Generic;
using Titanhold.Combat;

namespace Titanhold.Run
{
    public sealed class RunCombatResourceService
    {
        public const int DefaultMaximumParticipantCount = 8;

        private readonly int maximumParticipantCount;
        private readonly Dictionary<string, ParticipantResources> participants =
            new(StringComparer.Ordinal);
        private readonly HashSet<string> characterIds =
            new(StringComparer.Ordinal);

        public RunCombatResourceService(
            int maximumParticipantCount = DefaultMaximumParticipantCount)
        {
            if (maximumParticipantCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumParticipantCount));
            }

            this.maximumParticipantCount = maximumParticipantCount;
        }

        public int ParticipantCount => participants.Count;
        public int MaximumParticipantCount => maximumParticipantCount;

        public event Action<string, CombatResourceSnapshot> ResourceChanged;

        public RunCombatResourceResult TryRegisterParticipant(
            RunParticipantIdentity identity)
        {
            if (!identity.IsValid)
            {
                return RunCombatResourceResult.Failed(
                    RunCombatResourceError.InvalidParticipant);
            }

            if (participants.ContainsKey(identity.PlayerId))
            {
                return RunCombatResourceResult.Failed(
                    RunCombatResourceError.DuplicatePlayer,
                    identity.PlayerId);
            }

            if (characterIds.Contains(identity.CharacterId))
            {
                return RunCombatResourceResult.Failed(
                    RunCombatResourceError.DuplicateCharacter,
                    identity.PlayerId);
            }

            if (participants.Count >= maximumParticipantCount)
            {
                return RunCombatResourceResult.Failed(
                    RunCombatResourceError.ParticipantLimitExceeded,
                    identity.PlayerId);
            }

            participants.Add(
                identity.PlayerId,
                new ParticipantResources(identity));
            characterIds.Add(identity.CharacterId);
            return RunCombatResourceResult.Succeeded(identity.PlayerId);
        }

        public RunCombatResourceResult TryRegisterResource(
            string playerId,
            string resourceId,
            float maximum,
            float initial = 0f)
        {
            if (!TryGetParticipant(playerId, out ParticipantResources participant))
            {
                return RunCombatResourceResult.Failed(
                    RunCombatResourceError.ParticipantNotFound,
                    Normalize(playerId));
            }

            string normalizedResourceId = Normalize(resourceId);
            if (normalizedResourceId.Length == 0 ||
                !IsPositiveFinite(maximum) ||
                !IsNonNegativeFinite(initial) ||
                initial > maximum)
            {
                return RunCombatResourceResult.Failed(
                    RunCombatResourceError.InvalidResource,
                    participant.Identity.PlayerId);
            }

            if (participant.Resources.TryGetValue(
                    normalizedResourceId,
                    out CombatResourceState existing))
            {
                return RunCombatResourceResult.Failed(
                    RunCombatResourceError.DuplicateResource,
                    participant.Identity.PlayerId,
                    existing.Snapshot);
            }

            CombatResourceState resource = new(
                normalizedResourceId,
                maximum,
                initial);
            participant.Resources.Add(normalizedResourceId, resource);
            ResourceChanged?.Invoke(
                participant.Identity.PlayerId,
                resource.Snapshot);
            return RunCombatResourceResult.Succeeded(
                participant.Identity.PlayerId,
                resource.Snapshot,
                changed: true);
        }

        public bool TryGetResource(
            string playerId,
            string resourceId,
            out CombatResourceSnapshot resource)
        {
            resource = default;
            return TryGetResourceState(
                       playerId,
                       resourceId,
                       out _,
                       out CombatResourceState state) &&
                   (resource = state.Snapshot).IsValid;
        }

        public bool TryCreateParticipantGateway(
            string playerId,
            out ICombatResourceGateway gateway)
        {
            gateway = null;
            if (!TryGetParticipant(playerId, out ParticipantResources participant))
                return false;

            gateway = new ParticipantGateway(
                this,
                participant.Identity.PlayerId);
            return true;
        }

        public RunCombatResourceResult TryGain(
            string playerId,
            CombatExecutionId sourceExecutionId,
            string resourceId,
            float amount)
        {
            if (!TryGetResourceState(
                    playerId,
                    resourceId,
                    out string normalizedPlayerId,
                    out CombatResourceState resource))
            {
                return MissingResourceResult(playerId, resourceId);
            }

            if (!sourceExecutionId.IsValid)
            {
                return RunCombatResourceResult.Failed(
                    RunCombatResourceError.InvalidExecutionId,
                    normalizedPlayerId,
                    resource.Snapshot);
            }

            if (!IsPositiveFinite(amount))
            {
                return RunCombatResourceResult.Failed(
                    RunCombatResourceError.InvalidAmount,
                    normalizedPlayerId,
                    resource.Snapshot);
            }

            float previous = resource.Current;
            if (!resource.TryGain(
                    sourceExecutionId,
                    resource.ResourceId,
                    amount))
            {
                return RunCombatResourceResult.Failed(
                    RunCombatResourceError.InvalidAmount,
                    normalizedPlayerId,
                    resource.Snapshot);
            }

            return CompleteMutation(normalizedPlayerId, resource, previous);
        }

        public RunCombatResourceResult TrySpend(
            string playerId,
            string resourceId,
            float amount)
        {
            if (!TryGetResourceState(
                    playerId,
                    resourceId,
                    out string normalizedPlayerId,
                    out CombatResourceState resource))
            {
                return MissingResourceResult(playerId, resourceId);
            }

            if (!IsPositiveFinite(amount))
            {
                return RunCombatResourceResult.Failed(
                    RunCombatResourceError.InvalidAmount,
                    normalizedPlayerId,
                    resource.Snapshot);
            }

            float previous = resource.Current;
            if (!resource.TrySpend(amount))
            {
                return RunCombatResourceResult.Failed(
                    RunCombatResourceError.InsufficientResource,
                    normalizedPlayerId,
                    resource.Snapshot);
            }

            return CompleteMutation(normalizedPlayerId, resource, previous);
        }

        public RunCombatResourceResult TrySetCurrent(
            string playerId,
            string resourceId,
            float value)
        {
            if (!TryGetResourceState(
                    playerId,
                    resourceId,
                    out string normalizedPlayerId,
                    out CombatResourceState resource))
            {
                return MissingResourceResult(playerId, resourceId);
            }

            if (!IsNonNegativeFinite(value) || value > resource.Maximum)
            {
                return RunCombatResourceResult.Failed(
                    RunCombatResourceError.InvalidAmount,
                    normalizedPlayerId,
                    resource.Snapshot);
            }

            float previous = resource.Current;
            if (!resource.TrySetCurrent(value))
            {
                return RunCombatResourceResult.Failed(
                    RunCombatResourceError.InvalidAmount,
                    normalizedPlayerId,
                    resource.Snapshot);
            }

            return CompleteMutation(normalizedPlayerId, resource, previous);
        }

        private RunCombatResourceResult CompleteMutation(
            string playerId,
            CombatResourceState resource,
            float previous)
        {
            bool changed = resource.Current != previous;
            CombatResourceSnapshot snapshot = resource.Snapshot;
            if (changed)
                ResourceChanged?.Invoke(playerId, snapshot);

            return RunCombatResourceResult.Succeeded(
                playerId,
                snapshot,
                changed);
        }

        private RunCombatResourceResult MissingResourceResult(
            string playerId,
            string resourceId)
        {
            string normalizedPlayerId = Normalize(playerId);
            if (!participants.ContainsKey(normalizedPlayerId))
            {
                return RunCombatResourceResult.Failed(
                    RunCombatResourceError.ParticipantNotFound,
                    normalizedPlayerId);
            }

            return RunCombatResourceResult.Failed(
                Normalize(resourceId).Length == 0
                    ? RunCombatResourceError.InvalidResource
                    : RunCombatResourceError.ResourceNotFound,
                normalizedPlayerId);
        }

        private bool TryGetParticipant(
            string playerId,
            out ParticipantResources participant)
        {
            string normalizedPlayerId = Normalize(playerId);
            participant = null;
            return normalizedPlayerId.Length > 0 &&
                   participants.TryGetValue(
                       normalizedPlayerId,
                       out participant);
        }

        private bool TryGetResourceState(
            string playerId,
            string resourceId,
            out string normalizedPlayerId,
            out CombatResourceState resource)
        {
            normalizedPlayerId = Normalize(playerId);
            resource = null;
            string normalizedResourceId = Normalize(resourceId);
            return normalizedPlayerId.Length > 0 &&
                   normalizedResourceId.Length > 0 &&
                   participants.TryGetValue(
                       normalizedPlayerId,
                       out ParticipantResources participant) &&
                   participant.Resources.TryGetValue(
                       normalizedResourceId,
                       out resource);
        }

        private static string Normalize(string value)
        {
            return value?.Trim() ?? string.Empty;
        }

        private static bool IsPositiveFinite(float value)
        {
            return IsNonNegativeFinite(value) && value > 0f;
        }

        private static bool IsNonNegativeFinite(float value)
        {
            return value >= 0f &&
                   !float.IsNaN(value) &&
                   !float.IsInfinity(value);
        }

        private sealed class ParticipantResources
        {
            public ParticipantResources(RunParticipantIdentity identity)
            {
                Identity = identity;
            }

            public RunParticipantIdentity Identity { get; }
            public Dictionary<string, CombatResourceState> Resources { get; } =
                new(StringComparer.Ordinal);
        }

        private sealed class ParticipantGateway : ICombatResourceGateway
        {
            private readonly RunCombatResourceService resources;
            private readonly string playerId;

            public ParticipantGateway(
                RunCombatResourceService resources,
                string playerId)
            {
                this.resources = resources;
                this.playerId = playerId;
            }

            public bool TryGain(
                CombatExecutionId sourceExecutionId,
                string resourceId,
                float amount)
            {
                return resources.TryGain(
                    playerId,
                    sourceExecutionId,
                    resourceId,
                    amount).Success;
            }
        }
    }
}
