using System;
using Titanhold.Run;
using Titanhold.Session;
using Titanhold.UI.Common;
using UnityEngine;

namespace Titanhold.UI.Run
{
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RunPauseView))]
    public sealed class RunPauseController : MonoBehaviour
    {
        [SerializeField] private RunFlowRuntime runFlowRuntime;
        [SerializeField] private RunPauseView view;
        [SerializeField] private RunSessionExitController sessionExitController;
        [SerializeField] private PlayerInput[] controlledInputs =
            Array.Empty<PlayerInput>();
        [SerializeField] private MonoBehaviour[] escapePriorityWindows =
            Array.Empty<MonoBehaviour>();
        [SerializeField] private bool pauseWorld = true;

        private bool ownsWorldPause;
        private float previousTimeScale = 1f;
        private bool controlsSuppressed;
        private bool[] previousInputEnabledStates = Array.Empty<bool>();
        private bool[] previousWindowEnabledStates = Array.Empty<bool>();

        public bool HasRequiredReferences =>
            runFlowRuntime != null &&
            view != null &&
            sessionExitController != null &&
            ValidateControlledInputs() &&
            ValidateEscapePriorityWindows();
        public RunPauseView View => view;
        public bool PauseWorld => pauseWorld;
        public bool IsPaused => view != null && view.IsVisible;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            RunFlowRuntime configuredRunFlowRuntime,
            RunPauseView configuredView,
            RunSessionExitController configuredSessionExitController,
            PlayerInput[] configuredControlledInputs,
            MonoBehaviour[] configuredEscapePriorityWindows,
            bool configuredPauseWorld)
        {
            runFlowRuntime = configuredRunFlowRuntime;
            view = configuredView;
            sessionExitController = configuredSessionExitController;
            controlledInputs = configuredControlledInputs ??
                Array.Empty<PlayerInput>();
            escapePriorityWindows = configuredEscapePriorityWindows ??
                Array.Empty<MonoBehaviour>();
            pauseWorld = configuredPauseWorld;
        }
#endif

        private void Awake()
        {
            if (view == null)
                view = GetComponent<RunPauseView>();
        }

        private void OnEnable()
        {
            if (view != null)
            {
                view.ResumeRequested += HandleResumeRequested;
                view.ExitRequested += HandleExitRequested;
                view.ExitCancelled += HandleExitCancelled;
                view.ExitConfirmed += HandleExitConfirmed;
                view.Show(RunPauseViewMode.Hidden);
            }

            if (runFlowRuntime != null)
                runFlowRuntime.StateChanged += HandleRunStateChanged;

            if (sessionExitController != null)
            {
                sessionExitController.TransitionInProgressChanged +=
                    HandleTransitionInProgressChanged;
            }
        }

        private void OnDisable()
        {
            if (view != null)
            {
                view.ResumeRequested -= HandleResumeRequested;
                view.ExitRequested -= HandleExitRequested;
                view.ExitCancelled -= HandleExitCancelled;
                view.ExitConfirmed -= HandleExitConfirmed;
            }

            if (runFlowRuntime != null)
                runFlowRuntime.StateChanged -= HandleRunStateChanged;

            if (sessionExitController != null)
            {
                sessionExitController.TransitionInProgressChanged -=
                    HandleTransitionInProgressChanged;
            }

            ClosePauseMenu();
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape))
                return;

            if (!IsPaused)
            {
                TryOpenPauseMenu();
                return;
            }

            if (runFlowRuntime != null &&
                runFlowRuntime.State.Phase == RunPhase.Abandoned)
            {
                return;
            }

            if (view.Mode == RunPauseViewMode.ExitConfirmation)
                view.Show(RunPauseViewMode.Paused);
            else
                ClosePauseMenu();
        }

        private void HandleResumeRequested()
        {
            ResumePauseMenu();
        }

        private void HandleExitRequested()
        {
            if (IsPaused && CanAbandonRun() &&
                sessionExitController.CanConcludeActiveRun)
            {
                view.Show(RunPauseViewMode.ExitConfirmation);
            }
        }

        private void HandleExitCancelled()
        {
            if (IsPaused && CanAbandonRun())
                view.Show(RunPauseViewMode.Paused);
        }

        private void HandleExitConfirmed()
        {
            if (!HasRequiredReferences)
                return;

            RunPhase phase = runFlowRuntime.State.Phase;
            if (phase != RunPhase.Abandoned)
            {
                if (!sessionExitController.CanConcludeActiveRun)
                    return;

                RunFlowTransitionResult result =
                    runFlowRuntime.Service.TryAbandonRun();
                if (!result.Success)
                {
                    Debug.LogWarning(
                        $"Run abandonment command failed: {result.Error}.",
                        this);
                    return;
                }
            }

            view.SetExitInteractable(
                !sessionExitController.TryReturnToHub());
        }

        private void HandleRunStateChanged(RunFlowState state)
        {
            if (state == null || !state.IsTerminal ||
                state.Phase == RunPhase.Abandoned)
            {
                return;
            }

            ClosePauseMenu();
        }

        private void HandleTransitionInProgressChanged(bool inProgress)
        {
            if (!inProgress && IsPaused && runFlowRuntime != null &&
                runFlowRuntime.State.Phase == RunPhase.Abandoned)
            {
                view.SetExitInteractable(true);
            }
        }

        private bool CanOpenPauseMenu()
        {
            if (!HasRequiredReferences || runFlowRuntime.State.IsTerminal)
                return false;

            RunCompletionView completionView =
                sessionExitController.CompletionView;
            return completionView == null || !completionView.IsVisible;
        }

        private bool CanAbandonRun()
        {
            return runFlowRuntime != null &&
                   !runFlowRuntime.State.IsTerminal;
        }

        public bool TryOpenPauseMenu()
        {
            if (IsPaused)
                return true;

            if (!CanOpenPauseMenu() || HasOpenEscapePriorityWindow())
                return false;

            OpenPauseMenu();
            return true;
        }

        public void ResumePauseMenu()
        {
            if (runFlowRuntime == null ||
                runFlowRuntime.State.Phase != RunPhase.Abandoned)
            {
                ClosePauseMenu();
            }
        }

        private void OpenPauseMenu()
        {
            if (IsPaused)
                return;

            SuppressGameplayControls();

            if (pauseWorld)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                ownsWorldPause = true;
            }

            view.SetExitInteractable(
                sessionExitController.CanConcludeActiveRun);
            view.Show(RunPauseViewMode.Paused);
        }

        private void ClosePauseMenu()
        {
            if (view != null)
                view.Show(RunPauseViewMode.Hidden);

            if (ownsWorldPause)
            {
                Time.timeScale = previousTimeScale;
                ownsWorldPause = false;
            }

            RestoreGameplayControls();
        }

        private bool HasOpenEscapePriorityWindow()
        {
            if (escapePriorityWindows == null)
                return false;

            for (int i = 0; i < escapePriorityWindows.Length; i++)
            {
                if (escapePriorityWindows[i] is IEscapePriorityWindow window &&
                    window.IsOpen)
                {
                    return true;
                }
            }

            return false;
        }

        private void SuppressGameplayControls()
        {
            if (controlsSuppressed)
                return;

            controlsSuppressed = true;
            previousInputEnabledStates = controlledInputs != null
                ? new bool[controlledInputs.Length]
                : Array.Empty<bool>();
            if (controlledInputs == null)
                return;

            for (int i = 0; i < controlledInputs.Length; i++)
            {
                PlayerInput input = controlledInputs[i];
                if (input == null)
                    continue;

                previousInputEnabledStates[i] = input.GameplayInputEnabled;
                input.SetGameplayInputEnabled(false);
            }

            if (escapePriorityWindows == null)
                return;

            previousWindowEnabledStates =
                new bool[escapePriorityWindows.Length];
            for (int i = 0; i < escapePriorityWindows.Length; i++)
            {
                MonoBehaviour controller = escapePriorityWindows[i];
                if (controller == null)
                    continue;

                previousWindowEnabledStates[i] = controller.enabled;
                controller.enabled = false;
            }
        }

        private void RestoreGameplayControls()
        {
            if (!controlsSuppressed)
                return;

            controlsSuppressed = false;
            if (controlledInputs != null &&
                previousInputEnabledStates.Length == controlledInputs.Length)
            {
                for (int i = 0; i < controlledInputs.Length; i++)
                {
                    if (controlledInputs[i] != null)
                    {
                        controlledInputs[i].SetGameplayInputEnabled(
                            previousInputEnabledStates[i]);
                    }
                }
            }

            previousInputEnabledStates = Array.Empty<bool>();
            if (escapePriorityWindows == null ||
                previousWindowEnabledStates.Length !=
                    escapePriorityWindows.Length)
            {
                previousWindowEnabledStates = Array.Empty<bool>();
                return;
            }

            for (int i = 0; i < escapePriorityWindows.Length; i++)
            {
                if (escapePriorityWindows[i] != null)
                {
                    escapePriorityWindows[i].enabled =
                        previousWindowEnabledStates[i];
                }
            }

            previousWindowEnabledStates = Array.Empty<bool>();
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

        private bool ValidateEscapePriorityWindows()
        {
            if (escapePriorityWindows == null)
                return false;

            for (int i = 0; i < escapePriorityWindows.Length; i++)
            {
                if (escapePriorityWindows[i] == null ||
                    escapePriorityWindows[i] is not IEscapePriorityWindow)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
