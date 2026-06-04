using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Project/Equipment/Equipment Item Definition")]
public sealed class EquipmentItemDefinition : ScriptableObject
{
    [Serializable]
    private struct EquipmentWeaponData
    {
        [SerializeField] private WeaponHandedness handedness;
        [SerializeField] private WeaponFamily family;

        public WeaponHandedness Handedness => handedness;
        public WeaponFamily Family => family;
    }

    [Serializable]
    private struct EquipmentWearableData
    {
        [SerializeField] private EquipmentType type;
        [SerializeField] private EquipmentSlot defaultSlot;

        public EquipmentType Type => type;
        public EquipmentSlot DefaultSlot => defaultSlot;
    }

    [SerializeField] private string displayName;
    [SerializeField] private string shortName;
    [SerializeField] private Sprite icon;
    [SerializeField] private string description;
    [SerializeField] private EquipmentItemCategory category;
    [SerializeField] private EquipmentWeaponData weaponData;
    [SerializeField] private EquipmentWearableData equipmentData;
    [SerializeField] private StatModifierData[] modifiers;

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string ShortName => string.IsNullOrWhiteSpace(shortName) ? DisplayName : shortName;
    public Sprite Icon => icon;
    public string Description => description;
    public EquipmentItemCategory Category => category;
    public bool IsWeapon => category == EquipmentItemCategory.Weapon;
    public bool IsEquipment => category == EquipmentItemCategory.Equipment;
    public EquipmentType EquipmentType => IsEquipment ? equipmentData.Type : default;
    public WeaponFamily WeaponFamily => IsWeapon ? weaponData.Family : global::WeaponFamily.None;
    public WeaponHandedness Handedness => IsWeapon ? weaponData.Handedness : default;
    public bool OccupiesBothHands => IsWeapon && weaponData.Handedness == WeaponHandedness.TwoHand;
    public EquipmentSlot DefaultSlot
    {
        get
        {
            if (IsWeapon)
                return EquipmentSlot.MainHand;

            if (equipmentData.Type == global::EquipmentType.Shield)
                return EquipmentSlot.OffHand;

            return equipmentData.DefaultSlot;
        }
    }

    public IReadOnlyList<StatModifierData> Modifiers => modifiers ?? System.Array.Empty<StatModifierData>();
}
