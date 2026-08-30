namespace Titanhold.Run
{
    public enum AssaultArenaTransitionError
    {
        None,
        InvalidPhase,
        MissingRuntime,
        MissingWaveSpawner,
        MissingGateway,
        MissingPlayer,
        MissingTargetRegistry,
        MissingPlayerTarget,
        TargetRegistrationRejected,
        GatewayRejected,
        WaveRejected,
        FlowRejected
    }

    public readonly struct AssaultArenaTransitionResult
    {
        private AssaultArenaTransitionResult(
            bool success,
            AssaultArenaTransitionError error,
            AssaultArenaTravelResult travelResult,
            AssaultWaveStartResult waveResult,
            RunFlowTransitionResult flowResult)
        {
            Success = success;
            Error = error;
            TravelResult = travelResult;
            WaveResult = waveResult;
            FlowResult = flowResult;
        }

        public bool Success { get; }
        public AssaultArenaTransitionError Error { get; }
        public AssaultArenaTravelResult TravelResult { get; }
        public AssaultWaveStartResult WaveResult { get; }
        public RunFlowTransitionResult FlowResult { get; }

        public static AssaultArenaTransitionResult Succeeded(
            AssaultArenaTravelResult travelResult,
            AssaultWaveStartResult waveResult = default,
            RunFlowTransitionResult flowResult = default)
        {
            return new AssaultArenaTransitionResult(
                true,
                AssaultArenaTransitionError.None,
                travelResult,
                waveResult,
                flowResult);
        }

        public static AssaultArenaTransitionResult Failed(
            AssaultArenaTransitionError error,
            AssaultArenaTravelResult travelResult = default,
            AssaultWaveStartResult waveResult = default,
            RunFlowTransitionResult flowResult = default)
        {
            return new AssaultArenaTransitionResult(
                false,
                error,
                travelResult,
                waveResult,
                flowResult);
        }
    }
}
