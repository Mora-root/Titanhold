using System;
using System.Collections;
using Titanhold.Run;
using Titanhold.Session;
using Titanhold.UI.Hub;
using UnityEngine;

namespace Titanhold.UI.Run
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HubStartingAbilitySelectionView))]
    public sealed class RunLevelAbilitySelectionController : MonoBehaviour
    {
        [SerializeField] private HubStartingAbilitySelectionView view;
        [SerializeField] private string playerId = "player:local";

        private RunAbilityChoiceService choices;
        private RunLevelAbilitySelectionService levelSelection;
        private RunLevelAbilitySelectionPresenter presenter;
        private HubStartingAbilitySelectionModel activeSelection;
        private PlayerInput[] controlledInputs = Array.Empty<PlayerInput>();
        private bool[] previousInputStates = Array.Empty<bool>();
        private float previousTimeScale = 1f;
        private bool selectionGateHeld;
        private bool servicesSubscribed;

        public bool HasRequiredReferences => view != null;
        public HubStartingAbilitySelectionView View => view;
        public string PlayerId => playerId?.Trim() ?? string.Empty;
        public bool IsPresenting => activeSelection != null;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            HubStartingAbilitySelectionView configuredView,
            string configuredPlayerId)
        {
            view = configuredView;
            playerId = configuredPlayerId;
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

            UnsubscribeServices();
            activeSelection = null;
            ReleaseSelectionGate();
        }

        private IEnumerator Start()
        {
            yield return null;
            TryBindServices();
        }

        private void TryBindServices()
        {
            if (servicesSubscribed || PlayerId.Length == 0)
                return;

            GameSessionRuntimeHost host =
                FindAnyObjectByType<GameSessionRuntimeHost>(
                    FindObjectsInactive.Include);
            if (host == null || !host.IsInitialized ||
                host.AbilityDefinitions == null ||
                !host.AbilityDefinitions.IsValid)
            {
                return;
            }

            GameSessionState session = host.Runtime.GameSession.State;
            if (session.ActiveRun == null ||
                !host.Runtime.TryGetActiveRunAbilityChoices(
                    session.ActiveRun.RunSessionId,
                    out choices) ||
                !host.Runtime.TryGetActiveRunLevelAbilitySelection(
                    session.ActiveRun.RunSessionId,
                    out levelSelection))
            {
                choices = null;
                levelSelection = null;
                return;
            }

            presenter = new RunLevelAbilitySelectionPresenter(
                host.AbilityDefinitions);
            choices.ChoiceOffered += HandleChoiceOffered;
            levelSelection.OfferFailed += HandleOfferFailed;
            servicesSubscribed = true;
            TryPresentPendingChoice();
        }

        private void TryPresentPendingChoice()
        {
            if (choices == null ||
                !choices.TryGetPendingChoice(
                    PlayerId,
                    out RunAbilityChoiceState pending) ||
                !RunLevelAbilitySelectionService.IsRunLevelChoiceId(
                    pending.ChoiceId))
            {
                return;
            }

            Present(pending);
        }

        private void HandleChoiceOffered(RunAbilityChoiceState choice)
        {
            if (choice == null ||
                !string.Equals(
                    choice.PlayerId,
                    PlayerId,
                    StringComparison.Ordinal) ||
                !RunLevelAbilitySelectionService.IsRunLevelChoiceId(
                    choice.ChoiceId))
            {
                return;
            }

            Present(choice);
        }

        private void Present(RunAbilityChoiceState choice)
        {
            HoldSelectionGate();
            if (presenter == null)
            {
                view?.SetStatus("ABILITY CHOICE PRESENTER IS MISSING");
                Debug.LogError(
                    "Run-level ability choice presenter is missing.",
                    this);
                return;
            }

            if (!presenter.TryBuild(
                    choice,
                    out HubStartingAbilitySelectionModel model,
                    out RunLevelAbilityPresentationError error))
            {
                view?.SetStatus($"ABILITY CHOICE ERROR: {error}");
                Debug.LogError(
                    $"Run-level ability choice could not be presented: {error}.",
                    this);
                return;
            }

            view.SetHeading(
                "CHOOSE A NEW ABILITY",
                "Add one ability to this run's loadout.");
            if (!view.TryShow(model))
            {
                view.SetStatus("ABILITY CHOICE UI IS INCOMPLETE");
                Debug.LogError(
                    "Run-level ability choice view references are incomplete.",
                    this);
                return;
            }

            activeSelection = model;
        }

        private void HandleOptionSelected(int optionIndex)
        {
            HubStartingAbilitySelectionModel submitted = activeSelection;
            if (submitted == null)
                return;

            if (optionIndex < 0 || optionIndex >= submitted.Options.Count)
            {
                view.SetStatus("INVALID ABILITY CHOICE");
                return;
            }

            view.SetInteractable(false);
            RunAbilityChoiceResult result = choices.TrySelectAbility(
                PlayerId,
                submitted.ChoiceId,
                submitted.Options[optionIndex].AbilityId);
            if (!result.Success)
            {
                view.SetStatus($"SELECTION REJECTED: {result.Error}");
                view.SetInteractable(true);
                return;
            }

            if (activeSelection != null &&
                !string.Equals(
                    activeSelection.ChoiceId,
                    submitted.ChoiceId,
                    StringComparison.Ordinal))
            {
                return;
            }

            activeSelection = null;
            if (choices.TryGetPendingChoice(
                    PlayerId,
                    out RunAbilityChoiceState next) &&
                RunLevelAbilitySelectionService.IsRunLevelChoiceId(
                    next.ChoiceId))
            {
                Present(next);
                return;
            }

            view.Hide();
            ReleaseSelectionGate();
        }

        private void HandleOfferFailed(
            string failedPlayerId,
            RunLevelAbilitySelectionResult result)
        {
            if (!string.Equals(
                    failedPlayerId,
                    PlayerId,
                    StringComparison.Ordinal))
            {
                return;
            }

            Debug.LogError(
                "Run-level ability offer failed: " +
                $"{result.Error} / {result.ChoiceError}.",
                this);
        }

        private void HoldSelectionGate()
        {
            if (selectionGateHeld)
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

            selectionGateHeld = true;
        }

        private void ReleaseSelectionGate()
        {
            if (!selectionGateHeld)
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
            selectionGateHeld = false;
        }

        private void UnsubscribeServices()
        {
            if (!servicesSubscribed)
                return;

            if (choices != null)
                choices.ChoiceOffered -= HandleChoiceOffered;
            if (levelSelection != null)
                levelSelection.OfferFailed -= HandleOfferFailed;

            choices = null;
            levelSelection = null;
            presenter = null;
            servicesSubscribed = false;
        }
    }
}
