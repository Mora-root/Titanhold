public sealed class ItemTransferService
{
    public ItemTransferResult TryTransfer(ItemSlotAddress source, ItemSlotAddress target)
    {
        if (!source.IsValid)
            return ItemTransferResult.Failed(ItemTransferError.InvalidSource);

        if (!target.IsValid)
            return ItemTransferResult.Failed(ItemTransferError.InvalidTarget);

        if (IsSameSlot(source, target))
            return ItemTransferResult.Failed(ItemTransferError.SameSlot);

        ItemContainerSection sourceSection = source.GetSection();
        ItemContainerSection targetSection = target.GetSection();
        ItemSlot sourceSlot = source.GetSlot();
        ItemSlot targetSlot = target.GetSlot();

        if (sourceSection == null || sourceSlot == null)
            return ItemTransferResult.Failed(ItemTransferError.InvalidSource);

        if (targetSection == null || targetSlot == null)
            return ItemTransferResult.Failed(ItemTransferError.InvalidTarget);

        if (sourceSlot.IsEmpty || sourceSlot.Stack == null)
            return ItemTransferResult.Failed(ItemTransferError.EmptySource);

        ItemStack sourceStack = sourceSlot.Stack;

        if (!targetSection.CanAccept(sourceStack))
            return ItemTransferResult.Failed(ItemTransferError.TargetRejectsSource);

        if (targetSlot.IsEmpty)
            return MoveToEmpty(sourceSlot, targetSlot, sourceStack);

        ItemStack targetStack = targetSlot.Stack;

        if (AreSameStackable(sourceStack, targetStack))
            return MergeStackable(sourceSlot, targetSlot, sourceStack, targetStack);

        if (!sourceSection.CanAccept(targetStack))
            return ItemTransferResult.Failed(ItemTransferError.SourceRejectsTarget);

        return Swap(sourceSlot, targetSlot, sourceStack);
    }

    private static ItemTransferResult MoveToEmpty(ItemSlot sourceSlot, ItemSlot targetSlot, ItemStack sourceStack)
    {
        int movedAmount = sourceStack.Amount;
        targetSlot.Set(sourceSlot.Take());
        return ItemTransferResult.Succeeded(movedAmount);
    }

    private static ItemTransferResult MergeStackable(
        ItemSlot sourceSlot,
        ItemSlot targetSlot,
        ItemStack sourceStack,
        ItemStack targetStack)
    {
        if (targetStack.IsFull || targetStack.FreeAmount <= 0)
            return ItemTransferResult.Failed(ItemTransferError.CannotMerge);

        int sourceAmountBefore = sourceStack.Amount;
        int remaining = targetStack.AddAmount(sourceAmountBefore);
        int movedAmount = sourceAmountBefore - remaining;

        if (movedAmount <= 0)
            return ItemTransferResult.Failed(ItemTransferError.CannotMerge);

        sourceStack.RemoveAmount(movedAmount);

        if (sourceStack.Amount <= 0)
            sourceSlot.Clear();

        return ItemTransferResult.Succeeded(movedAmount);
    }

    private static ItemTransferResult Swap(ItemSlot sourceSlot, ItemSlot targetSlot, ItemStack sourceStack)
    {
        int movedAmount = sourceStack.Amount;
        ItemStack takenSource = sourceSlot.Take();
        ItemStack takenTarget = targetSlot.Take();

        sourceSlot.Set(takenTarget);
        targetSlot.Set(takenSource);
        return ItemTransferResult.Succeeded(movedAmount);
    }

    private static bool IsSameSlot(ItemSlotAddress source, ItemSlotAddress target)
    {
        return ReferenceEquals(source.Container, target.Container) &&
               source.Category == target.Category &&
               source.SlotIndex == target.SlotIndex;
    }

    private static bool AreSameStackable(ItemStack first, ItemStack second)
    {
        if (first == null || second == null)
            return false;

        if (first.Definition == null || second.Definition == null)
            return false;

        if (first.Instance != null || second.Instance != null)
            return false;

        if (first.Definition.MaxStack <= 1 || second.Definition.MaxStack <= 1)
            return false;

        if (ReferenceEquals(first.Definition, second.Definition))
            return true;

        return !string.IsNullOrWhiteSpace(first.Definition.Id) &&
               first.Definition.Id == second.Definition.Id;
    }
}
