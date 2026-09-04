using System;
using UnityEngine;
using UnityEngine.UI;

namespace Titanhold.UI.Run
{
    public enum RunCompletionViewMode
    {
        Hidden,
        Victory,
        Collapsed,
        Confirmation,
        Completed,
        Defeat
    }

    [DisallowMultipleComponent]
    public sealed class RunCompletionView : MonoBehaviour
    {
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private GameObject collapsedPanel;
        [SerializeField] private GameObject confirmationPanel;
        [SerializeField] private GameObject completedPanel;
        [SerializeField] private GameObject defeatPanel;
        [SerializeField] private Button continueCollectingButton;
        [SerializeField] private Button victoryCompleteButton;
        [SerializeField] private Button collapsedCompleteButton;
        [SerializeField] private Button cancelCompletionButton;
        [SerializeField] private Button confirmCompletionButton;
        [SerializeField] private Button returnToHubButton;
        [SerializeField] private Button defeatReturnToHubButton;

        public event Action CollapseRequested;
        public event Action CompletionRequested;
        public event Action CompletionCancelled;
        public event Action CompletionConfirmed;
        public event Action ReturnToHubRequested;

        public RunCompletionViewMode Mode { get; private set; }
        public bool IsVisible => Mode != RunCompletionViewMode.Hidden;

        private void Awake()
        {
            Show(RunCompletionViewMode.Hidden);
        }

        private void OnEnable()
        {
            AddButtonListeners();
        }

        private void OnDisable()
        {
            RemoveButtonListeners();
        }

        public void Show(RunCompletionViewMode mode)
        {
            Mode = mode;
            SetActive(victoryPanel, mode == RunCompletionViewMode.Victory);
            SetActive(collapsedPanel, mode == RunCompletionViewMode.Collapsed);
            SetActive(confirmationPanel, mode == RunCompletionViewMode.Confirmation);
            SetActive(completedPanel, mode == RunCompletionViewMode.Completed);
            SetActive(defeatPanel, mode == RunCompletionViewMode.Defeat);
        }

        public void SetReturnToHubInteractable(bool interactable)
        {
            if (returnToHubButton != null)
                returnToHubButton.interactable = interactable;

            if (defeatReturnToHubButton != null)
                defeatReturnToHubButton.interactable = interactable;
        }

        private void AddButtonListeners()
        {
            continueCollectingButton?.onClick.AddListener(HandleCollapseRequested);
            victoryCompleteButton?.onClick.AddListener(HandleCompletionRequested);
            collapsedCompleteButton?.onClick.AddListener(HandleCompletionRequested);
            cancelCompletionButton?.onClick.AddListener(HandleCompletionCancelled);
            confirmCompletionButton?.onClick.AddListener(HandleCompletionConfirmed);
            returnToHubButton?.onClick.AddListener(HandleReturnToHubRequested);
            defeatReturnToHubButton?.onClick.AddListener(
                HandleReturnToHubRequested);
        }

        private void RemoveButtonListeners()
        {
            continueCollectingButton?.onClick.RemoveListener(HandleCollapseRequested);
            victoryCompleteButton?.onClick.RemoveListener(HandleCompletionRequested);
            collapsedCompleteButton?.onClick.RemoveListener(HandleCompletionRequested);
            cancelCompletionButton?.onClick.RemoveListener(HandleCompletionCancelled);
            confirmCompletionButton?.onClick.RemoveListener(HandleCompletionConfirmed);
            returnToHubButton?.onClick.RemoveListener(HandleReturnToHubRequested);
            defeatReturnToHubButton?.onClick.RemoveListener(
                HandleReturnToHubRequested);
        }

        private void HandleCollapseRequested()
        {
            CollapseRequested?.Invoke();
        }

        private void HandleCompletionRequested()
        {
            CompletionRequested?.Invoke();
        }

        private void HandleCompletionCancelled()
        {
            CompletionCancelled?.Invoke();
        }

        private void HandleCompletionConfirmed()
        {
            CompletionConfirmed?.Invoke();
        }

        private void HandleReturnToHubRequested()
        {
            ReturnToHubRequested?.Invoke();
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }
    }
}
