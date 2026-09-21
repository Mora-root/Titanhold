using System;
using Titanhold.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Titanhold.UI.Hub
{
    [DisallowMultipleComponent]
    public sealed class HubStartingAbilitySelectionView : MonoBehaviour
    {
        private const int MaximumOptionCount = 3;

        [SerializeField] private GameObject selectionRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button[] optionButtons =
            new Button[MaximumOptionCount];
        [SerializeField] private TMP_Text[] optionNameTexts =
            new TMP_Text[MaximumOptionCount];
        [SerializeField] private TMP_Text[] optionDescriptionTexts =
            new TMP_Text[MaximumOptionCount];
        [SerializeField] private Image[] optionIcons =
            new Image[MaximumOptionCount];

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
        public bool HasHeadingReferences =>
            titleText != null && subtitleText != null;
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

        public void ConfigureHeadingsForEditor(
            TMP_Text configuredTitleText,
            TMP_Text configuredSubtitleText)
        {
            titleText = configuredTitleText;
            subtitleText = configuredSubtitleText;
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
                model.Options.Count <= 0 ||
                model.Options.Count > MaximumOptionCount)
            {
                return false;
            }

            Subscribe();
            if (!subscribed)
                return false;

            return ShowOptions(
                model.Options.Count,
                index => model.Options[index].DisplayName,
                index => model.Options[index].Description,
                index => model.Options[index].Icon);
        }

        public bool TryShow(ChoiceSelectionModel model)
        {
            if (!HasRequiredReferences || model == null ||
                model.Options.Count <= 0 ||
                model.Options.Count > MaximumOptionCount)
            {
                return false;
            }

            Subscribe();
            if (!subscribed)
                return false;

            return ShowOptions(
                model.Options.Count,
                index => model.Options[index].DisplayName,
                index => model.Options[index].Description,
                index => model.Options[index].Icon);
        }

        public void SetHeading(string title, string subtitle)
        {
            if (titleText != null)
                titleText.text = title ?? string.Empty;

            if (subtitleText != null)
                subtitleText.text = subtitle ?? string.Empty;
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

            optionCallbacks = new UnityAction[MaximumOptionCount];
            for (int i = 0; i < MaximumOptionCount; i++)
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

            for (int i = 0; i < MaximumOptionCount; i++)
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
            if (optionIndex >= 0 &&
                optionIndex < MaximumOptionCount &&
                optionButtons != null &&
                optionIndex < optionButtons.Length &&
                optionButtons[optionIndex] != null &&
                optionButtons[optionIndex].gameObject.activeInHierarchy)
            {
                OptionSelected?.Invoke(optionIndex);
            }
        }

        private bool ShowOptions(
            int optionCount,
            Func<int, string> getDisplayName,
            Func<int, string> getDescription,
            Func<int, Sprite> getIcon)
        {
            for (int i = 0; i < MaximumOptionCount; i++)
            {
                bool hasOption = i < optionCount;
                optionButtons[i].gameObject.SetActive(hasOption);
                if (!hasOption)
                    continue;

                Sprite icon = getIcon(i);
                optionNameTexts[i].text = getDisplayName(i);
                optionDescriptionTexts[i].text = getDescription(i);
                optionIcons[i].sprite = icon;
                optionIcons[i].enabled = icon != null;
            }

            statusText.text = string.Empty;
            selectionRoot.SetActive(true);
            SetInteractable(true);
            return true;
        }

        private static bool HasCompleteArray<T>(T[] values)
            where T : UnityEngine.Object
        {
            if (values == null || values.Length != MaximumOptionCount)
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
