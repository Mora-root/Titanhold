using UnityEngine;

public enum ItemRarity
{
    Common,
    Magic,
    Rare,
    Epic,
    Legendary,
    Unique
}

public static class ItemRarityUtility
{
    public static Color GetColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Magic:
                return new Color(0.25f, 0.55f, 1f, 1f);
            case ItemRarity.Rare:
                return new Color(1f, 0.86f, 0.25f, 1f);
            case ItemRarity.Epic:
                return new Color(0.72f, 0.35f, 1f, 1f);
            case ItemRarity.Legendary:
                return new Color(1f, 0.48f, 0.12f, 1f);
            case ItemRarity.Unique:
                return new Color(1f, 0.2f, 0.2f, 1f);
            default:
                return new Color(0.88f, 0.88f, 0.88f, 1f);
        }
    }
}
