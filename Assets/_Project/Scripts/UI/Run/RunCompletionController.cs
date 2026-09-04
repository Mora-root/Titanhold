using Titanhold.Run;
using UnityEngine;

namespace Titanhold.UI.Run
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RunCompletionView))]
    public sealed class RunCompletionController : MonoBehaviour
    {
        [SerializeField] private RunFlowRuntime runFlowRuntime;
        [SerializeField] private RunCompletionView view;

        private RunCompletionViewMode returnModeAfterConfirmation =
            RunCompletionViewMode.Victory;
        private bool loggedMissingReferences;

        public bool HasRequiredReferences => runFlowRuntime != null && view != null;

        private void Awake()
        {
            if (view == null)
                view = GetComponent<RunCompletionView>();
        }

        private void OnEnable()
        {
            if (!TryResolveReferences())
                return;

            view.CollapseRequested += HandleCollapseRequested;
            view.CompletionRequested += HandleCompletionRequested;
            view.CompletionCancelled += HandleCompletionCancelled;
            view.CompletionConfirmed += HandleCompletionConfirmed;
            runFlowRuntime.StateChanged += HandleStateChanged;
            Synchronize(runFlowRuntime.State);
        }

        private void OnDisable()
        {
            if (view != null)
            {
                view.CollapseRequested -= HandleCollapseRequested;
                view.CompletionRequested -= HandleCompletionRequested;
                view.CompletionCancelled -= HandleCompletionCancelled;
                view.CompletionConfirmed -= HandleCompletionConfirmed;
            }

            if (runFlowRuntime != null)
                runFlowRuntime.StateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(RunFlowState state)
        {
            Synchronize(state);
        }

        private void Synchronize(RunFlowState state)
        {
            if (state == null || view == null)
                return;

            if (state.Phase == RunPhase.Completed)
            {
                view.Show(RunCompletionViewMode.Completed);
                return;
            }

            if (state.Phase == RunPhase.Failed)
            {
                view.Show(RunCompletionViewMode.Defeat);
                return;
            }

            bool isFinalIntermission =
                state.Phase == RunPhase.Intermission &&
                state.CurrentEncounterKind == RunEncounterKind.Boss;
            if (!isFinalIntermission)
            {
                view.Show(RunCompletionViewMode.Hidden);
                return;
            }

            if (!view.IsVisible ||
                view.Mode == RunCompletionViewMode.Completed ||
                view.Mode == RunCompletionViewMode.Defeat)
                view.Show(RunCompletionViewMode.Victory);
        }

        private void HandleCollapseRequested()
        {
            if (IsFinalIntermission())
                view.Show(RunCompletionViewMode.Collapsed);
        }

        private void HandleCompletionRequested()
        {
            if (!IsFinalIntermission())
                return;

            returnModeAfterConfirmation = view.Mode == RunCompletionViewMode.Collapsed
                ? RunCompletionViewMode.Collapsed
                : RunCompletionViewMode.Victory;
            view.Show(RunCompletionViewMode.Confirmation);
        }

        private void HandleCompletionCancelled()
        {
            if (IsFinalIntermission())
                view.Show(returnModeAfterConfirmation);
        }

        private void HandleCompletionConfirmed()
        {
            if (!IsFinalIntermission())
                return;

            RunFlowTransitionResult result = runFlowRuntime.Service.TryCompleteRun();
            if (!result.Success)
            {
                Debug.LogWarning(
                    $"Run completion command failed: {result.Error}.",
                    this);
                Synchronize(runFlowRuntime.State);
            }
        }

        private bool IsFinalIntermission()
        {
            if (runFlowRuntime == null)
                return false;

            RunFlowState state = runFlowRuntime.State;
            return state.Phase == RunPhase.Intermission &&
                   state.CurrentEncounterKind == RunEncounterKind.Boss;
        }

        private bool TryResolveReferences()
        {
            if (view == null)
                view = GetComponent<RunCompletionView>();

            if (HasRequiredReferences)
                return true;

            if (!loggedMissingReferences)
            {
                Debug.LogWarning(
                    $"{nameof(RunCompletionController)} requires RunFlowRuntime and view references.",
                    this);
                loggedMissingReferences = true;
            }

            return false;
        }
    }
}
