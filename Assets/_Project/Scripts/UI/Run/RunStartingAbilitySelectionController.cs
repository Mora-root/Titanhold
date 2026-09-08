using System;
using System.Collections;
using Titanhold.Run;
using Titanhold.Session;
using Titanhold.UI.Hub;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Titanhold.UI.Run
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HubStartingAbilitySelectionView))]
    public sealed class RunStartingAbilitySelectionController : MonoBehaviour
    {
        [SerializeField] private HubStartingAbilitySelectionView view;
        [SerializeField] private string playerId = "player:local";
        [SerializeField] private string hubSceneName = "HubScene";

        private HubStartingAbilitySelectionCoordinator coordinator;
        private GameSessionRuntime runtime;
        private RunStartReadinessService readiness;
        private string runSessionId = string.Empty;
        private PlayerInput[] controlledInputs = Array.Empty<PlayerInput>();
        private bool[] previousInputStates = Array.Empty<bool>();
        private float previousTimeScale = 1f;
        private bool startGateHeld;
        private bool readinessSubscribed;

        public bool HasRequiredReferences => view != null;
        public HubStartingAbilitySelectionView View => view;
        public string PlayerId => playerId ?? string.Empty;
        public string HubSceneName => hubSceneName ?? string.Empty;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            HubStartingAbilitySelectionView configuredView,
            string configuredPlayerId,
            string configuredHubSceneName)
        {
            view = configuredView;
            playerId = configuredPlayerId;
            hubSceneName = configuredHubSceneName;
        }
#endif

        private void Awake()
        {
            if (view == null)
                view = GetComponent<HubStartingAbilitySelectionView>();

            if (HasActiveRunTransition())
                HoldStartGate();
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
        }

        private IEnumerator Start()
        {
            // Let the run entry point restore the participant and bind its live
            // loadout before the player can resolve the pending choice.
            yield return null;
            TryPresentPendingChoice();
        }

        private void OnDestroy()
        {
            UnsubscribeReadiness();
            ReleaseStartGate();
        }

        private void TryPresentPendingChoice()
        {
            GameSessionRuntimeHost host =
                FindAnyObjectByType<GameSessionRuntimeHost>(
                    FindObjectsInactive.Include);

            // Direct SampleScene play remains available for editor iteration.
            if (host == null || !host.IsInitialized)
            {
                view?.Hide();
                ReleaseStartGate();
                return;
            }

            runtime = host.Runtime;
            GameSessionState session = runtime.GameSession.State;
            if (session.ActiveRun == null)
            {
                RejectAndReturnToHub(
                    "STARTING CHOICE IS UNAVAILABLE",
                    "Run scene has no active session descriptor.");
                return;
            }

            runSessionId = session.ActiveRun.RunSessionId;
            if (session.Phase == GameSessionPhase.Run)
            {
                CompleteSelection();
                return;
            }

            if (session.Phase != GameSessionPhase.TransitionToRun)
            {
                RejectAndReturnToHub(
                    "STARTING CHOICE IS UNAVAILABLE",
                    "Run scene did not enter through an active transition.");
                return;
            }

            if (!runtime.TryGetActiveRunAbilityChoices(
                    runSessionId,
                    out RunAbilityChoiceService choices) ||
                !runtime.TryGetActiveRunStartingAbilitySelection(
                    runSessionId,
                    out RunStartingAbilitySelectionService selection) ||
                !runtime.TryGetActiveRunStartReadiness(
                    runSessionId,
                    out readiness) ||
                host.AbilityDefinitions == null ||
                !host.AbilityDefinitions.IsValid)
            {
                RejectAndReturnToHub(
                    "STARTING CHOICE IS UNAVAILABLE",
                    "Run starting services or ability definitions are missing.");
                return;
            }

            if (readiness.IsSealed)
            {
                ActivateRun();
                return;
            }

            readiness.ReadinessSealed += HandleReadinessSealed;
            readinessSubscribed = true;
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
                RejectAndReturnToHub(
                    "STARTING CHOICE IS UNAVAILABLE",
                    detail);
            }
        }

        private void HandleOptionSelected(int optionIndex)
        {
            HubStartingAbilitySelectionModel model =
                coordinator?.ActiveSelection;
            if (model == null ||
                optionIndex < 0 ||
                optionIndex >= model.Options.Count)
            {
                view?.SetStatus("INVALID STARTING CHOICE");
                return;
            }

            view.SetInteractable(false);
            HubStartingAbilityCoordinationResult result =
                coordinator.TrySelect(
                    model.Options[optionIndex].AbilityId);
            if (!result.Success)
            {
                view.SetStatus($"SELECTION REJECTED: {result.Error}");
                view.SetInteractable(true);
                return;
            }

            if (result.SelectionResult.RosterSealed)
            {
                ActivateRun();
                return;
            }

            view.SetStatus("WAITING FOR OTHER PARTICIPANTS");
        }

        private void HandleReadinessSealed()
        {
            ActivateRun();
        }

        private void ActivateRun()
        {
            if (runtime == null || runSessionId.Length == 0)
                return;

            if (runtime.GameSession.State.Phase == GameSessionPhase.Run)
            {
                CompleteSelection();
                return;
            }

            GameSessionCommandResult result =
                runtime.GameSession.TryActivateRun(runSessionId);
            if (!result.Success)
            {
                RejectAndReturnToHub(
                    "RUN COULD NOT START",
                    $"Session activation failed: {result.Error}.");
                return;
            }

            CompleteSelection();
        }

        private void CompleteSelection()
        {
            UnsubscribeReadiness();
            coordinator?.Clear();
            coordinator = null;
            view?.Hide();
            ReleaseStartGate();
        }

        private void RejectAndReturnToHub(string status, string detail)
        {
            view?.SetStatus(status);
            Debug.LogError($"{status}: {detail}", this);
            UnsubscribeReadiness();
            coordinator?.Clear();
            coordinator = null;

            if (runtime != null && runSessionId.Length > 0 &&
                runtime.GameSession.State.Phase ==
                    GameSessionPhase.TransitionToRun)
            {
                runtime.GameSession.TryCancelRunTransition(runSessionId);
            }

            ReleaseStartGate();
            if (!string.IsNullOrWhiteSpace(hubSceneName))
                SceneManager.LoadSceneAsync(hubSceneName, LoadSceneMode.Single);
        }

        private void HoldStartGate()
        {
            if (startGateHeld)
                return;

            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            controlledInputs = FindObjectsByType<PlayerInput>(
                FindObjectsInactive.Include);
            previousInputStates = new bool[controlledInputs.Length];
            for (int i = 0; i < controlledInputs.Length; i++)
            {
                PlayerInput input = controlledInputs[i];
                if (input == null)
                    continue;

                previousInputStates[i] = input.GameplayInputEnabled;
                input.SetGameplayInputEnabled(false);
            }

            startGateHeld = true;
        }

        private static bool HasActiveRunTransition()
        {
            GameSessionRuntimeHost host =
                FindAnyObjectByType<GameSessionRuntimeHost>(
                    FindObjectsInactive.Include);
            return host != null &&
                   host.IsInitialized &&
                   host.Runtime.GameSession.State.Phase ==
                       GameSessionPhase.TransitionToRun;
        }

        private void ReleaseStartGate()
        {
            if (!startGateHeld)
                return;

            for (int i = 0; i < controlledInputs.Length; i++)
            {
                PlayerInput input = controlledInputs[i];
                if (input != null)
                {
                    input.SetGameplayInputEnabled(
                        previousInputStates[i]);
                }
            }

            Time.timeScale = previousTimeScale;
            controlledInputs = Array.Empty<PlayerInput>();
            previousInputStates = Array.Empty<bool>();
            startGateHeld = false;
        }

        private void UnsubscribeReadiness()
        {
            if (readinessSubscribed && readiness != null)
                readiness.ReadinessSealed -= HandleReadinessSealed;

            readinessSubscribed = false;
            readiness = null;
        }
    }
}
