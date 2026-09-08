using Titanhold.Run;
using Titanhold.Session;
using UnityEngine;

namespace Titanhold.UI.Hub
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HubStartingAbilitySelectionView))]
    public sealed class HubStartingAbilitySelectionController : MonoBehaviour
    {
        [SerializeField] private HubStartingAbilitySelectionView view;
        [SerializeField] private HubRunLaunchController launchController;

        private string activeRunSessionId = string.Empty;
        private HubStartingAbilitySelectionCoordinator coordinator;

        public bool HasRequiredReferences =>
            view != null && launchController != null;
        public HubStartingAbilitySelectionView View => view;
        public HubRunLaunchController LaunchController => launchController;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            HubStartingAbilitySelectionView configuredView,
            HubRunLaunchController configuredLaunchController)
        {
            view = configuredView;
            launchController = configuredLaunchController;
        }
#endif

        private void Awake()
        {
            if (view == null)
                view = GetComponent<HubStartingAbilitySelectionView>();
        }

        private void OnEnable()
        {
            if (view != null)
                view.OptionSelected += HandleOptionSelected;
        }

        private void OnDisable()
        {
            if (view != null)
                view.OptionSelected -= HandleOptionSelected;

            ClearPresentation();
        }

        private void HandleSelectionRequired(
            string runSessionId,
            string playerId)
        {
            ClearPresentation();
            GameSessionRuntimeHost host = launchController?.SessionHost;
            GameSessionRuntime runtime = host?.Runtime;
            if (runtime == null || host.AbilityDefinitions == null ||
                !runtime.TryGetActiveRunAbilityChoices(
                    runSessionId,
                    out RunAbilityChoiceService choices) ||
                !runtime.TryGetActiveRunStartingAbilitySelection(
                    runSessionId,
                    out RunStartingAbilitySelectionService selection))
            {
                RejectPresentation("STARTING CHOICE IS UNAVAILABLE");
                return;
            }

            coordinator = new HubStartingAbilitySelectionCoordinator(
                choices,
                selection,
                new HubStartingAbilitySelectionPresenter(
                    host.AbilityDefinitions));
            HubStartingAbilityCoordinationResult result =
                coordinator.TryPresent(playerId);
            if (!result.Success || !view.TryShow(result.Model))
            {
                string detail = result.Success
                    ? "View references are incomplete."
                    : $"{result.Error} / {result.PresentationError}";
                RejectPresentation(
                    "STARTING CHOICE IS UNAVAILABLE",
                    detail);
                return;
            }

            activeRunSessionId = runSessionId;
        }

        private void HandleOptionSelected(int optionIndex)
        {
            HubStartingAbilitySelectionModel model =
                coordinator?.ActiveSelection;
            if (model == null || activeRunSessionId.Length == 0 ||
                optionIndex < 0 || optionIndex >= model.Options.Count)
            {
                RejectPresentation("INVALID STARTING CHOICE");
                return;
            }

            view.SetInteractable(false);
            HubStartingAbilityCoordinationResult result =
                coordinator.TrySelect(model.Options[optionIndex].AbilityId);
            if (!result.Success)
            {
                view.SetStatus($"SELECTION REJECTED: {result.Error}");
                view.SetInteractable(true);
                return;
            }

            ClearPresentation();
        }

        private void RejectPresentation(string status, string detail = "")
        {
            string reason = detail.Length > 0
                ? $"{status}: {detail}"
                : status;
            bool cancelled = launchController != null &&
                launchController.TryCancelPendingLaunch(status, reason);
            if (!cancelled)
                Debug.LogError(reason, this);
            ClearPresentation();
        }

        private void ClearPresentation()
        {
            coordinator?.Clear();
            coordinator = null;
            activeRunSessionId = string.Empty;
            view?.Hide();
        }
    }
}
