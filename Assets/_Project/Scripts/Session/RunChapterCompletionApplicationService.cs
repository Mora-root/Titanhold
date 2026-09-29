using System;
using System.Collections.Generic;
using Titanhold.Run;

namespace Titanhold.Session
{
    public sealed class RunChapterCompletionApplicationService
    {
        public RunChapterCompletionResult TryConfirm(
            GameSessionRuntime runtime,
            RunChapterFlowService chapterFlow,
            IReadOnlyList<RunSceneParticipantBinding> bindings,
            RunChapterCompletionReward reward,
            RunChapterCompletionCommand command)
        {
            if (!command.IsValid)
            {
                return RunChapterCompletionResult.Failed(
                    RunChapterCompletionError.InvalidCommand);
            }

            RunChapterCompletionResult validation = ValidateCommon(
                runtime,
                chapterFlow,
                reward,
                command.RunSessionId,
                command.ChapterNumber);
            if (!validation.Success)
                return validation;

            RunSessionDescriptor descriptor =
                runtime.GameSession.State.ActiveRun;
            if (!ContainsParticipant(descriptor, command.ParticipantId))
            {
                return RunChapterCompletionResult.Failed(
                    RunChapterCompletionError.ParticipantNotRegistered,
                    command.ParticipantId,
                    command.RunSessionId);
            }

            RunResultSummary expectedSummary = CreateSummary(
                command.RunSessionId,
                reward);
            if (runtime.TryGetSettledRunResult(
                    command.RunSessionId,
                    out RunResultSummary settled))
            {
                return CompleteFromSettlement(
                    runtime,
                    chapterFlow,
                    reward,
                    expectedSummary,
                    settled);
            }

            if (chapterFlow.State.Phase != RunChapterPhase.Reward)
            {
                return RunChapterCompletionResult.Failed(
                    RunChapterCompletionError.InvalidChapterPhase,
                    chapterFlow.State.Phase.ToString(),
                    command.RunSessionId);
            }

            if (runtime.GameSession.State.Phase != GameSessionPhase.Run)
            {
                return RunChapterCompletionResult.Failed(
                    RunChapterCompletionError.InvalidSessionPhase,
                    runtime.GameSession.State.Phase.ToString(),
                    command.RunSessionId);
            }

            if (!RunSceneParticipantBindingResolver.TryResolve(
                    descriptor,
                    bindings,
                    out RunSceneParticipantBinding[] resolved,
                    out string bindingError))
            {
                return RunChapterCompletionResult.Failed(
                    RunChapterCompletionError.InvalidParticipantBinding,
                    bindingError,
                    command.RunSessionId);
            }

            List<CharacterSnapshot> stagedSnapshots =
                new(resolved.Length);
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
                    return RunChapterCompletionResult.Failed(
                        RunChapterCompletionError.CharacterCaptureFailed,
                        $"{binding.CharacterId}: {capture.Error} " +
                        capture.Detail,
                        command.RunSessionId);
                }

                if (!binding.Experience.TryCalculateStateAfterGain(
                        reward.CharacterExperience,
                        out int rewardedLevel,
                        out int rewardedExperience))
                {
                    return RunChapterCompletionResult.Failed(
                        RunChapterCompletionError.CharacterRewardFailed,
                        binding.CharacterId,
                        command.RunSessionId);
                }

                stagedSnapshots.Add(
                    capture.Snapshot.WithProgression(
                        rewardedLevel,
                        rewardedExperience));
            }

            if (reward.Crystals > 0 &&
                !runtime.AccountCrystals.CanAdd(reward.Crystals))
            {
                return RunChapterCompletionResult.Failed(
                    RunChapterCompletionError.CrystalRewardFailed,
                    "The account crystal balance would overflow.",
                    command.RunSessionId);
            }

            if (!runtime.TryStoreCharacterSnapshots(
                    stagedSnapshots,
                    out string snapshotError))
            {
                return RunChapterCompletionResult.Failed(
                    RunChapterCompletionError.SnapshotStoreFailed,
                    snapshotError,
                    command.RunSessionId);
            }

            if (reward.Crystals > 0 &&
                !runtime.AccountCrystals.TryAdd(reward.Crystals).Success)
            {
                return RunChapterCompletionResult.Failed(
                    RunChapterCompletionError.CrystalRewardFailed,
                    runSessionId: command.RunSessionId);
            }

            if (!runtime.TryRecordSettledRunResult(expectedSummary))
            {
                return RunChapterCompletionResult.Failed(
                    RunChapterCompletionError.RewardSettlementFailed,
                    runSessionId: command.RunSessionId);
            }

            RunChapterFlowResult completion =
                chapterFlow.TryCompleteChapter();
            if (!completion.Success)
            {
                return RunChapterCompletionResult.Failed(
                    RunChapterCompletionError.ChapterCompletionFailed,
                    completion.Error.ToString(),
                    command.RunSessionId);
            }

            GameSessionCommandResult conclusion =
                runtime.GameSession.TryConcludeRun(expectedSummary);
            if (!conclusion.Success)
            {
                return RunChapterCompletionResult.Failed(
                    RunChapterCompletionError.SessionConclusionFailed,
                    conclusion.Error.ToString(),
                    command.RunSessionId);
            }

            return RunChapterCompletionResult.Succeeded(
                command.RunSessionId,
                reward,
                expectedSummary);
        }

        public RunChapterCompletionResult TryPrepareHubTransition(
            GameSessionRuntime runtime,
            RunChapterFlowService chapterFlow,
            RunChapterCompletionReward reward,
            string runSessionId)
        {
            RunChapterCompletionResult validation = ValidateCommon(
                runtime,
                chapterFlow,
                reward,
                runSessionId,
                reward.ChapterNumber);
            if (!validation.Success)
                return validation;

            if (chapterFlow.State.Phase != RunChapterPhase.Completed)
            {
                return RunChapterCompletionResult.Failed(
                    RunChapterCompletionError.InvalidChapterPhase,
                    chapterFlow.State.Phase.ToString(),
                    runSessionId);
            }

            RunResultSummary expected = CreateSummary(runSessionId, reward);
            if (!runtime.TryGetSettledRunResult(
                    runSessionId,
                    out RunResultSummary settled) ||
                !Matches(settled, expected))
            {
                return RunChapterCompletionResult.Failed(
                    RunChapterCompletionError.RewardSettlementFailed,
                    runSessionId: runSessionId);
            }

            GameSessionPhase phase = runtime.GameSession.State.Phase;
            if (phase == GameSessionPhase.TransitionToHub)
            {
                return RunChapterCompletionResult.Succeeded(
                    runSessionId,
                    reward,
                    settled,
                    replayed: true);
            }

            if (phase != GameSessionPhase.Run)
            {
                return RunChapterCompletionResult.Failed(
                    RunChapterCompletionError.InvalidSessionPhase,
                    phase.ToString(),
                    runSessionId);
            }

            GameSessionCommandResult conclusion =
                runtime.GameSession.TryConcludeRun(settled);
            return conclusion.Success
                ? RunChapterCompletionResult.Succeeded(
                    runSessionId,
                    reward,
                    settled,
                    replayed: true)
                : RunChapterCompletionResult.Failed(
                    RunChapterCompletionError.SessionConclusionFailed,
                    conclusion.Error.ToString(),
                    runSessionId);
        }

        private RunChapterCompletionResult CompleteFromSettlement(
            GameSessionRuntime runtime,
            RunChapterFlowService chapterFlow,
            RunChapterCompletionReward reward,
            RunResultSummary expected,
            RunResultSummary settled)
        {
            if (!Matches(settled, expected))
            {
                return RunChapterCompletionResult.Failed(
                    RunChapterCompletionError.RewardSettlementFailed,
                    "The settled result does not match the chapter reward.",
                    expected.RunSessionId);
            }

            if (chapterFlow.State.Phase == RunChapterPhase.Reward)
            {
                RunChapterFlowResult completion =
                    chapterFlow.TryCompleteChapter();
                if (!completion.Success)
                {
                    return RunChapterCompletionResult.Failed(
                        RunChapterCompletionError.ChapterCompletionFailed,
                        completion.Error.ToString(),
                        expected.RunSessionId);
                }
            }
            else if (chapterFlow.State.Phase != RunChapterPhase.Completed)
            {
                return RunChapterCompletionResult.Failed(
                    RunChapterCompletionError.InvalidChapterPhase,
                    chapterFlow.State.Phase.ToString(),
                    expected.RunSessionId);
            }

            RunChapterCompletionResult prepared = TryPrepareHubTransition(
                runtime,
                chapterFlow,
                reward,
                expected.RunSessionId);
            if (!prepared.Success)
                return prepared;

            return RunChapterCompletionResult.Succeeded(
                expected.RunSessionId,
                reward,
                settled,
                replayed: true);
        }

        private static RunChapterCompletionResult ValidateCommon(
            GameSessionRuntime runtime,
            RunChapterFlowService chapterFlow,
            RunChapterCompletionReward reward,
            string runSessionId,
            int chapterNumber)
        {
            if (runtime == null)
            {
                return RunChapterCompletionResult.Failed(
                    RunChapterCompletionError.MissingRuntime);
            }

            if (chapterFlow == null)
            {
                return RunChapterCompletionResult.Failed(
                    RunChapterCompletionError.MissingChapterFlow);
            }

            if (!reward.IsValid)
            {
                return RunChapterCompletionResult.Failed(
                    RunChapterCompletionError.MissingReward);
            }

            RunSessionDescriptor descriptor =
                runtime.GameSession.State.ActiveRun;
            if (descriptor == null)
            {
                return RunChapterCompletionResult.Failed(
                    RunChapterCompletionError.MissingActiveRun);
            }

            if (!string.Equals(
                    descriptor.RunSessionId,
                    runSessionId?.Trim(),
                    StringComparison.Ordinal))
            {
                return RunChapterCompletionResult.Failed(
                    RunChapterCompletionError.RunSessionMismatch,
                    runSessionId);
            }

            if (chapterNumber != reward.ChapterNumber ||
                chapterFlow.State.ChapterNumber != reward.ChapterNumber)
            {
                return RunChapterCompletionResult.Failed(
                    RunChapterCompletionError.InvalidChapter,
                    chapterNumber.ToString(),
                    descriptor.RunSessionId);
            }

            return RunChapterCompletionResult.Succeeded(
                descriptor.RunSessionId,
                reward,
                null);
        }

        private static bool ContainsParticipant(
            RunSessionDescriptor descriptor,
            string participantId)
        {
            for (int i = 0; i < descriptor.Participants.Count; i++)
            {
                if (string.Equals(
                        descriptor.Participants[i].PlayerId,
                        participantId,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static RunResultSummary CreateSummary(
            string runSessionId,
            RunChapterCompletionReward reward)
        {
            return new RunResultSummary(
                runSessionId,
                RunOutcome.Victory,
                completedRoundCount: 0,
                characterExperienceAwarded: reward.CharacterExperience,
                crystalsAwarded: reward.Crystals,
                completedChapterNumber: reward.ChapterNumber);
        }

        private static bool Matches(
            RunResultSummary left,
            RunResultSummary right)
        {
            return left != null &&
                   right != null &&
                   string.Equals(
                       left.RunSessionId,
                       right.RunSessionId,
                       StringComparison.Ordinal) &&
                   left.Outcome == right.Outcome &&
                   left.CompletedRoundCount == right.CompletedRoundCount &&
                   left.CharacterExperienceAwarded ==
                       right.CharacterExperienceAwarded &&
                   left.CrystalsAwarded == right.CrystalsAwarded &&
                   left.CompletedChapterNumber ==
                       right.CompletedChapterNumber;
        }
    }
}
