using System.Collections.Generic;
using UnityEngine;

namespace Titanhold.Run
{
    public interface IRunUpgradeDefinition
    {
        string UpgradeId { get; }
        bool TryValidate(out string error);
    }

    public interface IRunStatUpgradeDefinition : IRunUpgradeDefinition
    {
        IReadOnlyList<StatModifierData> Modifiers { get; }
    }

    public interface IRunUpgradePresentationDefinition
    {
        string DisplayName { get; }
        string Description { get; }
        Sprite Icon { get; }
    }

    public interface IRunUpgradeDefinitionResolver
    {
        bool TryResolve(
            string upgradeId,
            out IRunUpgradeDefinition definition);
    }
}
