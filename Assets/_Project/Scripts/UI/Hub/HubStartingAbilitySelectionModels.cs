using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Titanhold.Run;
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

    public enum HubStartingAbilityCoordinationError
    {
        None,
        InvalidPlayerId,
        PendingChoiceNotFound,
        PresentationRejected,
        NoActiveSelection,
        InvalidAbilityId,
        AbilityNotOffered,
        SelectionRejected
    }

    public readonly struct HubStartingAbilityCoordinationResult
    {
        private HubStartingAbilityCoordinationResult(
            bool success,
            HubStartingAbilityCoordinationError error,
            HubStartingAbilitySelectionModel model,
            HubStartingAbilityPresentationError presentationError,
            RunStartingAbilitySelectionResult selectionResult)
        {
            Success = success;
            Error = error;
            Model = model;
            PresentationError = presentationError;
            SelectionResult = selectionResult;
        }

        public bool Success { get; }
        public HubStartingAbilityCoordinationError Error { get; }
        public HubStartingAbilitySelectionModel Model { get; }
        public HubStartingAbilityPresentationError PresentationError { get; }
        public RunStartingAbilitySelectionResult SelectionResult { get; }

        internal static HubStartingAbilityCoordinationResult Succeeded(
            HubStartingAbilitySelectionModel model,
            RunStartingAbilitySelectionResult selectionResult = default)
        {
            return new HubStartingAbilityCoordinationResult(
                true,
                HubStartingAbilityCoordinationError.None,
                model,
                HubStartingAbilityPresentationError.None,
                selectionResult);
        }

        internal static HubStartingAbilityCoordinationResult Failed(
            HubStartingAbilityCoordinationError error,
            HubStartingAbilitySelectionModel model = null,
            HubStartingAbilityPresentationError presentationError =
                HubStartingAbilityPresentationError.None,
            RunStartingAbilitySelectionResult selectionResult = default)
        {
            return new HubStartingAbilityCoordinationResult(
                false,
                error,
                model,
                presentationError,
                selectionResult);
        }
    }
}
