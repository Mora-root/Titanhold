using UnityEngine;

namespace Titanhold.UI.Common
{
    public readonly struct ItemDragVisualData
    {
        public ItemDragVisualData(Sprite icon, int amount)
        {
            Icon = icon;
            Amount = amount;
        }

        public Sprite Icon { get; }
        public int Amount { get; }
    }
}
