public readonly struct LootDropResult
{
    private LootDropResult(LootDropKind kind, ItemStack stack, int goldAmount)
    {
        Kind = kind;
        Stack = stack;
        GoldAmount = goldAmount;
    }

    public LootDropKind Kind { get; }
    public ItemStack Stack { get; }
    public int GoldAmount { get; }

    public static LootDropResult Item(ItemStack stack)
    {
        return new LootDropResult(LootDropKind.Item, stack, 0);
    }

    public static LootDropResult Gold(int amount)
    {
        return new LootDropResult(LootDropKind.Gold, null, amount);
    }
}
