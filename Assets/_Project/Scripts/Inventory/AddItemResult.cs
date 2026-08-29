public readonly struct AddItemResult
{
    public AddItemResult(int addedAmount, int remainingAmount)
    {
        AddedAmount = addedAmount;
        RemainingAmount = remainingAmount;
    }

    public int AddedAmount { get; }
    public int RemainingAmount { get; }
    public bool AddedAnything => AddedAmount > 0;
    public bool FullyAdded => RemainingAmount == 0;
}
