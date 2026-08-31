public enum WorldLootEmissionError
{
    None,
    EmptyDrops,
    InvalidDrop,
    MissingItemPickupPrefab,
    MissingGoldPickupPrefab,
    InvalidGoldPickupPrefab
}

public readonly struct WorldLootEmissionResult
{
    private WorldLootEmissionResult(
        bool success,
        WorldLootEmissionError error,
        int emittedDropCount)
    {
        Success = success;
        Error = error;
        EmittedDropCount = emittedDropCount;
    }

    public bool Success { get; }
    public WorldLootEmissionError Error { get; }
    public int EmittedDropCount { get; }

    public static WorldLootEmissionResult Succeeded(int emittedDropCount)
    {
        return new WorldLootEmissionResult(
            true,
            WorldLootEmissionError.None,
            emittedDropCount);
    }

    public static WorldLootEmissionResult Failed(
        WorldLootEmissionError error)
    {
        return new WorldLootEmissionResult(false, error, 0);
    }
}
