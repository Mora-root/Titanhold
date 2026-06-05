using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class ItemContainer
{
    private const int DefaultSectionCapacity = 24;

    [SerializeField] private ItemContainerSection[] sections;

    public ItemContainer()
        : this(DefaultSectionCapacity)
    {
    }

    public ItemContainer(int defaultSectionCapacity)
        : this(null, defaultSectionCapacity)
    {
    }

    public ItemContainer(IReadOnlyDictionary<ItemCategory, int> sectionCapacities, int fallbackCapacity = DefaultSectionCapacity)
    {
        ItemCategory[] categories = (ItemCategory[])Enum.GetValues(typeof(ItemCategory));
        sections = new ItemContainerSection[categories.Length];

        for (int i = 0; i < categories.Length; i++)
        {
            ItemCategory category = categories[i];
            int capacity = fallbackCapacity;

            if (sectionCapacities != null && sectionCapacities.TryGetValue(category, out int configuredCapacity))
                capacity = configuredCapacity;

            sections[i] = new ItemContainerSection(category, capacity);
        }
    }

    public IReadOnlyList<ItemContainerSection> Sections => sections;

    public AddItemResult TryAdd(ItemDefinition definition, int amount)
    {
        if (definition == null)
            return new AddItemResult(0, Math.Max(0, amount));

        if (amount <= 0)
            return new AddItemResult(0, Math.Max(0, amount));

        ItemContainerSection section = GetSection(definition.Category);
        return section != null
            ? section.TryAdd(definition, amount)
            : new AddItemResult(0, amount);
    }

    public ItemContainerSection GetSection(ItemCategory category)
    {
        EnsureSections();

        foreach (ItemContainerSection section in sections)
        {
            if (section != null && section.Category == category)
                return section;
        }

        return null;
    }

    public ItemSlot GetSlot(ItemCategory category, int index)
    {
        ItemContainerSection section = GetSection(category);
        return section != null ? section.GetSlot(index) : null;
    }

    public int CountFreeSlots(ItemCategory category)
    {
        ItemContainerSection section = GetSection(category);
        return section != null ? section.CountFreeSlots() : 0;
    }

    public int CountOccupiedSlots(ItemCategory category)
    {
        ItemContainerSection section = GetSection(category);
        return section != null ? section.CountOccupiedSlots() : 0;
    }

    public bool Move(ItemCategory fromCategory, int fromIndex, ItemCategory toCategory, int toIndex)
    {
        ItemContainerSection sourceSection = GetSection(fromCategory);
        ItemContainerSection targetSection = GetSection(toCategory);

        if (sourceSection == null || targetSection == null)
            return false;

        ItemSlot source = sourceSection.GetSlot(fromIndex);
        ItemSlot target = targetSection.GetSlot(toIndex);

        if (source == null || target == null)
            return false;

        return sourceSection.MoveTo(source, target, sourceSection, targetSection);
    }

    private void EnsureSections()
    {
        ItemCategory[] categories = (ItemCategory[])Enum.GetValues(typeof(ItemCategory));

        if (HasAllSections(categories))
            return;

        ItemContainerSection[] currentSections = sections ?? Array.Empty<ItemContainerSection>();
        ItemContainerSection[] rebuiltSections = new ItemContainerSection[categories.Length];

        for (int i = 0; i < categories.Length; i++)
        {
            ItemCategory category = categories[i];
            ItemContainerSection existingSection = FindSection(currentSections, category);
            rebuiltSections[i] = existingSection ?? new ItemContainerSection(category, DefaultSectionCapacity);
        }

        sections = rebuiltSections;
    }

    private bool HasAllSections(ItemCategory[] categories)
    {
        if (categories == null || sections == null || sections.Length != categories.Length)
            return false;

        foreach (ItemCategory category in categories)
        {
            if (FindSection(sections, category) == null)
                return false;
        }

        return true;
    }

    private ItemContainerSection FindSection(ItemContainerSection[] sourceSections, ItemCategory category)
    {
        if (sourceSections == null)
            return null;

        foreach (ItemContainerSection section in sourceSections)
        {
            if (section != null && section.Category == category)
                return section;
        }

        return null;
    }
}
