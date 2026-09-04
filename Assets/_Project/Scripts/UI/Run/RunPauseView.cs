using System;
using UnityEngine;
using UnityEngine.UI;

namespace Titanhold.UI.Run
{
    public enum RunPauseViewMode
    {
        Hidden,
        Paused,
        ExitConfirmation
    }

    [DisallowMultipleComponent]
    public sealed class RunPauseView : MonoBehaviour
    {
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject exitConfirmationPanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private Button cancelExitButton;
        [SerializeField] private Button confirmExitButton;

        public event Action ResumeRequested;
        public event Action ExitRequested;
        public event Action ExitCancelled;
        public event Action ExitConfirmed;

        public RunPauseViewMode Mode { get; private set; }
        public bool IsVisible => Mode != RunPauseViewMode.Hidden;

        private void Awake()
        {
            Show(RunPauseViewMode.Hidden);
        }

        private void OnEnable()
        {
            resumeButton?.onClick.AddListener(HandleResumeRequested);
            exitButton?.onClick.AddListener(HandleExitRequested);
            cancelExitButton?.onClick.AddListener(HandleExitCancelled);
            confirmExitButton?.onClick.AddListener(HandleExitConfirmed);
        }

        private void OnDisable()
        {
            resumeButton?.onClick.RemoveListener(HandleResumeRequested);
            exitButton?.onClick.RemoveListener(HandleExitRequested);
            cancelExitButton?.onClick.RemoveListener(HandleExitCancelled);
            confirmExitButton?.onClick.RemoveListener(HandleExitConfirmed);
        }

        public void Show(RunPauseViewMode mode)
        {
            Mode = mode;
            SetActive(pausePanel, mode == RunPauseViewMode.Paused);
            SetActive(
                exitConfirmationPanel,
                mode == RunPauseViewMode.ExitConfirmation);
        }

        public void SetExitInteractable(bool interactable)
        {
            if (exitButton != null)
                exitButton.interactable = interactable;

            if (confirmExitButton != null)
                confirmExitButton.interactable = interactable;
        }

        private void HandleResumeRequested()
        {
            ResumeRequested?.Invoke();
        }

        private void HandleExitRequested()
        {
            ExitRequested?.Invoke();
        }

        private void HandleExitCancelled()
        {
            ExitCancelled?.Invoke();
        }

        private void HandleExitConfirmed()
        {
            ExitConfirmed?.Invoke();
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }
    }
}
