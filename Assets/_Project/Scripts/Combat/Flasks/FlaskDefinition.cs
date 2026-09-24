using System;
using UnityEngine;

namespace Titanhold.Combat.Flasks
{
    public enum FlaskRecoveryTarget
    {
        Health = 0,
        PrimaryResource = 1
    }

    public readonly struct FlaskDefinitionSnapshot
    {
        public FlaskDefinitionSnapshot(
            string flaskId,
            FlaskRecoveryTarget recoveryTarget,
            float recoveryFraction,
            double cooldown)
        {
            if (string.IsNullOrWhiteSpace(flaskId))
                throw new ArgumentException(
                    "A flask requires a stable definition id.",
                    nameof(flaskId));
            if (!Enum.IsDefined(typeof(FlaskRecoveryTarget), recoveryTarget))
                throw new ArgumentOutOfRangeException(nameof(recoveryTarget));
            if (recoveryFraction <= 0f ||
                float.IsNaN(recoveryFraction) ||
                float.IsInfinity(recoveryFraction))
            {
                throw new ArgumentOutOfRangeException(nameof(recoveryFraction));
            }

            if (cooldown < 0d ||
                double.IsNaN(cooldown) ||
                double.IsInfinity(cooldown))
            {
                throw new ArgumentOutOfRangeException(nameof(cooldown));
            }

            FlaskId = flaskId.Trim();
            RecoveryTarget = recoveryTarget;
            RecoveryFraction = recoveryFraction;
            Cooldown = cooldown;
        }

        public string FlaskId { get; }
        public FlaskRecoveryTarget RecoveryTarget { get; }
        public float RecoveryFraction { get; }
        public double Cooldown { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(FlaskId);
    }

    [CreateAssetMenu(menuName = "Titanhold/Combat/Flask")]
    public sealed class FlaskDefinition : ScriptableObject
    {
        [SerializeField] private string flaskId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private FlaskRecoveryTarget recoveryTarget;
        [SerializeField, Min(0.01f)] private float recoveryFraction = 0.5f;
        [SerializeField, Min(0f)] private float cooldown = 30f;

        public string FlaskId => flaskId?.Trim() ?? string.Empty;
        public string DisplayName => displayName?.Trim() ?? string.Empty;
        public string Description => description?.Trim() ?? string.Empty;
        public Sprite Icon => icon;
        public FlaskRecoveryTarget RecoveryTarget => recoveryTarget;

        public bool TryCreateSnapshot(out FlaskDefinitionSnapshot snapshot)
        {
            try
            {
                snapshot = new FlaskDefinitionSnapshot(
                    flaskId,
                    recoveryTarget,
                    recoveryFraction,
                    cooldown);
                return true;
            }
            catch (ArgumentException)
            {
                snapshot = default;
                return false;
            }
        }
    }
}
