using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Project/Items/Item Definition")]
public sealed class ItemDefinition : ScriptableObject
{
    [Header("Common")]
    [SerializeField] private string id;
    [SerializeField] private string displayName;
    [SerializeField] private string shortName;
    [SerializeField] private Sprite icon;
    [SerializeField] private string description;
    [SerializeField] private ItemCategory category = ItemCategory.Misc;
    [SerializeField, Min(1)] private int maxStack = 1;

    [Header("World Pickup Presentation")]
    [SerializeField] private ItemRarity rarity = ItemRarity.Common;
    [SerializeField] private GameObject worldPickupVisualPrefab;
    [SerializeField] private GameObject worldPickupEffectPrefab;
    [SerializeField] private bool overridePickupLabelColor;
    [SerializeField] private Color pickupLabelColor = Color.white;

    [Header("Equipment")]
    [SerializeField] private EquipmentSlotType equipmentSlotType;
    [SerializeField, Min(0f)] private float equipmentBaseArmor;
    [SerializeField] private StatModifierData[] modifiers;

    [Header("Weapon")]
    [SerializeField] private WeaponType weaponType;
    [SerializeField, Min(0f)] private float weaponBaseDamage;
    [SerializeField, Min(0.01f)] private float weaponBaseAttacksPerSecond = 1f;

    [Header("Consumable")]
    [SerializeField] private ConsumableSubtype consumableSubtype;
    [SerializeField] private string useActionId;
    [SerializeField, Min(1)] private int consumeAmountOnUse = 1;
    [SerializeField] private ActivityItemSubtype activitySubtype;
    [SerializeField] private ActivityModifierData[] activityModifiers;

    [Header("Trophy")]
    [SerializeField] private TrophySubtype trophySubtype;

    [Header("Crafting")]
    [SerializeField] private CraftingSubtype craftingSubtype;

    [Header("Economy")]
    [SerializeField] private int sellValue;
    [SerializeField] private bool canBeSold = true;
    [SerializeField] private ItemSellWarningType sellWarningType;
    [SerializeField] private bool canBeDismantled;

    [Header("External Drop")]
    [SerializeField] private ItemExternalDropAction externalDropAction;
    [SerializeField] private bool requiresConfirmationOnExternalDrop;

    public string Id => id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string ShortName => string.IsNullOrWhiteSpace(shortName) ? DisplayName : shortName;
    public Sprite Icon => icon;
    public string Description => description;
    public ItemCategory Category => category;
    public string CategoryDisplayName => category.ToString();
    public int MaxStack => Mathf.Max(1, maxStack);
    public bool IsStackable => MaxStack > 1;
    public ItemRarity Rarity => rarity;
    public GameObject WorldPickupVisualPrefab => worldPickupVisualPrefab;
    public GameObject WorldPickupEffectPrefab => worldPickupEffectPrefab;
    public Color PickupLabelColor => overridePickupLabelColor ? pickupLabelColor : ItemRarityUtility.GetColor(rarity);

    public bool IsEquipment => category == ItemCategory.Equipment;
    public EquipmentSlotType EquipmentSlotType => IsEquipment ? equipmentSlotType : EquipmentSlotType.None;
    public bool IsEquippable => IsEquipment && equipmentSlotType != EquipmentSlotType.None;
    public bool IsWeapon => IsEquipment && equipmentSlotType == EquipmentSlotType.Weapon;
    public bool IsShield => IsEquipment && equipmentSlotType == EquipmentSlotType.Shield;
    public bool IsArtifact => IsEquipment && equipmentSlotType == EquipmentSlotType.Artifact;
    public float EquipmentBaseArmor => IsEquippable && !IsWeapon ? Mathf.Max(0f, equipmentBaseArmor) : 0f;
    public WeaponType WeaponType => IsWeapon ? weaponType : WeaponType.None;
    public WeaponFamily WeaponFamily => GetWeaponFamily(WeaponType);
    public WeaponHandedness WeaponHandedness => GetWeaponHandedness(WeaponType);
    public WeaponHandedness Handedness => WeaponHandedness;
    public bool OccupiesBothHands => WeaponHandedness == WeaponHandedness.TwoHand;
    public float WeaponBaseDamage => IsWeapon ? Mathf.Max(0f, weaponBaseDamage) : 0f;
    public float WeaponBaseAttacksPerSecond => IsWeapon ? Mathf.Max(0.01f, weaponBaseAttacksPerSecond) : 0f;
    public IReadOnlyList<StatModifierData> Modifiers => modifiers ?? Array.Empty<StatModifierData>();

    public bool IsConsumable => category == ItemCategory.Consumable;
    public ConsumableSubtype ConsumableSubtype => IsConsumable ? consumableSubtype : ConsumableSubtype.None;
    public string UseActionId => useActionId;
    public int ConsumeAmountOnUse => Mathf.Max(1, consumeAmountOnUse);
    public bool IsActivityToken => IsConsumable && consumableSubtype == ConsumableSubtype.ActivityToken;
    public ActivityItemSubtype ActivitySubtype => IsActivityToken ? activitySubtype : ActivityItemSubtype.None;
    public IReadOnlyList<ActivityModifierData> ActivityModifiers => activityModifiers ?? Array.Empty<ActivityModifierData>();
    public bool IsUsable => IsConsumable && !string.IsNullOrWhiteSpace(useActionId);
    public bool HasActivityModifiers => IsActivityToken && ActivityModifiers.Count > 0;

    public TrophySubtype TrophySubtype => category == ItemCategory.Trophy ? trophySubtype : TrophySubtype.None;
    public CraftingSubtype CraftingSubtype => category == ItemCategory.Crafting ? craftingSubtype : CraftingSubtype.None;

    public int SellValue => Mathf.Max(0, sellValue);
    public bool CanBeSold => category != ItemCategory.Quest && canBeSold;
    public ItemSellWarningType SellWarningType => sellWarningType;
    public bool ShouldWarnBeforeSelling => sellWarningType != ItemSellWarningType.None;
    public bool CanBeDismantled => category != ItemCategory.Quest && canBeDismantled;

    public ItemExternalDropAction ExternalDropAction => category == ItemCategory.Quest
        ? ItemExternalDropAction.Deny
        : externalDropAction;
    public bool CanBeDroppedToWorld => ExternalDropAction == ItemExternalDropAction.DropToWorld;
    public bool CanBeDestroyed => ExternalDropAction == ItemExternalDropAction.Destroy;
    public bool RequiresConfirmationOnExternalDrop =>
        ExternalDropAction != ItemExternalDropAction.Deny && requiresConfirmationOnExternalDrop;

    private void OnValidate()
    {
        maxStack = Mathf.Max(1, maxStack);
        equipmentBaseArmor = Mathf.Max(0f, equipmentBaseArmor);
        weaponBaseDamage = Mathf.Max(0f, weaponBaseDamage);
        weaponBaseAttacksPerSecond = Mathf.Max(0.01f, weaponBaseAttacksPerSecond);
        consumeAmountOnUse = Mathf.Max(1, consumeAmountOnUse);
        sellValue = Mathf.Max(0, sellValue);

        ValidateRules();
        ValidateUniqueId();
    }

    private void ValidateRules()
    {
        if (string.IsNullOrWhiteSpace(id))
            LogValidationWarning("Id should be non-empty.");

        if (category == ItemCategory.Equipment)
        {
            if (maxStack != 1)
                LogValidationWarning("Equipment items must have MaxStack equal to 1.");

            if (equipmentSlotType == EquipmentSlotType.None)
                LogValidationWarning("Equipment items must have EquipmentSlotType different from None.");
        }
        else if (equipmentSlotType != EquipmentSlotType.None)
        {
            LogValidationWarning("Non-equipment items must have EquipmentSlotType set to None.");
        }

        if (equipmentSlotType == EquipmentSlotType.Weapon)
        {
            if (weaponType == WeaponType.None)
                LogValidationWarning("Weapon items must have WeaponType different from None.");
        }
        else if (weaponType != WeaponType.None)
        {
            LogValidationWarning("Non-weapon equipment must have WeaponType set to None.");
        }

        if (category == ItemCategory.Consumable)
        {
            if (consumableSubtype == ConsumableSubtype.None)
                LogValidationWarning("Consumable items should have ConsumableSubtype different from None.");
        }
        else if (consumableSubtype != ConsumableSubtype.None)
        {
            LogValidationWarning("Non-consumable items should have ConsumableSubtype set to None.");
        }

        if (consumableSubtype == ConsumableSubtype.ActivityToken)
        {
            if (activitySubtype == ActivityItemSubtype.None)
                LogValidationWarning("Activity tokens should have ActivitySubtype different from None.");
        }
        else
        {
            if (activitySubtype != ActivityItemSubtype.None)
                LogValidationWarning("Non-activity consumables should have ActivitySubtype set to None.");

            if (activityModifiers != null && activityModifiers.Length > 0)
                LogValidationWarning("ActivityModifiers should be empty unless ConsumableSubtype is ActivityToken.");
        }

        if (category == ItemCategory.Crafting)
        {
            if (craftingSubtype == CraftingSubtype.None)
                LogValidationWarning("Crafting items should have CraftingSubtype different from None.");
        }
        else if (craftingSubtype != CraftingSubtype.None)
        {
            LogValidationWarning("Non-crafting items should have CraftingSubtype set to None.");
        }

        if (category == ItemCategory.Trophy)
        {
            if (trophySubtype == TrophySubtype.None)
                LogValidationWarning("Trophy items should have TrophySubtype different from None.");
        }
        else if (trophySubtype != TrophySubtype.None)
        {
            LogValidationWarning("Non-trophy items should have TrophySubtype set to None.");
        }

        if (category == ItemCategory.Quest)
        {
            if (canBeSold)
                LogValidationWarning("Quest items cannot be sold. Raw canBeSold flag is ignored.");

            if (canBeDismantled)
                LogValidationWarning("Quest items cannot be dismantled. Raw canBeDismantled flag is ignored.");

            if (externalDropAction != ItemExternalDropAction.Deny)
                LogValidationWarning("Quest items cannot be externally dropped or destroyed. Raw ExternalDropAction is ignored.");

            if (sellValue > 0)
                LogValidationWarning("Quest items should have SellValue equal to 0.");
        }

        if (ExternalDropAction == ItemExternalDropAction.Destroy && !requiresConfirmationOnExternalDrop)
            LogValidationWarning("Destroy external drop action should require confirmation.");

        if (ExternalDropAction == ItemExternalDropAction.Deny && requiresConfirmationOnExternalDrop)
            LogValidationWarning("External drop confirmation flag is ignored when ExternalDropAction is Deny.");
    }

    private void LogValidationWarning(string message)
    {
        Debug.LogWarning($"ItemDefinition '{name}': {message}", this);
    }

    private static WeaponFamily GetWeaponFamily(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.OneHandSword:
            case WeaponType.TwoHandSword:
                return WeaponFamily.Sword;
            case WeaponType.OneHandAxe:
            case WeaponType.TwoHandAxe:
                return WeaponFamily.Axe;
            case WeaponType.Mace:
                return WeaponFamily.Mace;
            case WeaponType.Hammer:
                return WeaponFamily.Hammer;
            case WeaponType.Dagger:
                return WeaponFamily.Dagger;
            case WeaponType.Staff:
                return WeaponFamily.Staff;
            case WeaponType.Bow:
                return WeaponFamily.Bow;
            default:
                return WeaponFamily.None;
        }
    }

    private static WeaponHandedness GetWeaponHandedness(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.OneHandSword:
            case WeaponType.OneHandAxe:
            case WeaponType.Mace:
            case WeaponType.Dagger:
                return WeaponHandedness.OneHand;
            case WeaponType.TwoHandSword:
            case WeaponType.TwoHandAxe:
            case WeaponType.Hammer:
            case WeaponType.Staff:
            case WeaponType.Bow:
                return WeaponHandedness.TwoHand;
            default:
                return WeaponHandedness.None;
        }
    }

    private void ValidateUniqueId()
    {
#if UNITY_EDITOR
        if (string.IsNullOrWhiteSpace(id))
            return;

        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ItemDefinition");
        string ownPath = UnityEditor.AssetDatabase.GetAssetPath(this);

        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            if (path == ownPath)
                continue;

            ItemDefinition other = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (other != null && other.id == id)
                LogValidationWarning($"Duplicate item id '{id}' also used by asset '{path}'.");
        }
#endif
    }
}
