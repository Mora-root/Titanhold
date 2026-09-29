using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Titanhold.UI.Run
{
    public enum RunChapterCompletionViewMode
    {
        Hidden,
        Confirmation,
        Completed
    }

    [DisallowMultipleComponent]
    public sealed class RunChapterCompletionView : MonoBehaviour
    {
        [SerializeField] private GameObject confirmationPanel;
        [SerializeField] private GameObject completedPanel;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button returnToHubButton;
        [SerializeField] private TextMeshProUGUI rewardText;
        [SerializeField] private TextMeshProUGUI statusText;

        public event Action ConfirmationCancelled;
        public event Action ConfirmationAccepted;
        public event Action ReturnToHubRequested;

        public RunChapterCompletionViewMode Mode { get; private set; }
        public bool IsVisible => Mode != RunChapterCompletionViewMode.Hidden;
        public bool IsConfigured =>
            confirmationPanel != null &&
            completedPanel != null &&
            cancelButton != null &&
            confirmButton != null &&
            returnToHubButton != null &&
            rewardText != null &&
            statusText != null;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            GameObject configuredConfirmationPanel,
            GameObject configuredCompletedPanel,
            Button configuredCancelButton,
            Button configuredConfirmButton,
            Button configuredReturnToHubButton,
            TextMeshProUGUI configuredRewardText,
            TextMeshProUGUI configuredStatusText)
        {
            confirmationPanel = configuredConfirmationPanel;
            completedPanel = configuredCompletedPanel;
            cancelButton = configuredCancelButton;
            confirmButton = configuredConfirmButton;
            returnToHubButton = configuredReturnToHubButton;
            rewardText = configuredRewardText;
            statusText = configuredStatusText;
        }
#endif

        private void Awake()
        {
            ShowHidden();
        }

        private void OnEnable()
        {
            cancelButton?.onClick.AddListener(HandleCancel);
            confirmButton?.onClick.AddListener(HandleConfirm);
            returnToHubButton?.onClick.AddListener(HandleReturnToHub);
        }

        private void OnDisable()
        {
            cancelButton?.onClick.RemoveListener(HandleCancel);
            confirmButton?.onClick.RemoveListener(HandleConfirm);
            returnToHubButton?.onClick.RemoveListener(HandleReturnToHub);
        }

        public void ShowHidden()
        {
            Mode = RunChapterCompletionViewMode.Hidden;
            SetActive(confirmationPanel, false);
            SetActive(completedPanel, false);
            SetStatus(string.Empty);
        }

        public void ShowConfirmation()
        {
            Mode = RunChapterCompletionViewMode.Confirmation;
            SetActive(confirmationPanel, true);
            SetActive(completedPanel, false);
            SetConfirmationInteractable(true);
            SetStatus(string.Empty);
        }

        public void ShowCompleted(
            int characterExperience,
            int crystals)
        {
            Mode = RunChapterCompletionViewMode.Completed;
            SetActive(confirmationPanel, false);
            SetActive(completedPanel, true);
            rewardText.text =
                $"Опыт персонажа: {characterExperience}\n" +
                $"Кристаллы: {crystals}";
            SetReturnToHubInteractable(true);
            SetStatus(string.Empty);
        }

        public void SetConfirmationInteractable(bool interactable)
        {
            if (cancelButton != null)
                cancelButton.interactable = interactable;
            if (confirmButton != null)
                confirmButton.interactable = interactable;
        }

        public void SetReturnToHubInteractable(bool interactable)
        {
            if (returnToHubButton != null)
                returnToHubButton.interactable = interactable;
        }

        public void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message ?? string.Empty;
        }

        private void HandleCancel()
        {
            ConfirmationCancelled?.Invoke();
        }

        private void HandleConfirm()
        {
            ConfirmationAccepted?.Invoke();
        }

        private void HandleReturnToHub()
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
