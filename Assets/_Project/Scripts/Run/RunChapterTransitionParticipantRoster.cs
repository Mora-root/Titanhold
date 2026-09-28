using System;
using System.Collections.Generic;
using Titanhold.Session;
using UnityEngine;

namespace Titanhold.Run
{
    public sealed class RunChapterTransitionParticipantRoster :
        IRunChapterTransitionParticipantRoster
    {
        private readonly IReadOnlyList<RunSceneParticipantBinding> participants;

        public RunChapterTransitionParticipantRoster(
            IReadOnlyList<RunSceneParticipantBinding> participants)
        {
            this.participants = participants ??
                throw new ArgumentNullException(nameof(participants));
        }

        public bool Contains(string participantId)
        {
            string normalizedId = participantId?.Trim() ?? string.Empty;
            if (normalizedId.Length == 0)
                return false;

            for (int i = 0; i < participants.Count; i++)
            {
                RunSceneParticipantBinding participant = participants[i];
                if (participant != null &&
                    participant.IsValid &&
                    string.Equals(
                        participant.PlayerId,
                        normalizedId,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryResolveParticipantId(
            GameObject interactor,
            out string participantId)
        {
            participantId = string.Empty;
            if (interactor == null)
                return false;

            Transform interactorRoot = interactor.transform.root;
            for (int i = 0; i < participants.Count; i++)
            {
                RunSceneParticipantBinding participant = participants[i];
                if (participant == null ||
                    !participant.IsValid ||
                    participant.Inventory == null ||
                    participant.Inventory.transform.root != interactorRoot)
                {
                    continue;
                }

                participantId = participant.PlayerId;
                return true;
            }

            return false;
        }
    }
}
