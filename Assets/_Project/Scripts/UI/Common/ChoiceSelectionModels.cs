using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Titanhold.UI.Common
{
    public sealed class ChoiceSelectionOption
    {
        public ChoiceSelectionOption(
            string optionId,
            string displayName,
            string description,
            Sprite icon)
        {
            OptionId = optionId?.Trim() ?? string.Empty;
            DisplayName = displayName?.Trim() ?? string.Empty;
            Description = description?.Trim() ?? string.Empty;
            Icon = icon;
        }

        public string OptionId { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public Sprite Icon { get; }
    }

    public sealed class ChoiceSelectionModel
    {
        private readonly ReadOnlyCollection<ChoiceSelectionOption> options;

        public ChoiceSelectionModel(
            string playerId,
            string choiceId,
            IReadOnlyList<ChoiceSelectionOption> options)
        {
            PlayerId = playerId?.Trim() ?? string.Empty;
            ChoiceId = choiceId?.Trim() ?? string.Empty;

            int optionCount = options?.Count ?? 0;
            ChoiceSelectionOption[] copy =
                new ChoiceSelectionOption[optionCount];
            for (int i = 0; i < copy.Length; i++)
                copy[i] = options[i];

            this.options = Array.AsReadOnly(copy);
        }

        public string PlayerId { get; }
        public string ChoiceId { get; }
        public IReadOnlyList<ChoiceSelectionOption> Options => options;
    }
}
