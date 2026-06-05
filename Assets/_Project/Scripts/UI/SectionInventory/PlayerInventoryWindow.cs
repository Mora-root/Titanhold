using System;
using UnityEngine;

namespace Titanhold.UI.SectionInventory
{
    public sealed class PlayerInventoryWindow : MonoBehaviour
    {
        [SerializeField] private global::PlayerInventory playerInventory;
        [SerializeField] private InventorySectionGridView gridView;
        [SerializeField] private InventoryCategoryTabButton[] tabButtons;
        [SerializeField] private bool showMiscTab;
        [SerializeField] private global::ItemCategory selectedCategory = global::ItemCategory.Equipment;

        private bool loggedMissingInventory;
        private bool loggedMissingGridView;
        private bool loggedMissingSection;

        public event Action<global::ItemCategory, int> SlotRightClicked;

        private void Awake()
        {
            NormalizeSelectedCategory();
            InitializeTabs();
        }

        private void OnEnable()
        {
            if (playerInventory != null)
            {
                playerInventory.Changed += HandleInventoryChanged;
                playerInventory.SectionChanged += HandleSectionChanged;
            }
            else
            {
                LogMissingInventory();
            }

            if (gridView != null)
                gridView.SlotRightClicked += HandleSlotRightClicked;
            else
                LogMissingGridView();

            InitializeTabs();
            Refresh();
        }

        private void OnDisable()
        {
            if (playerInventory != null)
            {
                playerInventory.Changed -= HandleInventoryChanged;
                playerInventory.SectionChanged -= HandleSectionChanged;
            }

            if (gridView != null)
                gridView.SlotRightClicked -= HandleSlotRightClicked;
        }

        private void OnValidate()
        {
            NormalizeSelectedCategory();
        }

        [ContextMenu("Refresh")]
        public void Refresh()
        {
            RefreshCurrentSection();
        }

        public void SelectCategory(global::ItemCategory category)
        {
            if (category == global::ItemCategory.Misc && !showMiscTab)
                return;

            selectedCategory = category;
            InitializeTabs();
            RefreshCurrentSection();
        }

        private void RefreshCurrentSection()
        {
            if (gridView == null)
            {
                LogMissingGridView();
                return;
            }

            if (playerInventory == null)
            {
                LogMissingInventory();
                gridView.Clear();
                return;
            }

            global::ItemContainerSection section = playerInventory.GetSection(selectedCategory);

            if (section == null)
            {
                LogMissingSection(selectedCategory);
                gridView.Clear();
                return;
            }

            gridView.ShowSection(section);
        }

        private void InitializeTabs()
        {
            if (tabButtons == null)
                return;

            foreach (InventoryCategoryTabButton tabButton in tabButtons)
            {
                if (tabButton == null)
                    continue;

                bool isMisc = tabButton.Category == global::ItemCategory.Misc;
                tabButton.Initialize(this);
                tabButton.SetVisible(!isMisc || showMiscTab);
                tabButton.SetSelected(tabButton.Category == selectedCategory);
            }
        }

        private void HandleInventoryChanged()
        {
            RefreshCurrentSection();
        }

        private void HandleSectionChanged(global::ItemCategory category)
        {
            if (category == selectedCategory)
                RefreshCurrentSection();
        }

        private void HandleSlotRightClicked(global::ItemCategory category, int slotIndex)
        {
            SlotRightClicked?.Invoke(category, slotIndex);
        }

        private void NormalizeSelectedCategory()
        {
            if (selectedCategory == global::ItemCategory.Misc && !showMiscTab)
                selectedCategory = global::ItemCategory.Equipment;
        }

        private void LogMissingInventory()
        {
            if (loggedMissingInventory)
                return;

            Debug.LogWarning($"{nameof(PlayerInventoryWindow)} requires a PlayerInventory reference.", this);
            loggedMissingInventory = true;
        }

        private void LogMissingGridView()
        {
            if (loggedMissingGridView)
                return;

            Debug.LogWarning($"{nameof(PlayerInventoryWindow)} requires an InventorySectionGridView reference.", this);
            loggedMissingGridView = true;
        }

        private void LogMissingSection(global::ItemCategory category)
        {
            if (loggedMissingSection)
                return;

            Debug.LogWarning($"{nameof(PlayerInventoryWindow)} could not find inventory section '{category}'.", this);
            loggedMissingSection = true;
        }
    }
}
