using System;
using System.Collections.Generic;
using UnityEngine;

namespace Titanhold.Run
{
    [CreateAssetMenu(
        fileName = "RunStatUpgrade",
        menuName = "Titanhold/Run/Stat Upgrade")]
    public sealed class RunStatUpgradeDefinition :
        ScriptableObject,
        IRunStatUpgradeDefinition,
        IRunUpgradePresentationDefinition
    {
        [SerializeField] private string upgradeId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private StatModifierData[] modifiers =
            Array.Empty<StatModifierData>();

        public string UpgradeId => upgradeId ?? string.Empty;
        public string DisplayName => displayName ?? string.Empty;
        public string Description => description ?? string.Empty;
        public Sprite Icon => icon;
        public IReadOnlyList<StatModifierData> Modifiers =>
            modifiers ?? Array.Empty<StatModifierData>();

        public bool TryValidate(out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(upgradeId) ||
                !string.Equals(
                    upgradeId,
                    upgradeId.Trim(),
                    StringComparison.Ordinal))
            {
                error = $"Run upgrade '{name}' has an invalid stable id.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                error = $"Run upgrade '{name}' has no display name.";
                return false;
            }

            StatModifierData[] source =
                modifiers ?? Array.Empty<StatModifierData>();
            if (source.Length == 0)
            {
                error = $"Run stat upgrade '{name}' has no modifiers.";
                return false;
            }

            for (int i = 0; i < source.Length; i++)
            {
                float value = source[i].Value;
                if (value == 0f || float.IsNaN(value) ||
                    float.IsInfinity(value))
                {
                    error =
                        $"Run stat upgrade '{name}' has an invalid modifier at index {i}.";
                    return false;
                }
            }

            return true;
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string configuredUpgradeId,
            string configuredDisplayName,
            string configuredDescription,
            Sprite configuredIcon,
            StatModifierData[] configuredModifiers)
        {
            upgradeId = configuredUpgradeId;
            displayName = configuredDisplayName;
            description = configuredDescription;
            icon = configuredIcon;
            modifiers = configuredModifiers ?? Array.Empty<StatModifierData>();
        }
#endif
    }
}
