using System;
using System.Collections;
using Titanhold.Run;
using Titanhold.UI.Run;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Titanhold.Session
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RunSceneSessionEntryPoint))]
    public sealed class RunSessionExitController : MonoBehaviour
    {
        [SerializeField] private RunFlowRuntime runFlowRuntime;
        [SerializeField] private RunCompletionView completionView;
        [SerializeField] private RunSceneSessionEntryPoint sessionEntryPoint;
        [SerializeField] private string hubSceneName = "HubScene";

        private readonly RunSessionConclusionApplicationService conclusion =
            new();
        private GameSessionRuntimeHost sessionHost;
        private bool transitionInProgress;

        public bool HasRequiredReferences =>
            runFlowRuntime != null &&
            completionView != null &&
            sessionEntryPoint != null;
        public RunFlowRuntime RunFlowRuntime => runFlowRuntime;
        public RunCompletionView CompletionView => completionView;
        public RunSceneSessionEntryPoint SessionEntryPoint => sessionEntryPoint;
        public string HubSceneName => hubSceneName;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            RunFlowRuntime configuredRunFlowRuntime,
            RunCompletionView configuredCompletionView,
            RunSceneSessionEntryPoint configuredSessionEntryPoint,
            string configuredHubSceneName)
        {
            runFlowRuntime = configuredRunFlowRuntime;
            completionView = configuredCompletionView;
            sessionEntryPoint = configuredSessionEntryPoint;
            hubSceneName = configuredHubSceneName;
        }
#endif

        private void Awake()
        {
            if (sessionEntryPoint == null)
                sessionEntryPoint = GetComponent<RunSceneSessionEntryPoint>();
        }

        private void OnEnable()
        {
            if (completionView != null)
                completionView.ReturnToHubRequested += HandleReturnToHubRequested;
        }

        private void OnDisable()
        {
            if (completionView != null)
                completionView.ReturnToHubRequested -= HandleReturnToHubRequested;
        }

        private void Start()
        {
            TryResolveSessionHost();
        }

        private void HandleReturnToHubRequested()
        {
            if (transitionInProgress || !HasRequiredReferences)
                return;

            if (!TryResolveSessionHost())
            {
                Debug.LogError("Persistent session host is unavailable.", this);
                return;
            }

            RunSessionConclusionResult result = conclusion.TryConclude(
                sessionHost.Runtime,
                runFlowRuntime.State,
                sessionEntryPoint.Participants);
            if (!result.Success)
            {
                Debug.LogError(
                    $"Could not conclude run: {result.Error}. {result.Detail}",
                    this);
                return;
            }

            transitionInProgress = true;
            completionView.SetReturnToHubInteractable(false);
            StartCoroutine(LoadHubScene(result.RunSessionId));
        }

        private IEnumerator LoadHubScene(string runSessionId)
        {
            AsyncOperation operation = null;
            try
            {
                operation = SceneManager.LoadSceneAsync(
                    hubSceneName,
                    LoadSceneMode.Single);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Could not begin Hub scene load: {exception}", this);
            }

            if (operation != null)
            {
                yield return operation;
                yield break;
            }

            GameSessionCommandResult cancel =
                sessionHost.Runtime.GameSession.TryCancelHubTransition(
                    runSessionId);
            if (!cancel.Success)
            {
                Debug.LogError(
                    $"Could not cancel failed Hub transition: {cancel.Error}.",
                    this);
            }

            transitionInProgress = false;
            completionView.SetReturnToHubInteractable(true);
        }

        private bool TryResolveSessionHost()
        {
            if (sessionHost != null && sessionHost.IsInitialized)
                return true;

            GameSessionRuntimeHost[] hosts =
                FindObjectsByType<GameSessionRuntimeHost>(
                    FindObjectsInactive.Include);
            for (int i = 0; i < hosts.Length; i++)
            {
                if (hosts[i] == null || !hosts[i].IsInitialized)
                    continue;

                sessionHost = hosts[i];
                return true;
            }

            return false;
        }
    }
}
