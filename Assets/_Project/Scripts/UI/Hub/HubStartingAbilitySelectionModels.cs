using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Titanhold.UI.Hub
{
    public sealed class HubStartingAbilityOption
    {
        internal HubStartingAbilityOption(
            string abilityId,
            string displayName,
            string description,
            Sprite icon)
        {
            AbilityId = abilityId;
            DisplayName = displayName;
            Description = description;
            Icon = icon;
        }

        public string AbilityId { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public Sprite Icon { get; }
    }

    public sealed class HubStartingAbilitySelectionModel
    {
        private readonly ReadOnlyCollection<HubStartingAbilityOption> options;

        internal HubStartingAbilitySelectionModel(
            string playerId,
            string choiceId,
            IReadOnlyList<HubStartingAbilityOption> options)
        {
            PlayerId = playerId;
            ChoiceId = choiceId;

            HubStartingAbilityOption[] copy =
                new HubStartingAbilityOption[options.Count];
            for (int i = 0; i < copy.Length; i++)
                copy[i] = options[i];

            this.options = Array.AsReadOnly(copy);
        }

        public string PlayerId { get; }
        public string ChoiceId { get; }
        public IReadOnlyList<HubStartingAbilityOption> Options => options;
    }

    public enum HubStartingAbilityPresentationError
    {
        None,
        MissingChoice,
        NotStartingChoice,
        WrongTargetSlot,
        InvalidOptionCount,
        AbilityNotFound
    }
}
