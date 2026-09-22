using System;
using System.Collections.Generic;
using Titanhold.Combat.Abilities;
using TMPro;
using UnityEngine;

namespace Titanhold.UI.Run
{
    [DisallowMultipleComponent]
    public sealed class RunCombatHudView : MonoBehaviour
    {
        [SerializeField] private RunAbilitySlotView[] abilitySlots =
            Array.Empty<RunAbilitySlotView>();
        [SerializeField] private TMP_Text combatResourceText;

        private readonly List<RunAbilitySlotView> subscribedSlots = new();

        public event Action<int> AbilitySlotPressed;

        public int AbilitySlotCount => abilitySlots?.Length ?? 0;
        public IReadOnlyList<RunAbilitySlotView> AbilitySlots =>
            abilitySlots ?? Array.Empty<RunAbilitySlotView>();
        public TMP_Text CombatResourceText => combatResourceText;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            RunAbilitySlotView[] configuredSlots,
            TMP_Text configuredCombatResourceText)
        {
            abilitySlots = configuredSlots ??
                Array.Empty<RunAbilitySlotView>();
            combatResourceText = configuredCombatResourceText;
            if (isActiveAndEnabled)
                RefreshSlotSubscriptions();
        }
#endif

        private void Awake()
        {
            Clear();
        }

        private void OnEnable()
        {
            RefreshSlotSubscriptions();
        }

        private void OnDisable()
        {
            ClearSlotSubscriptions();
        }

        public void Clear()
        {
            if (abilitySlots != null)
            {
                for (int i = 0; i < abilitySlots.Length; i++)
                {
                    abilitySlots[i]?.RenderContent(
                        i,
                        string.Empty,
                        string.Empty,
                        null);
                }
            }

            RenderCombatResourceUnavailable(string.Empty);
        }

        public bool TryRenderAbility(
            int slotIndex,
            string abilityId,
            string displayName,
            Sprite icon)
        {
            if (!TryGetSlot(slotIndex, out RunAbilitySlotView slot))
                return false;

            slot.RenderContent(
                slotIndex,
                abilityId,
                displayName,
                icon);
            return true;
        }

        public bool TryRenderCooldown(
            int slotIndex,
            AbilityCooldownSnapshot cooldown)
        {
            if (!TryGetSlot(slotIndex, out RunAbilitySlotView slot))
                return false;

            slot.RenderCooldown(cooldown);
            return true;
        }

        public bool TryRenderReady(int slotIndex)
        {
            if (!TryGetSlot(slotIndex, out RunAbilitySlotView slot))
                return false;

            slot.RenderReady();
            return true;
        }

        public void RenderCombatResource(
            string label,
            float current,
            float maximum)
        {
            if (combatResourceText == null)
                return;

            combatResourceText.text =
                $"{NormalizeLabel(label)} " +
                $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(maximum)}";
        }

        public void RenderCombatResourceUnavailable(string label)
        {
            if (combatResourceText != null)
                combatResourceText.text = $"{NormalizeLabel(label)} --";
        }

        private bool TryGetSlot(
            int slotIndex,
            out RunAbilitySlotView slot)
        {
            slot = null;
            if (abilitySlots == null ||
                slotIndex < 0 ||
                slotIndex >= abilitySlots.Length)
            {
                return false;
            }

            slot = abilitySlots[slotIndex];
            return slot != null;
        }

        private void RefreshSlotSubscriptions()
        {
            ClearSlotSubscriptions();
            if (abilitySlots == null)
                return;

            for (int i = 0; i < abilitySlots.Length; i++)
            {
                RunAbilitySlotView slot = abilitySlots[i];
                if (slot == null || subscribedSlots.Contains(slot))
                    continue;

                slot.UseRequested += HandleSlotUseRequested;
                subscribedSlots.Add(slot);
            }
        }

        private void ClearSlotSubscriptions()
        {
            for (int i = 0; i < subscribedSlots.Count; i++)
            {
                RunAbilitySlotView slot = subscribedSlots[i];
                if (slot != null)
                    slot.UseRequested -= HandleSlotUseRequested;
            }

            subscribedSlots.Clear();
        }

        private void HandleSlotUseRequested(int slotIndex)
        {
            AbilitySlotPressed?.Invoke(slotIndex);
        }

        private static string NormalizeLabel(string label)
        {
            string normalized = label?.Trim() ?? string.Empty;
            return normalized.Length > 0
                ? normalized.ToUpperInvariant()
                : "RESOURCE";
        }
    }
}
