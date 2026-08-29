using System;
using Titanhold.UI.Common;
using Titanhold.UI.SectionInventory;
using UnityEngine;

namespace Titanhold.UI.Containers
{
    public sealed class ItemContainerWindow : MonoBehaviour, IItemSlotEventSource
    {
        [SerializeField] private MonoBehaviour containerOwnerBehaviour;
        [SerializeField] private InventorySectionGridView gridView;
        [SerializeField] private ItemContainerCategoryTabButton[] tabButtons;
        [SerializeField] private bool showMiscTab;
        [SerializeField] private global::ItemCategory selectedCategory = global::ItemCategory.Equipment;

        private global::IItemContainerOwner owner;
        private global::IItemContainerOwner subscribedOwner;
        private bool loggedMissingOwner;
        private bool loggedMissingGridView;
        private bool loggedMissingSection;

        public event Action<global::ItemSlotRef> ItemSlotRightClicked;
        public event Action<IItemDragSourceView, global::ItemSlotRef, ItemDragVisualData> ItemSlotDragStarted;
        public event Action<global::ItemSlotRef> ItemSlotDropped;
        public event Action ItemSlotDragEnded;

        public global::IItemContainerOwner Owner
        {
            get
            {
                ResolveOwner();
                return owner;
            }
        }

        public global::ItemCategory SelectedCategory => selectedCategory;

        private void Awake()
        {
            NormalizeSelectedCategory();
            InitializeTabs();
        }

        private void OnEnable()
        {
            ResolveOwner();
            SubscribeOwner(owner);
            SubscribeGrid();
            InitializeTabs();
            Refresh();
        }

        private void OnDisable()
        {
            UnsubscribeGrid();
            UnsubscribeOwner();
        }

        private void OnValidate()
        {
            NormalizeSelectedCategory();
        }

        public void SetOwnerBehaviour(MonoBehaviour ownerBehaviour)
        {
            SetOwner(ownerBehaviour as global::IItemContainerOwner);
            containerOwnerBehaviour = ownerBehaviour;
        }

        public void SetOwner(global::IItemContainerOwner newOwner)
        {
            if (ReferenceEquals(owner, newOwner))
            {
                Refresh();
                return;
            }

            if (isActiveAndEnabled)
                UnsubscribeOwner();

            owner = newOwner;

            if (newOwner is MonoBehaviour ownerMonoBehaviour)
                containerOwnerBehaviour = ownerMonoBehaviour;

            if (isActiveAndEnabled)
                SubscribeOwner(owner);

            Refresh();
        }

        [ContextMenu("Refresh")]
        public void Refresh()
        {
            RefreshCurrentSection();
            RefreshTabs();
        }

        public void SelectCategory(global::ItemCategory category)
        {
            if (category == global::ItemCategory.Misc && !showMiscTab)
                return;

            selectedCategory = category;
            Refresh();
        }

        private void RefreshCurrentSection()
        {
            if (gridView == null)
            {
                LogMissingGridView();
                return;
            }

            global::IItemContainerOwner currentOwner = Owner;
            if (currentOwner == null)
            {
                LogMissingOwner();
                gridView.Clear();
                return;
            }

            global::ItemContainerSection section = currentOwner.GetSection(selectedCategory);
            if (section == null)
            {
                LogMissingSection(selectedCategory);
                gridView.Clear();
                return;
            }

            gridView.ShowSection(section);
        }

        private void ResolveOwner()
        {
            if (owner != null && ReferenceEquals(owner, containerOwnerBehaviour))
                return;

            owner = containerOwnerBehaviour as global::IItemContainerOwner;
        }

        private void SubscribeOwner(global::IItemContainerOwner newOwner)
        {
            if (newOwner == null || ReferenceEquals(subscribedOwner, newOwner))
                return;

            UnsubscribeOwner();
            subscribedOwner = newOwner;
            subscribedOwner.Changed += HandleOwnerChanged;
            subscribedOwner.SectionChanged += HandleSectionChanged;
        }

        private void UnsubscribeOwner()
        {
            if (subscribedOwner == null)
                return;

            subscribedOwner.Changed -= HandleOwnerChanged;
            subscribedOwner.SectionChanged -= HandleSectionChanged;
            subscribedOwner = null;
        }

        private void SubscribeGrid()
        {
            if (gridView == null)
                return;

            gridView.SlotRightClicked += HandleSlotRightClicked;
            gridView.SlotDragStartedWithView += HandleSlotDragStartedWithView;
            gridView.SlotDropped += HandleSlotDropped;
            gridView.SlotDragEnded += HandleSlotDragEnded;
        }

        private void UnsubscribeGrid()
        {
            if (gridView == null)
                return;

            gridView.SlotRightClicked -= HandleSlotRightClicked;
            gridView.SlotDragStartedWithView -= HandleSlotDragStartedWithView;
            gridView.SlotDropped -= HandleSlotDropped;
            gridView.SlotDragEnded -= HandleSlotDragEnded;
        }

        private void InitializeTabs()
        {
            if (tabButtons == null)
                return;

            foreach (ItemContainerCategoryTabButton tabButton in tabButtons)
            {
                if (tabButton == null)
                    continue;

                bool isMisc = tabButton.Category == global::ItemCategory.Misc;
                tabButton.Initialize(this);
                tabButton.SetVisible(!isMisc || showMiscTab);
                tabButton.SetSelected(tabButton.Category == selectedCategory);
            }
        }

        private void RefreshTabs()
        {
            if (tabButtons == null)
                return;

            foreach (ItemContainerCategoryTabButton tabButton in tabButtons)
            {
                if (tabButton == null)
                    continue;

                bool isMisc = tabButton.Category == global::ItemCategory.Misc;
                tabButton.SetVisible(!isMisc || showMiscTab);
                tabButton.SetSelected(tabButton.Category == selectedCategory);
            }
        }

        private void HandleOwnerChanged()
        {
            Refresh();
        }

        private void HandleSectionChanged(global::ItemCategory category)
        {
            if (category == selectedCategory)
                Refresh();
        }

        private void HandleSlotRightClicked(global::ItemCategory category, int slotIndex)
        {
            global::ItemSlotRef slotRef = CreateSlotRef(category, slotIndex);
            if (slotRef.IsValid)
                ItemSlotRightClicked?.Invoke(slotRef);
        }

        private void HandleSlotDragStartedWithView(
            InventorySlotView slotView,
            global::ItemCategory category,
            int slotIndex)
        {
            global::ItemSlotRef slotRef = CreateSlotRef(category, slotIndex);
            if (!slotRef.IsValid)
                return;

            global::ItemSlot slot = slotRef.ContainerOwner.GetSlot(category, slotIndex);
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
            global::ItemSlotRef slotRef = CreateSlotRef(category, slotIndex);
            if (slotRef.IsValid)
                ItemSlotDropped?.Invoke(slotRef);
        }

        private void HandleSlotDragEnded()
        {
            ItemSlotDragEnded?.Invoke();
        }

        private global::ItemSlotRef CreateSlotRef(global::ItemCategory category, int slotIndex)
        {
            global::IItemContainerOwner currentOwner = Owner;
            return global::ItemSlotRef.ForContainer(currentOwner, category, slotIndex);
        }

        private void NormalizeSelectedCategory()
        {
            if (selectedCategory == global::ItemCategory.Misc && !showMiscTab)
                selectedCategory = global::ItemCategory.Equipment;
        }

        private void LogMissingOwner()
        {
            if (loggedMissingOwner)
                return;

            Debug.LogWarning($"{nameof(ItemContainerWindow)} requires a container owner behaviour implementing IItemContainerOwner.", this);
            loggedMissingOwner = true;
        }

        private void LogMissingGridView()
        {
            if (loggedMissingGridView)
                return;

            Debug.LogWarning($"{nameof(ItemContainerWindow)} requires an InventorySectionGridView reference.", this);
            loggedMissingGridView = true;
        }

        private void LogMissingSection(global::ItemCategory category)
        {
            if (loggedMissingSection)
                return;

            Debug.LogWarning($"{nameof(ItemContainerWindow)} could not find container section '{category}'.", this);
            loggedMissingSection = true;
        }
    }
}
