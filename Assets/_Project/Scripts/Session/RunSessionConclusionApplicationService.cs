using System.Collections.Generic;
using Titanhold.Run;

namespace Titanhold.Session
{
    public sealed class RunSessionConclusionApplicationService
    {
        public RunSessionConclusionResult TryConcludeVictory(
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

            if (runState == null || runState.Phase != RunPhase.Completed)
            {
                return RunSessionConclusionResult.Failed(
                    RunSessionConclusionError.RunNotCompleted,
                    runState?.Phase.ToString(),
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

                stagedSnapshots.Add(capture.Snapshot);
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

            RunResultSummary summary = new(
                descriptor.RunSessionId,
                RunOutcome.Victory,
                runState.RoundNumber);
            GameSessionCommandResult conclusion =
                runtime.GameSession.TryConcludeRun(summary);
            if (!conclusion.Success)
            {
                return RunSessionConclusionResult.Failed(
                    RunSessionConclusionError.SessionConclusionFailed,
                    conclusion.Error.ToString(),
                    descriptor.RunSessionId);
            }

            return RunSessionConclusionResult.Succeeded(
                descriptor.RunSessionId);
        }
    }
}
