using System.Collections.Generic;
using Titanhold.Run;

namespace Titanhold.Session
{
    public sealed class RunSessionConclusionApplicationService
    {
        public RunSessionConclusionResult TryConclude(
            GameSessionRuntime runtime,
            RunFlowState runState,
            IReadOnlyList<RunSceneParticipantBinding> bindings)
        {
            if (runtime == null)
            {
                return RunSessionConclusionResult.Failed(
                    RunSessionConclusionError.MissingRuntime);
            }

            GameSessionState sessionState = runtime.GameSession.State;
            if (sessionState.Phase != GameSessionPhase.Run)
            {
                return RunSessionConclusionResult.Failed(
                    RunSessionConclusionError.InvalidSessionPhase);
            }

            RunSessionDescriptor descriptor = sessionState.ActiveRun;
            if (descriptor == null)
            {
                return RunSessionConclusionResult.Failed(
                    RunSessionConclusionError.MissingActiveRun);
            }

            if (!TryCreateResultSummary(
                    descriptor,
                    runState,
                    out RunResultSummary summary))
            {
                return RunSessionConclusionResult.Failed(
                    RunSessionConclusionError.RunNotTerminal,
                    runState?.Phase.ToString(),
                    descriptor.RunSessionId);
            }

            if (runtime.TryGetSettledRunResult(
                    descriptor.RunSessionId,
                    out RunResultSummary settledResult))
            {
                if (settledResult.Outcome != summary.Outcome ||
                    settledResult.CompletedRoundCount !=
                        summary.CompletedRoundCount)
                {
                    return RunSessionConclusionResult.Failed(
                        RunSessionConclusionError.RewardSettlementFailed,
                        "The settled result does not match the current terminal run state.",
                        descriptor.RunSessionId);
                }

                GameSessionCommandResult retryConclusion =
                    runtime.GameSession.TryConcludeRun(settledResult);
                if (!retryConclusion.Success)
                {
                    return RunSessionConclusionResult.Failed(
                        RunSessionConclusionError.SessionConclusionFailed,
                        retryConclusion.Error.ToString(),
                        descriptor.RunSessionId);
                }

                return RunSessionConclusionResult.Succeeded(
                    descriptor.RunSessionId,
                    settledResult);
            }

            RunConclusionRewardResult rewards =
                runtime.ConclusionRewards.Calculate(
                    summary,
                    descriptor.DifficultyId);
            if (!rewards.Success)
            {
                return RunSessionConclusionResult.Failed(
                    RunSessionConclusionError.RewardCalculationFailed,
                    rewards.Error.ToString(),
                    descriptor.RunSessionId);
            }

            if (!RunSceneParticipantBindingResolver.TryResolve(
                    descriptor,
                    bindings,
                    out RunSceneParticipantBinding[] resolved,
                    out string bindingError))
            {
                return RunSessionConclusionResult.Failed(
                    RunSessionConclusionError.InvalidParticipantBinding,
                    bindingError,
                    descriptor.RunSessionId);
            }

            List<CharacterSnapshot> stagedSnapshots = new(resolved.Length);
            for (int i = 0; i < resolved.Length; i++)
            {
                RunSceneParticipantBinding binding = resolved[i];
                CharacterSnapshotCaptureResult capture =
                    runtime.CharacterSnapshots.TryCapture(
                        binding.CharacterId,
                        binding.Inventory,
                        binding.Equipment,
                        binding.Experience,
                        binding.Gold);
                if (!capture.Success)
                {
                    return RunSessionConclusionResult.Failed(
                        RunSessionConclusionError.CharacterCaptureFailed,
                        $"{binding.CharacterId}: {capture.Error} {capture.Detail}",
                        descriptor.RunSessionId);
                }

                if (!binding.Experience.TryCalculateStateAfterGain(
                        rewards.CharacterExperience,
                        out int rewardedLevel,
                        out int rewardedExperience))
                {
                    return RunSessionConclusionResult.Failed(
                        RunSessionConclusionError.CharacterRewardFailed,
                        binding.CharacterId,
                        descriptor.RunSessionId);
                }

                stagedSnapshots.Add(
                    capture.Snapshot.WithProgression(
                        rewardedLevel,
                        rewardedExperience));
            }

            if (rewards.Crystals > 0 &&
                !runtime.AccountCrystals.CanAdd(rewards.Crystals))
            {
                return RunSessionConclusionResult.Failed(
                    RunSessionConclusionError.CrystalRewardFailed,
                    "The account crystal balance would overflow.",
                    descriptor.RunSessionId);
            }

            if (!runtime.TryStoreCharacterSnapshots(
                    stagedSnapshots,
                    out string snapshotError))
            {
                return RunSessionConclusionResult.Failed(
                    RunSessionConclusionError.SnapshotStoreFailed,
                    snapshotError,
                    descriptor.RunSessionId);
            }

            if (rewards.Crystals > 0 &&
                !runtime.AccountCrystals.TryAdd(rewards.Crystals).Success)
            {
                return RunSessionConclusionResult.Failed(
                    RunSessionConclusionError.CrystalRewardFailed,
                    runSessionId: descriptor.RunSessionId);
            }

            RunResultSummary rewardedSummary = new(
                summary.RunSessionId,
                summary.Outcome,
                summary.CompletedRoundCount,
                rewards.CharacterExperience,
                rewards.Crystals);
            if (!runtime.TryRecordSettledRunResult(rewardedSummary))
            {
                return RunSessionConclusionResult.Failed(
                    RunSessionConclusionError.RewardSettlementFailed,
                    runSessionId: descriptor.RunSessionId);
            }

            GameSessionCommandResult conclusion =
                runtime.GameSession.TryConcludeRun(rewardedSummary);
            if (!conclusion.Success)
            {
                return RunSessionConclusionResult.Failed(
                    RunSessionConclusionError.SessionConclusionFailed,
                    conclusion.Error.ToString(),
                    descriptor.RunSessionId);
            }

            return RunSessionConclusionResult.Succeeded(
                descriptor.RunSessionId,
                rewardedSummary);
        }

        private static bool TryCreateResultSummary(
            RunSessionDescriptor descriptor,
            RunFlowState runState,
            out RunResultSummary summary)
        {
            summary = null;
            if (runState == null)
                return false;

            switch (runState.Phase)
            {
                case RunPhase.Completed:
                    summary = new RunResultSummary(
                        descriptor.RunSessionId,
                        RunOutcome.Victory,
                        runState.RoundNumber);
                    return true;

                case RunPhase.Failed:
                    summary = new RunResultSummary(
                        descriptor.RunSessionId,
                        RunOutcome.Defeat,
                        GetFullyCompletedRoundCount(runState));
                    return true;

                case RunPhase.Abandoned:
                    summary = new RunResultSummary(
                        descriptor.RunSessionId,
                        RunOutcome.Abandoned,
                        GetFullyCompletedRoundCount(runState));
                    return true;

                default:
                    return false;
            }
        }

        private static int GetFullyCompletedRoundCount(
            RunFlowState runState)
        {
            return runState.RoundNumber > 1
                ? runState.RoundNumber - 1
                : 0;
        }
    }
}
