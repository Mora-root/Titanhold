using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Titanhold.UI.Hub
{
    [DisallowMultipleComponent]
    public sealed class HubRunPreparationView : MonoBehaviour
    {
        [SerializeField] private TMP_Text characterNameText;
        [SerializeField] private TMP_Text difficultyNameText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button startRunButton;

        public event Action StartRequested;

        public Button StartRunButton => startRunButton;

        private void OnEnable()
        {
            startRunButton?.onClick.AddListener(HandleStartRequested);
        }

        private void OnDisable()
        {
            startRunButton?.onClick.RemoveListener(HandleStartRequested);
        }

        public void SetSelection(string characterName, string difficultyName)
        {
            if (characterNameText != null)
                characterNameText.text = characterName ?? string.Empty;

            if (difficultyNameText != null)
                difficultyNameText.text = difficultyName ?? string.Empty;
        }

        public void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message ?? string.Empty;
        }

        public void SetStartInteractable(bool interactable)
        {
            if (startRunButton != null)
                startRunButton.interactable = interactable;
        }

        private void HandleStartRequested()
        {
            StartRequested?.Invoke();
        }
    }
}
