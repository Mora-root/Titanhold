using System;
using Titanhold.Combat.Flasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Titanhold.UI.Run
{
    [DisallowMultipleComponent]
    public sealed class RunFlaskSlotView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Image cooldownOverlay;
        [SerializeField] private TMP_Text keyText;
        [SerializeField] private TMP_Text flaskNameText;
        [SerializeField] private TMP_Text cooldownText;

        private int renderedSlotIndex = -1;
        private string assignedFlaskId = string.Empty;

        public event Action<int> UseRequested;

        public Image IconImage => iconImage;
        public Image CooldownOverlay => cooldownOverlay;
        public TMP_Text KeyText => keyText;
        public TMP_Text FlaskNameText => flaskNameText;
        public TMP_Text CooldownText => cooldownText;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            Image configuredIcon,
            Image configuredCooldownOverlay,
            TMP_Text configuredKeyText,
            TMP_Text configuredFlaskNameText,
            TMP_Text configuredCooldownText)
        {
            iconImage = configuredIcon;
            cooldownOverlay = configuredCooldownOverlay;
            keyText = configuredKeyText;
            flaskNameText = configuredFlaskNameText;
            cooldownText = configuredCooldownText;
        }
#endif

        public void RenderContent(
            int slotIndex,
            string flaskId,
            string displayName,
            Sprite icon,
            string keyLabel)
        {
            renderedSlotIndex = slotIndex;
            assignedFlaskId = flaskId?.Trim() ?? string.Empty;
            if (keyText != null)
                keyText.text = keyLabel?.Trim() ?? string.Empty;

            bool assigned = assignedFlaskId.Length > 0;
            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = assigned && icon != null;
            }

            if (flaskNameText != null)
                flaskNameText.text = assigned ? displayName : string.Empty;

            RenderReady();
        }

        public void RenderCooldown(FlaskCooldownSnapshot cooldown)
        {
            if (!cooldown.IsValid ||
                !cooldown.IsCoolingDown ||
                !string.Equals(
                    assignedFlaskId,
                    cooldown.FlaskId,
                    StringComparison.Ordinal))
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

            if (cooldownText != null)
            {
                cooldownText.text = Mathf.Max(
                    1,
                    Mathf.CeilToInt((float)cooldown.Remaining)).ToString();
                cooldownText.gameObject.SetActive(true);
            }
        }

        public void RenderReady()
        {
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

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null &&
                eventData.button == PointerEventData.InputButton.Left)
            {
                TryRequestUse();
            }
        }

        public bool TryRequestUse()
        {
            if (renderedSlotIndex < 0 || assignedFlaskId.Length == 0)
                return false;

            UseRequested?.Invoke(renderedSlotIndex);
            return true;
        }
    }
}
