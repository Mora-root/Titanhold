using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Titanhold.UI.Equipment
{
    public sealed class CharacterStatRowView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private global::StatType statType;
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private string labelOverride;
        [SerializeField] private string valueFormat = "0.##";
        [SerializeField] private string valueSuffix;

        public global::StatType StatType => statType;
        public RectTransform RectTransform { get; private set; }
        public bool IsPointerInside { get; private set; }

        public event System.Action<CharacterStatRowView> PointerEntered;
        public event System.Action<CharacterStatRowView> PointerExited;

        private void Awake()
        {
            RectTransform = transform as RectTransform;

            if (labelText != null)
                labelText.raycastTarget = false;

            if (valueText != null)
                valueText.raycastTarget = false;

            RefreshLabel();
            EnsureArmorRaycastSurface();
        }

        private void OnValidate()
        {
            RefreshLabel();
        }

        public void SetValue(float value)
        {
            if (valueText == null)
                return;

            string format = string.IsNullOrWhiteSpace(valueFormat) ? "0.##" : valueFormat;
            valueText.text = $"{value.ToString(format)}{valueSuffix}";
        }

        public void Clear()
        {
            if (valueText != null)
                valueText.text = "-";
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            IsPointerInside = true;
            PointerEntered?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            IsPointerInside = false;
            PointerExited?.Invoke(this);
        }

        private void OnDisable()
        {
            IsPointerInside = false;
        }

        private void RefreshLabel()
        {
            if (labelText != null && !string.IsNullOrWhiteSpace(labelOverride))
                labelText.text = labelOverride;
        }

        private void EnsureArmorRaycastSurface()
        {
            if (statType != global::StatType.Armor || GetComponent<Graphic>() != null)
                return;

            Image surface = gameObject.AddComponent<Image>();
            surface.color = Color.clear;
            surface.raycastTarget = true;
        }
    }
}
