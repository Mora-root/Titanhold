using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerInventory : MonoBehaviour
{
    private const int MinSectionCapacity = 1;

    [Header("Section Capacities")]
    [SerializeField, Min(MinSectionCapacity)] private int equipmentCapacity = 40;
    [SerializeField, Min(MinSectionCapacity)] private int consumableCapacity = 40;
    [SerializeField, Min(MinSectionCapacity)] private int trophyCapacity = 60;
    [SerializeField, Min(MinSectionCapacity)] private int craftingCapacity = 60;
    [SerializeField, Min(MinSectionCapacity)] private int questCapacity = 30;
    [SerializeField, Min(MinSectionCapacity)] private int miscCapacity = 20;

    private ItemContainer container;
    private ItemTransferService transferService;

    public event Action Changed;
    public event Action<ItemCategory> SectionChanged;

    public ItemContainer Container
    {
        get
        {
            EnsureInitialized();
            return container;
        }
    }

    private void Awake()
    {
        EnsureInitialized();
    }

    private void OnValidate()
    {
        NormalizeCapacities();
    }

    public void EnsureInitialized()
    {
        if (container != null)
        {
            transferService ??= new ItemTransferService();
            return;
        }

        NormalizeCapacities();
        container = new ItemContainer(CreateSectionCapacities(), 0);
        transferService = new ItemTransferService();
    }

    public AddItemResult TryAdd(ItemDefinition definition, int amount = 1)
    {
        EnsureInitialized();

        AddItemResult result = container.TryAdd(definition, amount);

        if (result.AddedAnything && definition != null)
        {
            Changed?.Invoke();
            SectionChanged?.Invoke(definition.Category);
        }

        return result;
    }

    public AddItemResult TryAdd(ItemStack stack)
    {
        EnsureInitialized();

        AddItemResult result = container.TryAdd(stack);
        ItemDefinition definition = stack != null ? stack.Definition : null;

        if (result.AddedAnything && definition != null)
        {
            Changed?.Invoke();
            SectionChanged?.Invoke(definition.Category);
        }

        return result;
    }

    public AddItemResult TryAddInstance(ItemInstance instance)
    {
        if (instance == null)
            return new AddItemResult(0, 0);

        if (instance.Definition == null)
            return new AddItemResult(0, 0);

        if (instance.Definition.MaxStack > 1)
            return new AddItemResult(0, 1);

        return TryAdd(ItemStack.CreateNonStackable(instance));
    }

    public ItemStack TakeStack(ItemCategory category, int slotIndex)
    {
        EnsureInitialized();

        ItemSlot slot = container.GetSlot(category, slotIndex);
        if (slot == null || slot.IsEmpty)
            return null;

        ItemStack stack = slot.Take();
        Changed?.Invoke();
        SectionChanged?.Invoke(category);
        return stack;
    }

    public bool SetStack(ItemCategory category, int slotIndex, ItemStack stack)
    {
        EnsureInitialized();

        if (stack == null || stack.Definition == null)
            return false;

        if (stack.Definition.Category != category)
            return false;

        ItemSlot slot = container.GetSlot(category, slotIndex);
        if (slot == null || !slot.IsEmpty)
            return false;

        slot.Set(stack);
        Changed?.Invoke();
        SectionChanged?.Invoke(category);
        return true;
    }

    public ItemTransferResult TryTransfer(
        ItemCategory sourceCategory,
        int sourceIndex,
        ItemCategory targetCategory,
        int targetIndex)
    {
        EnsureInitialized();

        ItemSlotAddress source = new ItemSlotAddress(container, sourceCategory, sourceIndex);
        ItemSlotAddress target = new ItemSlotAddress(container, targetCategory, targetIndex);
        ItemTransferResult result = transferService.TryTransfer(source, target);

        if (!result.Success)
            return result;

        Changed?.Invoke();
        SectionChanged?.Invoke(sourceCategory);

        if (targetCategory != sourceCategory)
            SectionChanged?.Invoke(targetCategory);

        return result;
    }

    public ItemContainerSection GetSection(ItemCategory category)
    {
        EnsureInitialized();
        return container.GetSection(category);
    }

    public ItemSlot GetSlot(ItemCategory category, int index)
    {
        EnsureInitialized();
        return container.GetSlot(category, index);
    }

    public int CountFreeSlots(ItemCategory category)
    {
        EnsureInitialized();
        return container.CountFreeSlots(category);
    }

    public int CountOccupiedSlots(ItemCategory category)
    {
        EnsureInitialized();
        return container.CountOccupiedSlots(category);
    }

    private Dictionary<ItemCategory, int> CreateSectionCapacities()
    {
        return new Dictionary<ItemCategory, int>
        {
            [ItemCategory.Equipment] = equipmentCapacity,
            [ItemCategory.Consumable] = consumableCapacity,
            [ItemCategory.Trophy] = trophyCapacity,
            [ItemCategory.Crafting] = craftingCapacity,
            [ItemCategory.Quest] = questCapacity,
            [ItemCategory.Misc] = miscCapacity
        };
    }

    private void NormalizeCapacities()
    {
        equipmentCapacity = Mathf.Max(MinSectionCapacity, equipmentCapacity);
        consumableCapacity = Mathf.Max(MinSectionCapacity, consumableCapacity);
        trophyCapacity = Mathf.Max(MinSectionCapacity, trophyCapacity);
        craftingCapacity = Mathf.Max(MinSectionCapacity, craftingCapacity);
        questCapacity = Mathf.Max(MinSectionCapacity, questCapacity);
        miscCapacity = Mathf.Max(MinSectionCapacity, miscCapacity);
    }
}
