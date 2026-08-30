namespace Titanhold.Run
{
    public enum AssaultArenaTravelError
    {
        None,
        MissingActor,
        MissingDestination,
        AlreadyOccupied,
        NotOccupied,
        ActorMismatch,
        DestinationOutsideNavMesh,
        NavMeshWarpRejected
    }

    public readonly struct AssaultArenaTravelResult
    {
        private AssaultArenaTravelResult(
            bool success,
            AssaultArenaTravelError error)
        {
            Success = success;
            Error = error;
        }

        public bool Success { get; }
        public AssaultArenaTravelError Error { get; }

        public static AssaultArenaTravelResult Succeeded()
        {
            return new AssaultArenaTravelResult(
                true,
                AssaultArenaTravelError.None);
        }

        public static AssaultArenaTravelResult Failed(
            AssaultArenaTravelError error)
        {
            return new AssaultArenaTravelResult(false, error);
        }
    }
}
