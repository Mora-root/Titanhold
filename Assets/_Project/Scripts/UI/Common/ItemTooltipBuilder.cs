using System.Collections.Generic;

namespace Titanhold.UI.Common
{
    public static class ItemTooltipBuilder
    {
        public static ItemTooltipData Build(global::ItemStack stack)
        {
            if (stack == null || stack.Definition == null)
                return null;

            return Build(stack.Definition, stack.Amount);
        }

        public static ItemTooltipData Build(global::ItemInstance instance)
        {
            if (instance == null || instance.Definition == null)
                return null;

            return Build(instance.Definition, 1);
        }

        public static ItemTooltipData Build(global::ItemDefinition definition, int amount = 1)
        {
            if (definition == null)
                return null;

            ItemTooltipData data = new ItemTooltipData
            {
                Title = definition.DisplayName,
                Subtitle = definition.CategoryDisplayName,
                Description = definition.Description,
                Footer = BuildFooter(definition),
                SellPriceText = BuildSellPriceText(definition),
                StackText = BuildStackText(definition, amount)
            };

            AddEquipmentBlock(data, definition);
            AddModifierBlock(data, definition);

            return data;
        }

        private static void AddEquipmentBlock(ItemTooltipData data, global::ItemDefinition definition)
        {
            if (!definition.IsEquippable)
                return;

            List<string> lines = new List<string>
            {
                $"Slot: {definition.EquipmentSlotType}"
            };

            if (definition.IsWeapon)
            {
                lines.Add($"Weapon Type: {definition.WeaponType}");
            }

            data.AddBlock(string.Empty, lines);
        }

        private static void AddModifierBlock(ItemTooltipData data, global::ItemDefinition definition)
        {
            if (!definition.IsEquippable || definition.Modifiers == null || definition.Modifiers.Count == 0)
                return;

            List<string> lines = new List<string>();

            foreach (global::StatModifierData modifier in definition.Modifiers)
            {
                string value = FormatModifierValue(modifier);
                lines.Add($"{value} {modifier.Type}");
            }

            data.AddBlock("Modifiers", lines);
        }

        private static string BuildFooter(global::ItemDefinition definition)
        {
            if (definition == null)
                return string.Empty;

            if (definition.Category == global::ItemCategory.Crafting && definition.CraftingSubtype != global::CraftingSubtype.None)
                return definition.CraftingSubtype.ToString();

            if (definition.Category == global::ItemCategory.Trophy && definition.TrophySubtype != global::TrophySubtype.None)
                return definition.TrophySubtype.ToString();

            if (definition.Category == global::ItemCategory.Consumable && definition.ConsumableSubtype != global::ConsumableSubtype.None)
                return definition.ConsumableSubtype.ToString();

            return string.Empty;
        }

        private static string BuildStackText(global::ItemDefinition definition, int amount)
        {
            if (!definition.IsStackable && amount <= 1)
                return string.Empty;

            if (definition.IsStackable)
                return $"Amount: {amount}    Max: {definition.MaxStack}";

            return $"Amount: {amount}";
        }

        private static string BuildSellPriceText(global::ItemDefinition definition)
        {
            if (definition == null || definition.SellValue <= 0)
                return string.Empty;

            return $"Sell: {definition.SellValue}";
        }

        private static string FormatModifierValue(global::StatModifierData modifier)
        {
            if (modifier.ModifierType == global::StatModifierType.Percent)
                return $"{modifier.Value:+0.##;-0.##;0}%";

            return $"{modifier.Value:+0.##;-0.##;0}";
        }
    }
}
