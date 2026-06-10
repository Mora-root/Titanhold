using System;
using Titanhold.UI.Common;
using UnityEngine;

namespace Titanhold.UI.SectionInventory
{
    public sealed class PlayerInventoryWindow : MonoBehaviour, IItemSlotEventSource
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
        public event Action<global::ItemCategory, int> SlotDragStarted;
        public event Action<InventorySlotView, global::ItemCategory, int> SlotDragStartedWithView;
        public event Action<global::ItemCategory, int> SlotDropped;
        public event Action SlotDragEnded;
        public event Action<global::ItemSlotRef> ItemSlotRightClicked;
        public event Action<IItemDragSourceView, global::ItemSlotRef, ItemDragVisualData> ItemSlotDragStarted;
        public event Action<global::ItemSlotRef> ItemSlotDropped;
        public event Action ItemSlotDragEnded;

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
            {
                gridView.SlotRightClicked += HandleSlotRightClicked;
                gridView.SlotDragStarted += HandleSlotDragStarted;
                gridView.SlotDragStartedWithView += HandleSlotDragStartedWithView;
                gridView.SlotDropped += HandleSlotDropped;
                gridView.SlotDragEnded += HandleSlotDragEnded;
            }
            else
            {
                LogMissingGridView();
            }

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
            {
                gridView.SlotRightClicked -= HandleSlotRightClicked;
                gridView.SlotDragStarted -= HandleSlotDragStarted;
                gridView.SlotDragStartedWithView -= HandleSlotDragStartedWithView;
                gridView.SlotDropped -= HandleSlotDropped;
                gridView.SlotDragEnded -= HandleSlotDragEnded;
            }
        }

        private void OnValidate()
        {
            NormalizeSelectedCategory();
        }

        public void SetPlayerInventory(global::PlayerInventory inventory)
        {
            if (ReferenceEquals(playerInventory, inventory))
            {
                Refresh();
                return;
            }

            if (isActiveAndEnabled && playerInventory != null)
            {
                playerInventory.Changed -= HandleInventoryChanged;
                playerInventory.SectionChanged -= HandleSectionChanged;
            }

            playerInventory = inventory;
            loggedMissingInventory = false;

            if (isActiveAndEnabled && playerInventory != null)
            {
                playerInventory.Changed += HandleInventoryChanged;
                playerInventory.SectionChanged += HandleSectionChanged;
            }

            Refresh();
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

            global::ItemSlotRef slotRef = CreateSlotRef(category, slotIndex);
            if (slotRef.IsValid)
                ItemSlotRightClicked?.Invoke(slotRef);
        }

        private void HandleSlotDragStarted(global::ItemCategory category, int slotIndex)
        {
            SlotDragStarted?.Invoke(category, slotIndex);
        }

        private void HandleSlotDragStartedWithView(InventorySlotView slotView, global::ItemCategory category, int slotIndex)
        {
            SlotDragStartedWithView?.Invoke(slotView, category, slotIndex);

            global::ItemSlotRef slotRef = CreateSlotRef(category, slotIndex);
            if (!slotRef.IsValid)
                return;

            global::ItemSlot slot = playerInventory.GetSlot(category, slotIndex);
            global::ItemStack stack = slot?.Stack;
            global::ItemDefinition definition = stack?.Definition;
            if (definition == null)
                return;

            ItemSlotDragStarted?.Invoke(
                slotView,
                slotRef,
                new ItemDragVisualData(definition.Icon, stack.Amount));
        }

        private void HandleSlotDropped(global::ItemCategory category, int slotIndex)
        {
            SlotDropped?.Invoke(category, slotIndex);

            global::ItemSlotRef slotRef = CreateSlotRef(category, slotIndex);
            if (slotRef.IsValid)
                ItemSlotDropped?.Invoke(slotRef);
        }

        private void HandleSlotDragEnded()
        {
            SlotDragEnded?.Invoke();
            ItemSlotDragEnded?.Invoke();
        }

        private global::ItemSlotRef CreateSlotRef(global::ItemCategory category, int slotIndex)
        {
            return global::ItemSlotRef.ForContainer(playerInventory, category, slotIndex);
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
