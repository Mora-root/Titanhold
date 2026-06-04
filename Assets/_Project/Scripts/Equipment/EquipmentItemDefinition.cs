using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Project/Equipment/Equipment Item Definition")]
public sealed class EquipmentItemDefinition : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField] private string shortName;
    [SerializeField] private Sprite icon;
    [SerializeField] private string description;
    [SerializeField] private EquipmentSlot defaultSlot;
    [SerializeField] private EquipmentKind kind;
    [SerializeField] private WeaponType weaponType;
    [SerializeField] private WeaponHandedness handedness;
    [SerializeField] private StatModifierData[] modifiers;

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string ShortName => string.IsNullOrWhiteSpace(shortName) ? DisplayName : shortName;
    public Sprite Icon => icon;
    public string Description => description;
    public EquipmentSlot DefaultSlot => defaultSlot;
    public EquipmentKind Kind => kind;
    public WeaponType WeaponType => weaponType;
    public WeaponHandedness Handedness => handedness;
    public bool OccupiesBothHands => handedness == WeaponHandedness.TwoHand;
    public IReadOnlyList<StatModifierData> Modifiers => modifiers ?? System.Array.Empty<StatModifierData>();
}
