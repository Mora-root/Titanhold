using UnityEngine;

namespace Titanhold.Combat.Abilities
{
    public interface IAbilityPresentationDefinition
    {
        string DisplayName { get; }
        string Description { get; }
        Sprite Icon { get; }
    }
}
