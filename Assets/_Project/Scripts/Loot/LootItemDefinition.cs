using UnityEngine;

public enum LootItemCategory
{
    Material,
    Trophy,
    Sellable,
    Other
}

[CreateAssetMenu(menuName = "Project/Loot/Loot Item Definition")]
public sealed class LootItemDefinition : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField] private LootItemCategory category = LootItemCategory.Other;

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public LootItemCategory Category => category;
}
