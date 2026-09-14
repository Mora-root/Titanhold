using System;
using Titanhold.Combat.Abilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Titanhold.UI.Run
{
    [DisallowMultipleComponent]
    public sealed class RunAbilitySlotView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Image cooldownOverlay;
        [SerializeField] private TMP_Text keyText;
        [SerializeField] private TMP_Text abilityNameText;
        [SerializeField] private TMP_Text cooldownText;

        private string assignedAbilityId = string.Empty;
        private int renderedCooldownSeconds = -1;

        public Image IconImage => iconImage;
        public Image CooldownOverlay => cooldownOverlay;
        public TMP_Text KeyText => keyText;
        public TMP_Text AbilityNameText => abilityNameText;
        public TMP_Text CooldownText => cooldownText;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            Image configuredIcon,
            Image configuredCooldownOverlay,
            TMP_Text configuredKeyText,
            TMP_Text configuredAbilityNameText,
            TMP_Text configuredCooldownText)
        {
            iconImage = configuredIcon;
            cooldownOverlay = configuredCooldownOverlay;
            keyText = configuredKeyText;
            abilityNameText = configuredAbilityNameText;
            cooldownText = configuredCooldownText;
        }
#endif

        public void RenderContent(
            int slotIndex,
            string abilityId,
            string displayName,
            Sprite icon)
        {
            assignedAbilityId = abilityId?.Trim() ?? string.Empty;
            if (keyText != null)
                keyText.text = (slotIndex + 1).ToString();

            bool isAssigned = assignedAbilityId.Length > 0;
            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = isAssigned && icon != null;
            }

            if (abilityNameText != null)
            {
                abilityNameText.text = isAssigned
                    ? ResolveDisplayName(displayName, assignedAbilityId)
                    : string.Empty;
            }

            RenderReady();
        }

        public void RenderCooldown(AbilityCooldownSnapshot cooldown)
        {
            if (assignedAbilityId.Length == 0 ||
                !cooldown.IsValid ||
                !string.Equals(
                    assignedAbilityId,
                    cooldown.AbilityId,
                    StringComparison.Ordinal) ||
                !cooldown.IsCoolingDown)
            {
                RenderReady();
                return;
            }

            if (cooldownOverlay != null)
            {
                cooldownOverlay.gameObject.SetActive(true);
                cooldownOverlay.fillAmount =
                    (float)cooldown.NormalizedRemaining;
            }

            int seconds = Mathf.Max(
                1,
                Mathf.CeilToInt((float)cooldown.Remaining));
            if (cooldownText != null && seconds != renderedCooldownSeconds)
                cooldownText.text = seconds.ToString();

            renderedCooldownSeconds = seconds;
            if (cooldownText != null)
                cooldownText.gameObject.SetActive(true);
        }

        public void RenderReady()
        {
            renderedCooldownSeconds = -1;
            if (cooldownOverlay != null)
            {
                cooldownOverlay.fillAmount = 0f;
                cooldownOverlay.gameObject.SetActive(false);
            }

            if (cooldownText != null)
            {
                cooldownText.text = string.Empty;
                cooldownText.gameObject.SetActive(false);
            }
        }

        private static string ResolveDisplayName(
            string displayName,
            string abilityId)
        {
            string normalizedName = displayName?.Trim() ?? string.Empty;
            if (normalizedName.Length > 0)
                return normalizedName;

            const string prefix = "ability:";
            return abilityId.StartsWith(
                prefix,
                StringComparison.Ordinal)
                ? abilityId.Substring(prefix.Length)
                : abilityId;
        }
    }
}
