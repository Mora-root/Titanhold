using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class ItemInstance
{
    [SerializeField] private string instanceId;
    [SerializeField] private ItemDefinition definition;
    [SerializeField] private List<StatModifierData> generatedModifiers = new();

    public ItemInstance(ItemDefinition definition)
        : this(definition, Guid.NewGuid().ToString("N"), null)
    {
    }

    public ItemInstance(ItemDefinition definition, string instanceId)
        : this(definition, instanceId, null)
    {
    }

    public ItemInstance(ItemDefinition definition, IEnumerable<StatModifierData> generatedModifiers)
        : this(definition, Guid.NewGuid().ToString("N"), generatedModifiers)
    {
    }

    public ItemInstance(ItemDefinition definition, string instanceId, IEnumerable<StatModifierData> generatedModifiers)
    {
        if (definition == null)
            throw new ArgumentNullException(nameof(definition));

        if (definition.MaxStack > 1)
            throw new InvalidOperationException($"Cannot create an item instance for stackable item '{definition.Id}'.");

        if (string.IsNullOrWhiteSpace(instanceId))
            throw new ArgumentException("InstanceId must be non-empty.", nameof(instanceId));

        this.definition = definition;
        this.instanceId = instanceId;

        if (generatedModifiers != null)
            this.generatedModifiers.AddRange(generatedModifiers);
    }

    public string InstanceId => instanceId;
    public ItemDefinition Definition => definition;
    public IReadOnlyList<StatModifierData> GeneratedModifiers => generatedModifiers;

    public void AddGeneratedModifier(StatModifierData modifier)
    {
        generatedModifiers ??= new List<StatModifierData>();
        generatedModifiers.Add(modifier);
    }

    public void AddGeneratedModifiers(IEnumerable<StatModifierData> modifiers)
    {
        if (modifiers == null)
            return;

        generatedModifiers ??= new List<StatModifierData>();
        generatedModifiers.AddRange(modifiers);
    }
}
