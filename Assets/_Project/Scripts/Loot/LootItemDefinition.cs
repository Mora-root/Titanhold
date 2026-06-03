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
    [SerializeField] private string shortName;
    [SerializeField] private string description;
    [SerializeField] private Sprite icon;
    [SerializeField] private LootItemCategory category = LootItemCategory.Other;

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string ShortName => string.IsNullOrWhiteSpace(shortName) ? DisplayName : shortName;
    public string Description => description;
    public Sprite Icon => icon;
    public LootItemCategory Category => category;
    public string CategoryDisplayName => category.ToString();
}
