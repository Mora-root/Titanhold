using System;

namespace Titanhold.Progression
{
    public enum CrystalWalletError
    {
        None,
        InvalidAmount,
        BalanceOverflow,
        InsufficientCrystals
    }

    public readonly struct CrystalWalletResult
    {
        private CrystalWalletResult(
            bool success,
            CrystalWalletError error,
            int previousAmount,
            int currentAmount)
        {
            Success = success;
            Error = error;
            PreviousAmount = previousAmount;
            CurrentAmount = currentAmount;
        }

        public bool Success { get; }
        public CrystalWalletError Error { get; }
        public int PreviousAmount { get; }
        public int CurrentAmount { get; }

        public static CrystalWalletResult Succeeded(
            int previousAmount,
            int currentAmount)
        {
            return new CrystalWalletResult(
                true,
                CrystalWalletError.None,
                previousAmount,
                currentAmount);
        }

        public static CrystalWalletResult Failed(
            CrystalWalletError error,
            int currentAmount)
        {
            return new CrystalWalletResult(
                false,
                error,
                currentAmount,
                currentAmount);
        }
    }

    public sealed class AccountCrystalWallet
    {
        public int Amount { get; private set; }

        public event Action<int> AmountChanged;

        public CrystalWalletResult TryAdd(int amount)
        {
            if (amount <= 0)
            {
                return CrystalWalletResult.Failed(
                    CrystalWalletError.InvalidAmount,
                    Amount);
            }

            long updated = (long)Amount + amount;
            if (updated > int.MaxValue)
            {
                return CrystalWalletResult.Failed(
                    CrystalWalletError.BalanceOverflow,
                    Amount);
            }

            return SetAmount((int)updated);
        }

        public CrystalWalletResult TrySpend(int amount)
        {
            if (amount <= 0)
            {
                return CrystalWalletResult.Failed(
                    CrystalWalletError.InvalidAmount,
                    Amount);
            }

            if (Amount < amount)
            {
                return CrystalWalletResult.Failed(
                    CrystalWalletError.InsufficientCrystals,
                    Amount);
            }

            return SetAmount(Amount - amount);
        }

        public CrystalWalletResult TryRestore(int amount)
        {
            if (amount < 0)
            {
                return CrystalWalletResult.Failed(
                    CrystalWalletError.InvalidAmount,
                    Amount);
            }

            return SetAmount(amount);
        }

        private CrystalWalletResult SetAmount(int amount)
        {
            int previous = Amount;
            Amount = amount;
            if (previous != Amount)
                AmountChanged?.Invoke(Amount);

            return CrystalWalletResult.Succeeded(previous, Amount);
        }
    }
}
