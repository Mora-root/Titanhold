using System;
using System.Collections;
using Titanhold.Run;
using Titanhold.Session;
using Titanhold.UI.Common;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Titanhold.UI.Run
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RunChapterCompletionView))]
    public sealed class RunChapterCompletionController :
        MonoBehaviour,
        IEscapePriorityWindow
    {
        [SerializeField]
        private RunChapterCompletionRuntime completionRuntime;
        [SerializeField]
        private RunChapterExitPortalSpawner exitPortalSpawner;
        [SerializeField] private RunChapterCompletionView view;
        [SerializeField] private PlayerInput[] controlledInputs =
            Array.Empty<PlayerInput>();
        [SerializeField] private string hubSceneName = "HubScene";

        private RunChapterExitPortalRequest pendingRequest;
        private bool controlsSuppressed;
        private bool[] previousInputStates = Array.Empty<bool>();
        private bool transitionInProgress;

        public bool IsOpen => view != null && view.IsVisible;
        public bool HasRequiredReferences =>
            completionRuntime != null &&
            exitPortalSpawner != null &&
            view != null &&
            view.IsConfigured &&
            ValidateControlledInputs();
        public RunChapterCompletionRuntime CompletionRuntime =>
            completionRuntime;
        public RunChapterExitPortalSpawner ExitPortalSpawner =>
            exitPortalSpawner;
        public RunChapterCompletionView View => view;
        public string HubSceneName => hubSceneName;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            RunChapterCompletionRuntime configuredCompletionRuntime,
            RunChapterExitPortalSpawner configuredExitPortalSpawner,
            RunChapterCompletionView configuredView,
            PlayerInput[] configuredControlledInputs,
            string configuredHubSceneName)
        {
            completionRuntime = configuredCompletionRuntime;
            exitPortalSpawner = configuredExitPortalSpawner;
            view = configuredView;
            controlledInputs = configuredControlledInputs ??
                Array.Empty<PlayerInput>();
            hubSceneName = configuredHubSceneName;
        }
#endif

        private void Awake()
        {
            view ??= GetComponent<RunChapterCompletionView>();
        }

        private void OnEnable()
        {
            if (view != null)
            {
                view.ConfirmationCancelled += HandleConfirmationCancelled;
                view.ConfirmationAccepted += HandleConfirmationAccepted;
                view.ReturnToHubRequested += HandleReturnToHubRequested;
                view.ShowHidden();
            }

            if (exitPortalSpawner != null)
                exitPortalSpawner.ExitRequested += HandleExitRequested;
        }

        private void OnDisable()
        {
            if (view != null)
            {
                view.ConfirmationCancelled -= HandleConfirmationCancelled;
                view.ConfirmationAccepted -= HandleConfirmationAccepted;
                view.ReturnToHubRequested -= HandleReturnToHubRequested;
            }

            if (exitPortalSpawner != null)
                exitPortalSpawner.ExitRequested -= HandleExitRequested;

            RestoreGameplayControls();
        }

        private void Update()
        {
            if (view != null &&
                view.Mode == RunChapterCompletionViewMode.Confirmation &&
                Input.GetKeyDown(KeyCode.Escape))
            {
                HandleConfirmationCancelled();
            }
        }

        public bool TryOpenConfirmation(
            RunChapterExitPortalRequest request)
        {
            if (!HasRequiredReferences ||
                transitionInProgress ||
                !request.IsValid ||
                view.Mode == RunChapterCompletionViewMode.Completed)
            {
                return false;
            }

            RunChapterFlowPresentationSnapshot snapshot =
                completionRuntime.ChapterFlowRuntime
                    .CapturePresentationSnapshot();
            if (!snapshot.IsExitPortalAvailable ||
                snapshot.ChapterNumber != request.ChapterNumber ||
                !string.Equals(
                    completionRuntime.ChapterFlowRuntime.RunId,
                    request.RunSessionId,
                    StringComparison.Ordinal))
            {
                return false;
            }

            pendingRequest = request;
            SuppressGameplayControls();
            view.ShowConfirmation();
            return true;
        }

        private void HandleExitRequested(
            RunChapterExitPortalRequest request)
        {
            TryOpenConfirmation(request);
        }

        private void HandleConfirmationCancelled()
        {
            if (transitionInProgress ||
                view == null ||
                view.Mode != RunChapterCompletionViewMode.Confirmation)
            {
                return;
            }

            pendingRequest = default;
            view.ShowHidden();
            RestoreGameplayControls();
        }

        private void HandleConfirmationAccepted()
        {
            if (!pendingRequest.IsValid ||
                view == null ||
                view.Mode != RunChapterCompletionViewMode.Confirmation)
            {
                return;
            }

            view.SetConfirmationInteractable(false);
            RunChapterCompletionCommand command = new(
                pendingRequest.EventId,
                pendingRequest.RunSessionId,
                pendingRequest.ParticipantId,
                pendingRequest.ChapterNumber);
            RunChapterCompletionResult result =
                completionRuntime.TryConfirm(command);
            if (!result.Success)
            {
                view.SetConfirmationInteractable(true);
                view.SetStatus(
                    "Не удалось завершить забег. Попробуйте ещё раз.");
                Debug.LogError(
                    $"Chapter completion failed: {result.Error}. " +
                    result.Detail,
                    this);
                return;
            }

            pendingRequest = default;
            view.ShowCompleted(
                result.Reward.CharacterExperience,
                result.Reward.Crystals);
        }

        private void HandleReturnToHubRequested()
        {
            if (transitionInProgress ||
                view == null ||
                view.Mode != RunChapterCompletionViewMode.Completed)
            {
                return;
            }

            RunChapterCompletionResult prepared =
                completionRuntime.TryPrepareHubTransition();
            if (!prepared.Success)
            {
                view.SetStatus(
                    "Не удалось подготовить переход в хаб. " +
                    "Попробуйте ещё раз.");
                Debug.LogError(
                    $"Hub transition preparation failed: " +
                    $"{prepared.Error}. {prepared.Detail}",
                    this);
                return;
            }

            transitionInProgress = true;
            view.SetReturnToHubInteractable(false);
            view.SetStatus(string.Empty);
            StartCoroutine(LoadHubScene(prepared.RunSessionId));
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
                Debug.LogError(
                    $"Could not begin Hub scene load: {exception}",
                    this);
            }

            if (operation != null)
            {
                yield return operation;
                yield break;
            }

            if (!completionRuntime.TryCancelHubTransition(runSessionId))
            {
                Debug.LogError(
                    "Could not cancel failed chapter Hub transition.",
                    this);
            }

            transitionInProgress = false;
            view.SetReturnToHubInteractable(true);
            view.SetStatus(
                "Не удалось загрузить хаб. Попробуйте ещё раз.");
        }

        private void SuppressGameplayControls()
        {
            if (controlsSuppressed)
                return;

            controlsSuppressed = true;
            previousInputStates = new bool[controlledInputs.Length];
            for (int i = 0; i < controlledInputs.Length; i++)
            {
                PlayerInput input = controlledInputs[i];
                if (input == null)
                    continue;

                previousInputStates[i] = input.GameplayInputEnabled;
                input.SetGameplayInputEnabled(false);
            }
        }

        private void RestoreGameplayControls()
        {
            if (!controlsSuppressed)
                return;

            controlsSuppressed = false;
            if (previousInputStates.Length == controlledInputs.Length)
            {
                for (int i = 0; i < controlledInputs.Length; i++)
                {
                    if (controlledInputs[i] != null)
                    {
                        controlledInputs[i].SetGameplayInputEnabled(
                            previousInputStates[i]);
                    }
                }
            }

            previousInputStates = Array.Empty<bool>();
        }

        private bool ValidateControlledInputs()
        {
            if (controlledInputs == null || controlledInputs.Length == 0)
                return false;

            for (int i = 0; i < controlledInputs.Length; i++)
            {
                if (controlledInputs[i] == null)
                    return false;
            }

            return true;
        }
    }
}
