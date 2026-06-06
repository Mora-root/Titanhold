using System;
using UnityEngine;

public sealed class EquipmentStatsBinder : MonoBehaviour
{
    [SerializeField] private PlayerEquipmentRuntime equipmentRuntime;
    [SerializeField] private CharacterStats characterStats;

    private CharacterEquipment subscribedEquipment;
    private bool loggedMissingEquipmentRuntime;
    private bool loggedMissingCharacterStats;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        Subscribe();
        ApplyCurrentEquipment();
    }

    private void OnDisable()
    {
        RemoveAllEquipmentModifiers();
        Unsubscribe();
    }

    private void ResolveReferences()
    {
        if (equipmentRuntime == null)
            equipmentRuntime = GetComponent<PlayerEquipmentRuntime>();

        if (characterStats == null)
            characterStats = GetComponent<CharacterStats>();
    }

    private void Subscribe()
    {
        CharacterEquipment equipment = GetEquipment();
        if (equipment == null)
            return;

        if (ReferenceEquals(subscribedEquipment, equipment))
            return;

        Unsubscribe();
        subscribedEquipment = equipment;
        subscribedEquipment.SlotChanged += HandleSlotChanged;
    }

    private void Unsubscribe()
    {
        if (subscribedEquipment == null)
            return;

        subscribedEquipment.SlotChanged -= HandleSlotChanged;
        subscribedEquipment = null;
    }

    private void HandleSlotChanged(EquipmentSlotId slotId, ItemInstance oldItem, ItemInstance newItem)
    {
        if (characterStats == null)
        {
            LogMissingCharacterStats();
            return;
        }

        characterStats.RemoveModifiersFromSource(slotId);
        ApplyModifiers(slotId, newItem);
    }

    private void ApplyCurrentEquipment()
    {
        CharacterEquipment equipment = GetEquipment();
        if (equipment == null)
            return;

        if (characterStats == null)
        {
            LogMissingCharacterStats();
            return;
        }

        foreach (EquipmentSlotId slotId in Enum.GetValues(typeof(EquipmentSlotId)))
        {
            characterStats.RemoveModifiersFromSource(slotId);
            ApplyModifiers(slotId, equipment.GetEquipped(slotId));
        }
    }

    private void ApplyModifiers(EquipmentSlotId slotId, ItemInstance item)
    {
        if (characterStats == null || item == null || item.Definition == null)
            return;

        characterStats.AddModifiers(item.Definition.Modifiers, slotId);
    }

    private void RemoveAllEquipmentModifiers()
    {
        if (characterStats == null)
            return;

        foreach (EquipmentSlotId slotId in Enum.GetValues(typeof(EquipmentSlotId)))
        {
            characterStats.RemoveModifiersFromSource(slotId);
        }
    }

    private CharacterEquipment GetEquipment()
    {
        if (equipmentRuntime == null)
        {
            LogMissingEquipmentRuntime();
            return null;
        }

        CharacterEquipment equipment = equipmentRuntime.Equipment;
        if (equipment == null)
            LogMissingEquipmentRuntime();

        return equipment;
    }

    private void LogMissingEquipmentRuntime()
    {
        if (loggedMissingEquipmentRuntime)
            return;

        Debug.LogWarning($"{nameof(EquipmentStatsBinder)} requires a PlayerEquipmentRuntime reference.", this);
        loggedMissingEquipmentRuntime = true;
    }

    private void LogMissingCharacterStats()
    {
        if (loggedMissingCharacterStats)
            return;

        Debug.LogWarning($"{nameof(EquipmentStatsBinder)} requires a CharacterStats reference.", this);
        loggedMissingCharacterStats = true;
    }
}
