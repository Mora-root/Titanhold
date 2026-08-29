using System;
using TMPro;
using Titanhold.UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Titanhold.UI.Equipment
{
    public sealed class CharacterStatsPanel : MonoBehaviour
    {
        private enum Tab
        {
            Primary,
            Additional
        }

        [Header("Runtime Sources")]
        [SerializeField] private global::CharacterStats characterStats;
        [SerializeField] private global::Health health;
        [SerializeField] private global::PlayerResource playerResource;

        [Header("Vitals")]
        [SerializeField] private TMP_Text healthValueText;
        [SerializeField] private TMP_Text resourceValueText;

        [Header("Tabs")]
        [SerializeField] private Button primaryTabButton;
        [SerializeField] private Button additionalTabButton;
        [SerializeField] private GameObject primaryTabContent;
        [SerializeField] private GameObject additionalTabContent;
        [SerializeField] private GameObject primaryTabSelectedState;
        [SerializeField] private GameObject additionalTabSelectedState;
        [SerializeField] private bool openPrimaryTabByDefault = true;

        [Header("Tooltips")]
        [SerializeField] private ItemTooltipController tooltipController;

        [Header("Rows")]
        [SerializeField] private CharacterStatRowView[] primaryRows;
        [SerializeField] private CharacterStatRowView[] additionalRows;

        private Tab selectedTab;
        private bool refreshQueued;
        private bool subscribed;
        private bool loggedMissingStats;

        private void Awake()
        {
            selectedTab = openPrimaryTabByDefault ? Tab.Primary : Tab.Additional;
        }

        private void OnEnable()
        {
            Subscribe();
            SubscribeRowTooltips();
            BindTabButtons();
            ApplySelectedTab();
            Refresh();
        }

        private void OnDisable()
        {
            tooltipController?.Hide();
            UnbindTabButtons();
            UnsubscribeRowTooltips();
            Unsubscribe();
        }

        private void LateUpdate()
        {
            if (!refreshQueued)
                return;

            refreshQueued = false;
            Refresh();
        }

        public void SelectPrimaryTab()
        {
            SelectTab(Tab.Primary);
        }

        public void SelectAdditionalTab()
        {
            SelectTab(Tab.Additional);
        }

        [ContextMenu("Refresh")]
        public void Refresh()
        {
            if (characterStats == null)
            {
                ClearAll();
                LogMissingStats();
                return;
            }

            RefreshVitals();
            RefreshRows(primaryRows);
            RefreshRows(additionalRows);
        }

        private void SelectTab(Tab tab)
        {
            if (selectedTab == tab)
                return;

            selectedTab = tab;
            ApplySelectedTab();
        }

        private void ApplySelectedTab()
        {
            bool primarySelected = selectedTab == Tab.Primary;

            if (primaryTabContent != null)
                primaryTabContent.SetActive(primarySelected);

            if (additionalTabContent != null)
                additionalTabContent.SetActive(!primarySelected);

            if (primaryTabSelectedState != null)
                primaryTabSelectedState.SetActive(primarySelected);

            if (additionalTabSelectedState != null)
                additionalTabSelectedState.SetActive(!primarySelected);
        }

        private void Subscribe()
        {
            if (subscribed)
                return;

            if (characterStats == null)
                LogMissingStats();
            else
                characterStats.OnStatChanged += HandleStatChanged;

            if (health != null)
                health.OnHealthChanged += HandleHealthChanged;

            if (playerResource != null)
                playerResource.OnResourceChanged += HandleResourceChanged;

            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
                return;

            if (characterStats != null)
                characterStats.OnStatChanged -= HandleStatChanged;

            if (health != null)
                health.OnHealthChanged -= HandleHealthChanged;

            if (playerResource != null)
                playerResource.OnResourceChanged -= HandleResourceChanged;

            subscribed = false;
        }

        private void BindTabButtons()
        {
            if (primaryTabButton != null)
                primaryTabButton.onClick.AddListener(SelectPrimaryTab);

            if (additionalTabButton != null)
                additionalTabButton.onClick.AddListener(SelectAdditionalTab);
        }

        private void UnbindTabButtons()
        {
            if (primaryTabButton != null)
                primaryTabButton.onClick.RemoveListener(SelectPrimaryTab);

            if (additionalTabButton != null)
                additionalTabButton.onClick.RemoveListener(SelectAdditionalTab);
        }

        private void HandleStatChanged(global::StatType type)
        {
            refreshQueued = true;
        }

        private void HandleHealthChanged(float current, float maximum)
        {
            SetVitalText(healthValueText, current, maximum);
        }

        private void HandleResourceChanged(float current, float maximum)
        {
            SetVitalText(resourceValueText, current, maximum);
        }

        private void RefreshVitals()
        {
            if (health != null)
                SetVitalText(healthValueText, health.CurrentHealth, health.MaxHealth);
            else
                SetMissingText(healthValueText);

            if (playerResource != null)
                SetVitalText(resourceValueText, playerResource.CurrentResource, playerResource.MaxResource);
            else
                SetMissingText(resourceValueText);
        }

        private void RefreshRows(CharacterStatRowView[] rows)
        {
            if (rows == null)
                return;

            foreach (CharacterStatRowView row in rows)
            {
                if (row != null)
                {
                    row.SetValue(characterStats.GetValue(row.StatType));

                    if (row.StatType == global::StatType.Armor && row.IsPointerInside)
                        ShowArmorTooltip(row);
                }
            }
        }

        private void ClearAll()
        {
            SetMissingText(healthValueText);
            SetMissingText(resourceValueText);
            ClearRows(primaryRows);
            ClearRows(additionalRows);
        }

        private static void ClearRows(CharacterStatRowView[] rows)
        {
            if (rows == null)
                return;

            foreach (CharacterStatRowView row in rows)
            {
                if (row != null)
                    row.Clear();
            }
        }

        private static void SetVitalText(TMP_Text text, float current, float maximum)
        {
            if (text == null)
            {
                return;
            }

            var displayedCurrent = Mathf.Max(0, Mathf.RoundToInt(current));
            var displayedMaximum = Mathf.Max(0, Mathf.RoundToInt(maximum));
            text.text = $"{displayedCurrent}/{displayedMaximum}";
        }

        private static void SetMissingText(TMP_Text text)
        {
            if (text != null)
                text.text = "-";
        }

        private void LogMissingStats()
        {
            if (loggedMissingStats)
                return;

            Debug.LogWarning($"{nameof(CharacterStatsPanel)} requires a CharacterStats reference.", this);
            loggedMissingStats = true;
        }

        private void SubscribeRowTooltips()
        {
            SubscribeRowTooltips(primaryRows);
            SubscribeRowTooltips(additionalRows);
        }

        private void UnsubscribeRowTooltips()
        {
            UnsubscribeRowTooltips(primaryRows);
            UnsubscribeRowTooltips(additionalRows);
        }

        private void SubscribeRowTooltips(CharacterStatRowView[] rows)
        {
            if (rows == null)
                return;

            foreach (CharacterStatRowView row in rows)
            {
                if (row == null)
                    continue;

                row.PointerEntered += HandleRowPointerEntered;
                row.PointerExited += HandleRowPointerExited;
            }
        }

        private void UnsubscribeRowTooltips(CharacterStatRowView[] rows)
        {
            if (rows == null)
                return;

            foreach (CharacterStatRowView row in rows)
            {
                if (row == null)
                    continue;

                row.PointerEntered -= HandleRowPointerEntered;
                row.PointerExited -= HandleRowPointerExited;
            }
        }

        private void HandleRowPointerEntered(CharacterStatRowView row)
        {
            if (row != null && row.StatType == global::StatType.Armor)
                ShowArmorTooltip(row);
        }

        private void HandleRowPointerExited(CharacterStatRowView row)
        {
            if (row != null && row.StatType == global::StatType.Armor)
                tooltipController?.Hide();
        }

        private void ShowArmorTooltip(CharacterStatRowView row)
        {
            if (characterStats == null || row == null || row.RectTransform == null)
                return;

            ResolveTooltipController();
            if (tooltipController == null)
                return;

            float armor = Mathf.Max(0f, characterStats.GetValue(global::StatType.Armor));
            float reductionPercent = 100f - DamageMitigationCalculator.ApplyArmor(100f, armor);
            ItemTooltipData data = new ItemTooltipData
            {
                Title = "Armor",
                Subtitle = "Physical damage reduction",
                Description = $"Current reduction: {reductionPercent:0.##}%"
            };

            tooltipController.Show(data, row.RectTransform);
        }

        private void ResolveTooltipController()
        {
            if (tooltipController != null)
                return;

            tooltipController = GetComponentInParent<ItemTooltipController>(true);
            if (tooltipController != null)
                return;

            Canvas canvas = GetComponentInParent<Canvas>(true);
            if (canvas != null)
                tooltipController = canvas.GetComponentInChildren<ItemTooltipController>(true);
        }
    }
}
