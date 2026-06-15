using System.Collections.Generic;

namespace Titanhold.UI.Common
{
    public static class ItemTooltipBuilder
    {
        public static ItemTooltipData Build(global::ItemStack stack)
        {
            if (stack == null || stack.Definition == null)
                return null;

            return Build(stack.Definition, stack.Amount, stack.Instance);
        }

        public static ItemTooltipData Build(global::ItemInstance instance)
        {
            if (instance == null || instance.Definition == null)
                return null;

            return Build(instance.Definition, 1, instance);
        }

        public static ItemTooltipData Build(global::ItemDefinition definition, int amount = 1)
        {
            if (definition == null)
                return null;

            ItemTooltipData data = new ItemTooltipData
            {
                Title = definition.DisplayName,
                Subtitle = BuildSubtitle(definition),
                Description = definition.Description,
                Footer = BuildFooter(definition),
                SellPriceText = BuildSellPriceText(definition, amount),
                StackText = BuildStackText(definition, amount)
            };

            AddEquipmentBlock(data, definition);
            AddModifierBlock(data, definition);

            return data;
        }

        private static ItemTooltipData Build(global::ItemDefinition definition, int amount, global::ItemInstance instance)
        {
            ItemTooltipData data = Build(definition, amount);
            AddGeneratedModifierBlock(data, definition, instance);
            return data;
        }

        private static void AddEquipmentBlock(ItemTooltipData data, global::ItemDefinition definition)
        {
            if (!definition.IsEquippable)
                return;

            if (definition.IsWeapon)
            {
                data.AddBlock(string.Empty, new[]
                {
                    BuildWeaponHandednessText(definition)
                });

                data.AddBlock(string.Empty, new[]
                {
                    $"Base Damage: {definition.WeaponBaseDamage:0.##}",
                    $"Attack Speed: {definition.WeaponBaseAttacksPerSecond:0.##}/s"
                });
            }
        }

        private static void AddModifierBlock(ItemTooltipData data, global::ItemDefinition definition)
        {
            if (!definition.IsEquippable || definition.Modifiers == null || definition.Modifiers.Count == 0)
                return;

            List<string> lines = new List<string>();

            foreach (global::StatModifierData modifier in definition.Modifiers)
            {
                string value = FormatModifierValue(modifier);
                lines.Add($"{value} {FormatStatName(modifier.Type)}");
            }

            if (definition.IsWeapon)
                data.AddBlock("--------", null);

            data.AddBlock("Modifiers", lines);
        }

        private static void AddGeneratedModifierBlock(
            ItemTooltipData data,
            global::ItemDefinition definition,
            global::ItemInstance instance)
        {
            if (!definition.IsEquippable || instance == null || instance.GeneratedModifiers == null || instance.GeneratedModifiers.Count == 0)
                return;

            List<string> lines = new List<string>();

            foreach (global::StatModifierData modifier in instance.GeneratedModifiers)
            {
                string value = FormatModifierValue(modifier);
                lines.Add($"{value} {FormatStatName(modifier.Type)}");
            }

            if (definition.Modifiers == null || definition.Modifiers.Count == 0)
            {
                if (definition.IsWeapon)
                    data.AddBlock("--------", null);

                data.AddBlock("Modifiers", lines);
                return;
            }

            data.AddBlock("Rolled", lines);
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

        private static string BuildSubtitle(global::ItemDefinition definition)
        {
            if (definition == null)
                return string.Empty;

            if (!definition.IsEquipment)
                return definition.CategoryDisplayName;

            if (definition.IsWeapon)
                return BuildWeaponSubtitle(definition);

            return definition.EquipmentSlotType switch
            {
                global::EquipmentSlotType.Shield => "Shield",
                global::EquipmentSlotType.Head => "Head Armor",
                global::EquipmentSlotType.Chest => "Chest Armor",
                global::EquipmentSlotType.Hands => "Gloves",
                global::EquipmentSlotType.Legs => "Leg Armor",
                global::EquipmentSlotType.Feet => "Boots",
                global::EquipmentSlotType.Amulet => "Amulet",
                global::EquipmentSlotType.Ring => "Ring",
                global::EquipmentSlotType.Artifact => "Artifact",
                _ => definition.CategoryDisplayName
            };
        }

        private static string BuildWeaponSubtitle(global::ItemDefinition definition)
        {
            string family = BuildWeaponFamilyText(definition.WeaponFamily);
            string handedness = BuildWeaponHandednessText(definition);

            if (string.IsNullOrWhiteSpace(family))
                return "Weapon";

            if (string.IsNullOrWhiteSpace(handedness))
                return family;

            return $"{handedness} {family}";
        }

        private static string BuildWeaponFamilyText(global::WeaponFamily family)
        {
            return family switch
            {
                global::WeaponFamily.Sword => "Sword",
                global::WeaponFamily.Axe => "Axe",
                global::WeaponFamily.Mace => "Mace",
                global::WeaponFamily.Hammer => "Hammer",
                global::WeaponFamily.Dagger => "Dagger",
                global::WeaponFamily.Staff => "Staff",
                global::WeaponFamily.Bow => "Bow",
                _ => string.Empty
            };
        }

        private static string BuildWeaponHandednessText(global::ItemDefinition definition)
        {
            return definition.WeaponHandedness switch
            {
                global::WeaponHandedness.OneHand => "One-handed",
                global::WeaponHandedness.TwoHand => "Two-handed",
                _ => string.Empty
            };
        }

        private static string BuildStackText(global::ItemDefinition definition, int amount)
        {
            if (!definition.IsStackable && amount <= 1)
                return string.Empty;

            if (definition.IsStackable)
                return $"Amount: {amount}    Max: {definition.MaxStack}";

            return $"Amount: {amount}";
        }

        private static string BuildSellPriceText(global::ItemDefinition definition, int amount)
        {
            if (definition == null || definition.SellValue <= 0)
                return string.Empty;

            int safeAmount = System.Math.Max(1, amount);
            int totalSellValue = definition.SellValue * safeAmount;
            return $"Sell: {totalSellValue}";
        }

        private static string FormatModifierValue(global::StatModifierData modifier)
        {
            string number = FormatModifierNumber(modifier.Value);

            return modifier.ModifierType switch
            {
                global::StatModifierType.Increased => $"{FormatSignedModifierNumber(modifier.Value)}%",
                global::StatModifierType.More => $"{FormatSignedModifierNumber(modifier.Value)}% more",
                global::StatModifierType.Override => IsPercentLikeFlatStat(modifier.Type) ? $"={number}%" : $"={number}",
                _ => IsPercentLikeFlatStat(modifier.Type)
                    ? $"{FormatSignedModifierNumber(modifier.Value)}%"
                    : FormatSignedModifierNumber(modifier.Value)
            };
        }

        private static string FormatSignedModifierNumber(float value)
        {
            return value > 0f ? $"+{FormatModifierNumber(value)}" : FormatModifierNumber(value);
        }

        private static string FormatModifierNumber(float value)
        {
            return value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static bool IsPercentLikeFlatStat(global::StatType type)
        {
            return type == global::StatType.AttackSpeed || type == global::StatType.MoveSpeed;
        }

        private static string FormatStatName(global::StatType type)
        {
            return type switch
            {
                global::StatType.MaxHealth => "Max Health",
                global::StatType.MaxResource => "Max Resource",
                global::StatType.AttackSpeed => "Attack Speed",
                global::StatType.MoveSpeed => "Move Speed",
                global::StatType.AttackRange => "Attack Range",
                _ => type.ToString()
            };
        }
    }
}
