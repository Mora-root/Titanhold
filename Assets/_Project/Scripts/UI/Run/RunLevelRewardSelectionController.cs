using System;
using System.Collections;
using Titanhold.Run;
using Titanhold.Session;
using Titanhold.UI.Common;
using Titanhold.UI.Hub;
using UnityEngine;

namespace Titanhold.UI.Run
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HubStartingAbilitySelectionView))]
    public sealed class RunLevelRewardSelectionController : MonoBehaviour
    {
        [SerializeField] private HubStartingAbilitySelectionView view;
        [SerializeField] private string playerId = "player:local";

        private RunAbilityChoiceService abilityChoices;
        private RunUpgradeChoiceService upgradeChoices;
        private RunLevelRewardSelectionService rewardSelection;
        private RunLevelRewardSelectionPresenter presenter;
        private ChoiceSelectionModel activeSelection;
        private RunLevelRewardKind activeKind;
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
            activeKind = RunLevelRewardKind.None;
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
                !host.AbilityDefinitions.IsValid ||
                host.RunUpgrades == null ||
                !host.RunUpgrades.IsValid)
            {
                return;
            }

            GameSessionState session = host.Runtime.GameSession.State;
            if (session.ActiveRun == null ||
                !host.Runtime.TryGetActiveRunAbilityChoices(
                    session.ActiveRun.RunSessionId,
                    out abilityChoices) ||
                !host.Runtime.TryGetActiveRunUpgradeChoices(
                    session.ActiveRun.RunSessionId,
                    out upgradeChoices) ||
                !host.Runtime.TryGetActiveRunLevelRewardSelection(
                    session.ActiveRun.RunSessionId,
                    out rewardSelection))
            {
                abilityChoices = null;
                upgradeChoices = null;
                rewardSelection = null;
                return;
            }

            presenter = new RunLevelRewardSelectionPresenter(
                host.AbilityDefinitions,
                host.RunUpgrades);
            abilityChoices.ChoiceOffered += HandleAbilityChoiceOffered;
            upgradeChoices.ChoiceOffered += HandleUpgradeChoiceOffered;
            rewardSelection.OfferFailed += HandleOfferFailed;
            servicesSubscribed = true;
            TryPresentPendingChoice();
        }

        private void TryPresentPendingChoice()
        {
            if (abilityChoices != null &&
                abilityChoices.TryGetPendingChoice(
                    PlayerId,
                    out RunAbilityChoiceState abilityChoice) &&
                RunLevelAbilitySelectionService.IsRunLevelChoiceId(
                    abilityChoice.ChoiceId))
            {
                Present(abilityChoice);
                return;
            }

            if (upgradeChoices != null &&
                upgradeChoices.TryGetPendingChoice(
                    PlayerId,
                    out RunUpgradeChoiceState upgradeChoice) &&
                RunLevelUpgradeSelectionService.IsRunLevelChoiceId(
                    upgradeChoice.ChoiceId))
            {
                Present(upgradeChoice);
            }
        }

        private void HandleAbilityChoiceOffered(RunAbilityChoiceState choice)
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

        private void HandleUpgradeChoiceOffered(RunUpgradeChoiceState choice)
        {
            if (choice == null ||
                !string.Equals(
                    choice.PlayerId,
                    PlayerId,
                    StringComparison.Ordinal) ||
                !RunLevelUpgradeSelectionService.IsRunLevelChoiceId(
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
                ReportPresentationFailure(
                    RunLevelRewardPresentationError.PresenterNotFound);
                return;
            }

            if (!presenter.TryBuild(
                    choice,
                    out ChoiceSelectionModel model,
                    out RunLevelRewardPresentationError error))
            {
                ReportPresentationFailure(error);
                return;
            }

            view.SetHeading(
                "CHOOSE A NEW ABILITY",
                "Add one ability to this run's loadout.");
            Show(model, RunLevelRewardKind.Ability);
        }

        private void Present(RunUpgradeChoiceState choice)
        {
            HoldSelectionGate();
            if (presenter == null)
            {
                ReportPresentationFailure(
                    RunLevelRewardPresentationError.PresenterNotFound);
                return;
            }

            if (!presenter.TryBuild(
                    choice,
                    out ChoiceSelectionModel model,
                    out RunLevelRewardPresentationError error))
            {
                ReportPresentationFailure(error);
                return;
            }

            view.SetHeading(
                "CHOOSE AN UPGRADE",
                "Choose one improvement for the rest of this run.");
            Show(model, RunLevelRewardKind.Upgrade);
        }

        private void Show(
            ChoiceSelectionModel model,
            RunLevelRewardKind kind)
        {
            if (!view.TryShow(model))
            {
                view.SetStatus("RUN REWARD UI IS INCOMPLETE");
                Debug.LogError(
                    "Run-level reward choice view references are incomplete.",
                    this);
                return;
            }

            activeSelection = model;
            activeKind = kind;
        }

        private void HandleOptionSelected(int optionIndex)
        {
            ChoiceSelectionModel submitted = activeSelection;
            RunLevelRewardKind submittedKind = activeKind;
            if (submitted == null || submittedKind == RunLevelRewardKind.None)
                return;

            if (optionIndex < 0 || optionIndex >= submitted.Options.Count)
            {
                view.SetStatus("INVALID RUN REWARD CHOICE");
                return;
            }

            view.SetInteractable(false);
            string optionId = submitted.Options[optionIndex].OptionId;
            bool selected;
            string error;
            if (submittedKind == RunLevelRewardKind.Ability)
            {
                RunAbilityChoiceResult result =
                    abilityChoices.TrySelectAbility(
                        PlayerId,
                        submitted.ChoiceId,
                        optionId);
                selected = result.Success;
                error = result.Error.ToString();
            }
            else
            {
                RunUpgradeChoiceResult result =
                    upgradeChoices.TrySelectUpgrade(
                        PlayerId,
                        submitted.ChoiceId,
                        optionId);
                selected = result.Success;
                error = result.Error.ToString();
            }

            if (!selected)
            {
                view.SetStatus($"SELECTION REJECTED: {error}");
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
            activeKind = RunLevelRewardKind.None;
            TryPresentPendingChoice();
            if (activeSelection != null)
                return;

            view.Hide();
            ReleaseSelectionGate();
        }

        private void HandleOfferFailed(
            string failedPlayerId,
            RunLevelRewardSelectionResult result)
        {
            if (!string.Equals(
                    failedPlayerId,
                    PlayerId,
                    StringComparison.Ordinal))
            {
                return;
            }

            Debug.LogError(
                $"Run-level reward offer failed: {result.Error}.",
                this);
        }

        private void ReportPresentationFailure(
            RunLevelRewardPresentationError error)
        {
            view?.SetStatus($"RUN REWARD CHOICE ERROR: {error}");
            Debug.LogError(
                $"Run-level reward choice could not be presented: {error}.",
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
                    input.SetGameplayInputEnabled(previousInputStates[i]);
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

            if (abilityChoices != null)
                abilityChoices.ChoiceOffered -= HandleAbilityChoiceOffered;
            if (upgradeChoices != null)
                upgradeChoices.ChoiceOffered -= HandleUpgradeChoiceOffered;
            if (rewardSelection != null)
                rewardSelection.OfferFailed -= HandleOfferFailed;

            abilityChoices = null;
            upgradeChoices = null;
            rewardSelection = null;
            presenter = null;
            servicesSubscribed = false;
        }
    }
}
