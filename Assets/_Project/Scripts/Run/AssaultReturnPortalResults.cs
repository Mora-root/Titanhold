using Titanhold.Combat;

namespace Titanhold.Run
{
    public enum AssaultReturnPortalError
    {
        None,
        NotInitialized,
        InvalidEntrant,
        EntrantNotRegistered,
        InvalidExpectedRound,
        StalePortal,
        InvalidPhase,
        TransitionRejected
    }

    public readonly struct AssaultReturnPortalResult
    {
        private AssaultReturnPortalResult(
            bool success,
            AssaultReturnPortalError error,
            CombatActorReference entrant,
            int expectedRound,
            AssaultArenaTransitionResult transitionResult)
        {
            Success = success;
            Error = error;
            Entrant = entrant;
            ExpectedRound = expectedRound;
            TransitionResult = transitionResult;
        }

        public bool Success { get; }
        public AssaultReturnPortalError Error { get; }
        public CombatActorReference Entrant { get; }
        public int ExpectedRound { get; }
        public AssaultArenaTransitionResult TransitionResult { get; }

        public static AssaultReturnPortalResult Succeeded(
            CombatActorReference entrant,
            int expectedRound,
            AssaultArenaTransitionResult transitionResult)
        {
            return new AssaultReturnPortalResult(
                true,
                AssaultReturnPortalError.None,
                entrant,
                expectedRound,
                transitionResult);
        }

        public static AssaultReturnPortalResult Failed(
            AssaultReturnPortalError error,
            CombatActorReference entrant = default,
            int expectedRound = 0,
            AssaultArenaTransitionResult transitionResult = default)
        {
            return new AssaultReturnPortalResult(
                false,
                error,
                entrant,
                expectedRound,
                transitionResult);
        }
    }
}
