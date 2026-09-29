using Titanhold.Enemies;

namespace Titanhold.Run
{
    public enum RunChapterBossEncounterStartError
    {
        None,
        InvalidTransitionRequest,
        EncounterAlreadyStarted,
        InvalidChapterPhase,
        MissingArenaGateway,
        MissingPlayer,
        MissingParticipant,
        MissingPlayerTarget,
        MissingTargetRegistry,
        MissingBossPrefab,
        MissingBossSpawnPoint,
        InvalidEnemyDefinitions,
        ArenaTravelRejected,
        TargetRegistrationRejected,
        EnemyDefinitionInitializationRejected,
        BossScalingRejected,
        MissingBossTargetProvider,
        BossTargetBindingRejected,
        MissingBossDeathNotifier,
        ChapterFlowRejected
    }

    public readonly struct RunChapterBossEncounterStartResult
    {
        private RunChapterBossEncounterStartResult(
            bool success,
            RunChapterBossEncounterStartError error,
            RunChapterBossTransitionRequest transitionRequest,
            AssaultArenaTravelResult travelResult,
            EnemyDefinitionInitializationResult initializationResult,
            EnemyScalingResult scalingResult,
            RunChapterFlowResult flowResult)
        {
            Success = success;
            Error = error;
            TransitionRequest = transitionRequest;
            TravelResult = travelResult;
            InitializationResult = initializationResult;
            ScalingResult = scalingResult;
            FlowResult = flowResult;
        }

        public bool Success { get; }
        public RunChapterBossEncounterStartError Error { get; }
        public RunChapterBossTransitionRequest TransitionRequest { get; }
        public AssaultArenaTravelResult TravelResult { get; }
        public EnemyDefinitionInitializationResult InitializationResult { get; }
        public EnemyScalingResult ScalingResult { get; }
        public RunChapterFlowResult FlowResult { get; }

        public static RunChapterBossEncounterStartResult Succeeded(
            RunChapterBossTransitionRequest transitionRequest,
            AssaultArenaTravelResult travelResult,
            EnemyDefinitionInitializationResult initializationResult,
            EnemyScalingResult scalingResult,
            RunChapterFlowResult flowResult)
        {
            return new RunChapterBossEncounterStartResult(
                true,
                RunChapterBossEncounterStartError.None,
                transitionRequest,
                travelResult,
                initializationResult,
                scalingResult,
                flowResult);
        }

        public static RunChapterBossEncounterStartResult Failed(
            RunChapterBossEncounterStartError error,
            RunChapterBossTransitionRequest transitionRequest = default,
            AssaultArenaTravelResult travelResult = default,
            EnemyDefinitionInitializationResult initializationResult = default,
            EnemyScalingResult scalingResult = default,
            RunChapterFlowResult flowResult = default)
        {
            return new RunChapterBossEncounterStartResult(
                false,
                error,
                transitionRequest,
                travelResult,
                initializationResult,
                scalingResult,
                flowResult);
        }
    }
}
