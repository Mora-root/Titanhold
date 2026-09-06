using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Titanhold.UI.Hub
{
    [DisallowMultipleComponent]
    public sealed class HubStartingAbilitySelectionView : MonoBehaviour
    {
        private const int OptionCount = 3;

        [SerializeField] private GameObject selectionRoot;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button[] optionButtons =
            new Button[OptionCount];
        [SerializeField] private TMP_Text[] optionNameTexts =
            new TMP_Text[OptionCount];
        [SerializeField] private TMP_Text[] optionDescriptionTexts =
            new TMP_Text[OptionCount];
        [SerializeField] private Image[] optionIcons =
            new Image[OptionCount];

        private UnityAction[] optionCallbacks;
        private bool subscribed;

        public event Action<int> OptionSelected;

        public bool HasRequiredReferences =>
            selectionRoot != null &&
            statusText != null &&
            HasCompleteArray(optionButtons) &&
            HasCompleteArray(optionNameTexts) &&
            HasCompleteArray(optionDescriptionTexts) &&
            HasCompleteArray(optionIcons);
        public GameObject SelectionRoot => selectionRoot;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            GameObject configuredSelectionRoot,
            TMP_Text configuredStatusText,
            Button[] configuredOptionButtons,
            TMP_Text[] configuredOptionNameTexts,
            TMP_Text[] configuredOptionDescriptionTexts,
            Image[] configuredOptionIcons)
        {
            selectionRoot = configuredSelectionRoot;
            statusText = configuredStatusText;
            optionButtons = CopyArray(configuredOptionButtons);
            optionNameTexts = CopyArray(configuredOptionNameTexts);
            optionDescriptionTexts = CopyArray(
                configuredOptionDescriptionTexts);
            optionIcons = CopyArray(configuredOptionIcons);
        }
#endif

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public bool TryShow(HubStartingAbilitySelectionModel model)
        {
            if (!HasRequiredReferences || model == null ||
                model.Options.Count != OptionCount)
            {
                return false;
            }

            Subscribe();
            if (!subscribed)
                return false;

            for (int i = 0; i < OptionCount; i++)
            {
                HubStartingAbilityOption option = model.Options[i];
                optionNameTexts[i].text = option.DisplayName;
                optionDescriptionTexts[i].text = option.Description;
                optionIcons[i].sprite = option.Icon;
                optionIcons[i].enabled = option.Icon != null;
            }

            statusText.text = string.Empty;
            selectionRoot.SetActive(true);
            SetInteractable(true);
            return true;
        }

        public void Hide()
        {
            if (selectionRoot != null)
                selectionRoot.SetActive(false);
        }

        public void SetInteractable(bool interactable)
        {
            if (optionButtons == null)
                return;

            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (optionButtons[i] != null)
                    optionButtons[i].interactable = interactable;
            }
        }

        public void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message ?? string.Empty;
        }

        private void Subscribe()
        {
            if (subscribed || !HasCompleteArray(optionButtons))
                return;

            optionCallbacks = new UnityAction[OptionCount];
            for (int i = 0; i < OptionCount; i++)
            {
                int optionIndex = i;
                optionCallbacks[i] = () => HandleOptionSelected(optionIndex);
                optionButtons[i].onClick.AddListener(optionCallbacks[i]);
            }

            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
                return;

            for (int i = 0; i < OptionCount; i++)
            {
                if (optionButtons != null && i < optionButtons.Length &&
                    optionButtons[i] != null && optionCallbacks[i] != null)
                {
                    optionButtons[i].onClick.RemoveListener(
                        optionCallbacks[i]);
                }
            }

            optionCallbacks = null;
            subscribed = false;
        }

        private void HandleOptionSelected(int optionIndex)
        {
            if (optionIndex >= 0 && optionIndex < OptionCount)
                OptionSelected?.Invoke(optionIndex);
        }

        private static bool HasCompleteArray<T>(T[] values)
            where T : UnityEngine.Object
        {
            if (values == null || values.Length != OptionCount)
                return false;

            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] == null)
                    return false;
            }

            return true;
        }

#if UNITY_EDITOR
        private static T[] CopyArray<T>(T[] source)
        {
            if (source == null)
                return Array.Empty<T>();

            T[] copy = new T[source.Length];
            Array.Copy(source, copy, source.Length);
            return copy;
        }
#endif
    }
}
