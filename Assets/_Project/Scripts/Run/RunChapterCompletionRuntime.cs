using Titanhold.Session;
using UnityEngine;

namespace Titanhold.Run
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RunChapterFlowRuntime))]
    public sealed class RunChapterCompletionRuntime : MonoBehaviour
    {
        [SerializeField] private RunChapterFlowRuntime chapterFlowRuntime;
        [SerializeField] private RunSceneSessionEntryPoint sessionEntryPoint;
        [SerializeField]
        private RunChapterCompletionRewardDefinition rewardDefinition;

        private readonly RunChapterCompletionApplicationService application =
            new();
        private RunChapterCompletionReward reward;
        private bool initialized;

        public RunChapterFlowRuntime ChapterFlowRuntime =>
            chapterFlowRuntime;
        public RunSceneSessionEntryPoint SessionEntryPoint =>
            sessionEntryPoint;
        public RunChapterCompletionRewardDefinition RewardDefinition =>
            rewardDefinition;
        public RunChapterCompletionReward Reward
        {
            get
            {
                EnsureInitialized();
                return reward;
            }
        }
        public bool HasRequiredReferences =>
            chapterFlowRuntime != null &&
            sessionEntryPoint != null &&
            rewardDefinition != null;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            RunChapterFlowRuntime configuredChapterFlowRuntime,
            RunSceneSessionEntryPoint configuredSessionEntryPoint,
            RunChapterCompletionRewardDefinition configuredRewardDefinition)
        {
            chapterFlowRuntime = configuredChapterFlowRuntime;
            sessionEntryPoint = configuredSessionEntryPoint;
            rewardDefinition = configuredRewardDefinition;
            initialized = false;
        }
#endif

        private void Awake()
        {
            EnsureInitialized();
        }

        public RunChapterCompletionResult TryConfirm(
            RunChapterCompletionCommand command)
        {
            EnsureInitialized();
            if (!TryResolveSessionHost(out GameSessionRuntimeHost host))
            {
                return RunChapterCompletionResult.Failed(
                    RunChapterCompletionError.MissingRuntime);
            }

            return application.TryConfirm(
                host.Runtime,
                chapterFlowRuntime.Service,
                sessionEntryPoint.Participants,
                reward,
                command);
        }

        public RunChapterCompletionResult TryPrepareHubTransition()
        {
            EnsureInitialized();
            if (!TryResolveSessionHost(out GameSessionRuntimeHost host))
            {
                return RunChapterCompletionResult.Failed(
                    RunChapterCompletionError.MissingRuntime);
            }

            return application.TryPrepareHubTransition(
                host.Runtime,
                chapterFlowRuntime.Service,
                reward,
                chapterFlowRuntime.RunId);
        }

        public bool TryCancelHubTransition(string runSessionId)
        {
            return TryResolveSessionHost(out GameSessionRuntimeHost host) &&
                   host.Runtime.GameSession
                       .TryCancelHubTransition(runSessionId).Success;
        }

        private void EnsureInitialized()
        {
            if (initialized)
                return;

            chapterFlowRuntime ??= GetComponent<RunChapterFlowRuntime>();
            sessionEntryPoint ??=
                FindAnyObjectByType<RunSceneSessionEntryPoint>(
                    FindObjectsInactive.Include);
            if (!HasRequiredReferences)
            {
                throw new System.InvalidOperationException(
                    "Chapter completion runtime wiring is incomplete.");
            }

            if (!rewardDefinition.TryCreateReward(
                    out reward,
                    out string error))
            {
                throw new System.InvalidOperationException(error);
            }

            if (reward.ChapterNumber !=
                chapterFlowRuntime.State.ChapterNumber)
            {
                throw new System.InvalidOperationException(
                    "Chapter completion reward targets another chapter.");
            }

            initialized = true;
        }

        private static bool TryResolveSessionHost(
            out GameSessionRuntimeHost host)
        {
            GameSessionRuntimeHost[] hosts =
                FindObjectsByType<GameSessionRuntimeHost>(
                    FindObjectsInactive.Include);
            for (int i = 0; i < hosts.Length; i++)
            {
                if (hosts[i] == null || !hosts[i].IsInitialized)
                    continue;

                host = hosts[i];
                return true;
            }

            host = null;
            return false;
        }
    }
}
