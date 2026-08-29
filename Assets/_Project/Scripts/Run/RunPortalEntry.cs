using Titanhold.Combat;

namespace Titanhold.Run
{
    public enum RunPortalEntryError
    {
        None,
        InvalidEntrant,
        InvalidExpectedRound,
        StalePortal,
        RunFlowRejected
    }

    public readonly struct RunPortalEntryCommand
    {
        public RunPortalEntryCommand(CombatActorReference entrant, int expectedRound)
        {
            Entrant = entrant;
            ExpectedRound = expectedRound;
        }

        public CombatActorReference Entrant { get; }
        public int ExpectedRound { get; }
    }

    public readonly struct RunPortalEntryResult
    {
        private RunPortalEntryResult(
            bool success,
            RunPortalEntryError error,
            CombatActorReference entrant,
            int expectedRound,
            RunFlowTransitionResult runFlowResult)
        {
            Success = success;
            Error = error;
            Entrant = entrant;
            ExpectedRound = expectedRound;
            RunFlowResult = runFlowResult;
        }

        public bool Success { get; }
        public RunPortalEntryError Error { get; }
        public CombatActorReference Entrant { get; }
        public int ExpectedRound { get; }
        public RunFlowTransitionResult RunFlowResult { get; }

        public static RunPortalEntryResult Succeeded(
            RunPortalEntryCommand command,
            RunFlowTransitionResult runFlowResult)
        {
            return new RunPortalEntryResult(
                true,
                RunPortalEntryError.None,
                command.Entrant,
                command.ExpectedRound,
                runFlowResult);
        }

        public static RunPortalEntryResult Failed(
            RunPortalEntryError error,
            RunPortalEntryCommand command,
            RunFlowTransitionResult runFlowResult = default)
        {
            return new RunPortalEntryResult(
                false,
                error,
                command.Entrant,
                command.ExpectedRound,
                runFlowResult);
        }
    }
}
