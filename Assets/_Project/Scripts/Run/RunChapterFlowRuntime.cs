using System;
using Titanhold.Session;
using UnityEngine;

namespace Titanhold.Run
{
    [DisallowMultipleComponent]
    public sealed class RunChapterFlowRuntime : MonoBehaviour
    {
        [SerializeField] private RunChapterFlowDefinition definition;
        [SerializeField] private RunSceneSessionEntryPoint sessionEntryPoint;

        private RunChapterFlowService service;
        private RunChapterProgressApplicationService progressApplication;
        private RunChapterFlowPresentationProjection presentationProjection;
        private RunChapterTransitionParticipantRoster transitionParticipants;
        private RunChapterBossTransitionApplicationService transitionApplication;
        private string runId = string.Empty;

        public RunChapterFlowDefinition Definition => definition;
        public RunSceneSessionEntryPoint SessionEntryPoint => sessionEntryPoint;
        public string RunId
        {
            get
            {
                EnsureInitialized();
                return runId;
            }
        }
        public RunChapterFlowService Service
        {
            get
            {
                EnsureInitialized();
                return service;
            }
        }

        public RunChapterFlowState State => Service.State;
        public RunChapterProgressApplicationService ProgressApplication
        {
            get
            {
                EnsureInitialized();
                return progressApplication;
            }
        }
        public RunChapterBossTransitionApplicationService TransitionApplication
        {
            get
            {
                EnsureInitialized();
                return transitionApplication;
            }
        }

        public RunChapterFlowPresentationSnapshot CapturePresentationSnapshot()
        {
            EnsureInitialized();
            return presentationProjection.Capture();
        }

        public event Action<RunChapterFlowState> StateChanged;
        public event Action<RunChapterBossTransitionRequest>
            BossTransitionRequested;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            RunChapterFlowDefinition configuredDefinition,
            RunSceneSessionEntryPoint configuredSessionEntryPoint)
        {
            definition = configuredDefinition;
            sessionEntryPoint = configuredSessionEntryPoint;
        }
#endif

        private void Awake()
        {
            EnsureInitialized();
        }

        private void Update()
        {
            if (service == null ||
                service.State.Phase != RunChapterPhase.RiftCollapse)
            {
                return;
            }

            transitionApplication.TryAdvanceTime(Time.timeAsDouble);
        }

        private void OnDestroy()
        {
            if (transitionApplication != null)
            {
                transitionApplication.TransitionRequested -=
                    HandleBossTransitionRequested;
            }

            if (service != null)
                service.StateChanged -= HandleStateChanged;
        }

        public RunChapterFlowResult TryAddProgress(float amount)
        {
            return Service.TryAddProgress(amount, Time.timeAsDouble);
        }

        public RunChapterBossTransitionApplicationResult TryEnterBossPortal(
            RunChapterBossPortalEntryCommand command)
        {
            return TransitionApplication.TryEnterPortal(command);
        }

        public bool TryResolveParticipantId(
            GameObject interactor,
            out string participantId)
        {
            EnsureInitialized();
            return transitionParticipants.TryResolveParticipantId(
                interactor,
                out participantId);
        }

        public void EnsureInitialized()
        {
            if (service != null)
                return;

            if (definition == null)
            {
                throw new InvalidOperationException(
                    "Run chapter flow definition is missing.");
            }

            if (!definition.TryCreateConfiguration(
                    out RunChapterFlowConfiguration configuration,
                    out string error))
            {
                throw new InvalidOperationException(error);
            }

            sessionEntryPoint ??=
                FindAnyObjectByType<RunSceneSessionEntryPoint>(
                    FindObjectsInactive.Include);
            if (sessionEntryPoint == null)
            {
                throw new InvalidOperationException(
                    "Run scene session entry point is missing.");
            }

            service = new RunChapterFlowService(configuration);
            progressApplication = new RunChapterProgressApplicationService(
                service);
            presentationProjection =
                new RunChapterFlowPresentationProjection(service.State);
            transitionParticipants =
                new RunChapterTransitionParticipantRoster(
                    sessionEntryPoint.Participants);
            runId = ResolveRunId();
            transitionApplication =
                new RunChapterBossTransitionApplicationService(
                    runId,
                    service,
                    transitionParticipants);
            service.StateChanged += HandleStateChanged;
            transitionApplication.TransitionRequested +=
                HandleBossTransitionRequested;
        }

        private void HandleStateChanged(RunChapterFlowState state)
        {
            StateChanged?.Invoke(state);
        }

        private void HandleBossTransitionRequested(
            RunChapterBossTransitionRequest request)
        {
            BossTransitionRequested?.Invoke(request);
        }

        private string ResolveRunId()
        {
            GameSessionRuntimeHost host =
                FindAnyObjectByType<GameSessionRuntimeHost>(
                    FindObjectsInactive.Include);
            RunSessionDescriptor activeRun =
                host != null && host.IsInitialized
                    ? host.Runtime.GameSession.State.ActiveRun
                    : null;
            if (activeRun != null &&
                !string.IsNullOrWhiteSpace(activeRun.RunSessionId))
            {
                return activeRun.RunSessionId.Trim();
            }

            string sceneName = gameObject.scene.name;
            return $"run:direct:{sceneName}";
        }
    }
}
