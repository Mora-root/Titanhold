using System;
using UnityEngine;

[Serializable]
public sealed class ItemInstance
{
    [SerializeField] private string instanceId;
    [SerializeField] private ItemDefinition definition;

    public ItemInstance(ItemDefinition definition)
        : this(definition, Guid.NewGuid().ToString("N"))
    {
    }

    public ItemInstance(ItemDefinition definition, string instanceId)
    {
        if (definition == null)
            throw new ArgumentNullException(nameof(definition));

        if (definition.MaxStack > 1)
            throw new InvalidOperationException($"Cannot create an item instance for stackable item '{definition.Id}'.");

        if (string.IsNullOrWhiteSpace(instanceId))
            throw new ArgumentException("InstanceId must be non-empty.", nameof(instanceId));

        this.definition = definition;
        this.instanceId = instanceId;
    }

    public string InstanceId => instanceId;
    public ItemDefinition Definition => definition;
}
