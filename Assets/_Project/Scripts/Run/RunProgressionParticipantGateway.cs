using System;
using UnityEngine;

namespace Titanhold.Run
{
    [DisallowMultipleComponent]
    public sealed class RunProgressionParticipantGateway : MonoBehaviour
    {
        private RunProgressionService progression;
        private string playerId = string.Empty;

        public string PlayerId => playerId;
        public bool IsBound => progression != null &&
                               !string.IsNullOrWhiteSpace(playerId);

        internal bool TryBind(
            RunProgressionService configuredProgression,
            string configuredPlayerId)
        {
            string normalizedPlayerId =
                configuredPlayerId?.Trim() ?? string.Empty;
            if (configuredProgression == null ||
                normalizedPlayerId.Length == 0)
            {
                return false;
            }

            if (IsBound &&
                (!ReferenceEquals(progression, configuredProgression) ||
                 !string.Equals(
                     playerId,
                     normalizedPlayerId,
                     StringComparison.Ordinal)))
            {
                return false;
            }

            progression = configuredProgression;
            playerId = normalizedPlayerId;
            return true;
        }

        internal void Unbind(RunProgressionService expectedProgression)
        {
            if (!ReferenceEquals(progression, expectedProgression))
                return;

            progression = null;
            playerId = string.Empty;
        }

        public bool TryAddGold(
            int amount,
            out RunProgressionResult result)
        {
            result = default;
            if (!IsBound)
                return false;

            result = progression.TryAddGold(playerId, amount);
            return result.Success;
        }
    }
}
