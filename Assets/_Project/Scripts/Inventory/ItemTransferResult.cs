public readonly struct ItemTransferResult
{
    public ItemTransferResult(bool success, ItemTransferError error, int movedAmount, string message)
    {
        Success = success;
        Error = error;
        MovedAmount = movedAmount;
        Message = message;
    }

    public bool Success { get; }
    public ItemTransferError Error { get; }
    public int MovedAmount { get; }
    public string Message { get; }

    public static ItemTransferResult Succeeded(int movedAmount, string message = null)
    {
        return new ItemTransferResult(true, ItemTransferError.None, movedAmount, message);
    }

    public static ItemTransferResult Failed(ItemTransferError error, string message = null)
    {
        return new ItemTransferResult(false, error, 0, message);
    }
}
