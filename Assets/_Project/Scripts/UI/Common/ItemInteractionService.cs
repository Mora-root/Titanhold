using System.Collections.Generic;
using UnityEngine;

namespace Titanhold.UI.Common
{
    public sealed class ItemInteractionService : MonoBehaviour
    {
        [SerializeField] private global::PlayerInventory playerInventory;
        [SerializeField] private global::PlayerEquipmentRuntime playerEquipmentRuntime;
        [SerializeField] private ItemInteractionContext interactionContext;
        [SerializeField] private Titanhold.UI.SectionInventory.ItemDragContext dragContext;
        [SerializeField] private ItemDragVisual dragVisual;
        [SerializeField] private MonoBehaviour[] eventSourceBehaviours;
        [SerializeField] private bool autoDiscoverEventSourcesInChildren = true;

        private readonly List<IItemSlotEventSource> eventSources = new();
        private readonly global::ItemTransferService transferService = new();
        private IItemDragSourceView hiddenSourceView;
        private bool loggedMissingInventory;
        private bool loggedMissingEquipment;
        private bool loggedMissingDragContext;
        private bool loggedMissingEventSources;

        public void Configure(
            global::PlayerInventory inventory,
            global::PlayerEquipmentRuntime equipmentRuntime,
            ItemInteractionContext context,
            Titanhold.UI.SectionInventory.ItemDragContext drag,
            ItemDragVisual visual)
        {
            playerInventory = inventory;
            playerEquipmentRuntime = equipmentRuntime;
            interactionContext = context;
            dragContext = drag;
            dragVisual = visual;
        }

        public void RefreshEventSourceSubscriptions()
        {
            if (!isActiveAndEnabled)
                return;

            UnsubscribeEventSources();
            SubscribeEventSources();
        }

        private void OnEnable()
        {
            SubscribeEventSources();
        }

        private void OnDisable()
        {
            UnsubscribeEventSources();
            EndDrag();
        }

        public void RightClick(global::ItemSlotRef slotRef)
        {
            if (!slotRef.IsValid)
                return;

            if (TryHandleContextRightClick(slotRef))
                return;

            if (slotRef.IsEquipmentSlot)
            {
                UnequipToPlayerInventory(slotRef);
                return;
            }

            if (slotRef.IsContainerSlot &&
                IsPlayerInventoryOwner(slotRef.ContainerOwner) &&
                slotRef.Category == global::ItemCategory.Equipment)
            {
                EquipFromPlayerInventory(slotRef, null);
            }
        }

        public void BeginDrag(
            IItemDragSourceView sourceView,
            global::ItemSlotRef source,
            ItemDragVisualData visualData)
        {
            if (!source.IsValid)
                return;

            RestoreHiddenSourceView();
            hiddenSourceView = sourceView;
            hiddenSourceView?.SetDragHidden(true);

            if (dragContext != null)
                dragContext.Begin(source);
            else
                LogMissingDragContext();

            if (dragVisual != null)
                dragVisual.Show(visualData.Icon, visualData.Amount);
        }

        public void Drop(global::ItemSlotRef target)
        {
            if (dragContext == null)
            {
                LogMissingDragContext();
                EndDrag();
                return;
            }

            global::ItemSlotRef source = dragContext.SourceRef;
            if (!source.IsValid || !target.IsValid)
            {
                EndDrag();
                return;
            }

            if (source.IsContainerSlot && target.IsContainerSlot)
            {
                TransferContainerToContainer(source, target);
            }
            else if (source.IsContainerSlot && target.IsEquipmentSlot)
            {
                EquipFromPlayerInventory(source, target.EquipmentSlotId);
            }
            else if (source.IsEquipmentSlot && target.IsContainerSlot)
            {
                UnequipToContainerSlot(source, target);
            }
            else if (source.IsEquipmentSlot && target.IsEquipmentSlot)
            {
                SwapEquippedSlots(source, target);
            }

            EndDrag();
        }

        public void EndDrag()
        {
            if (dragContext != null)
                dragContext.Clear();

            RestoreHiddenSourceView();

            if (dragVisual != null)
                dragVisual.Hide();
        }

        private void SubscribeEventSources()
        {
            eventSources.Clear();

            if (eventSourceBehaviours != null)
            {
                foreach (MonoBehaviour behaviour in eventSourceBehaviours)
                {
                    TrySubscribeEventSource(behaviour);
                }
            }

            if (autoDiscoverEventSourcesInChildren)
            {
                MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);
                foreach (MonoBehaviour behaviour in behaviours)
                {
                    TrySubscribeEventSource(behaviour);
                }
            }

            if (eventSources.Count == 0)
                LogMissingEventSources();
        }

        private void UnsubscribeEventSources()
        {
            foreach (IItemSlotEventSource source in eventSources)
            {
                source.ItemSlotRightClicked -= HandleSlotRightClicked;
                source.ItemSlotDragStarted -= HandleSlotDragStarted;
                source.ItemSlotDropped -= HandleSlotDropped;
                source.ItemSlotDragEnded -= HandleSlotDragEnded;
            }

            eventSources.Clear();
        }

        private void HandleSlotRightClicked(global::ItemSlotRef slotRef)
        {
            RightClick(slotRef);
        }

        private void TrySubscribeEventSource(MonoBehaviour behaviour)
        {
            if (behaviour == null || ReferenceEquals(behaviour, this))
                return;

            if (behaviour is not IItemSlotEventSource source)
                return;

            if (eventSources.Contains(source))
                return;

            source.ItemSlotRightClicked += HandleSlotRightClicked;
            source.ItemSlotDragStarted += HandleSlotDragStarted;
            source.ItemSlotDropped += HandleSlotDropped;
            source.ItemSlotDragEnded += HandleSlotDragEnded;
            eventSources.Add(source);
        }

        private void HandleSlotDragStarted(
            IItemDragSourceView sourceView,
            global::ItemSlotRef source,
            ItemDragVisualData visualData)
        {
            BeginDrag(sourceView, source, visualData);
        }

        private void HandleSlotDropped(global::ItemSlotRef target)
        {
            Drop(target);
        }

        private void HandleSlotDragEnded()
        {
            EndDrag();
        }

        private bool TryHandleContextRightClick(global::ItemSlotRef slotRef)
        {
            if (interactionContext == null || !interactionContext.HasActiveContainer)
                return false;

            if (interactionContext.Mode != ItemInteractionMode.Chest &&
                interactionContext.Mode != ItemInteractionMode.Stash &&
                interactionContext.Mode != ItemInteractionMode.Loot)
            {
                return false;
            }

            if (!slotRef.IsContainerSlot)
                return false;

            global::IItemContainerOwner activeContainer = interactionContext.ActiveContainer;

            if (ReferenceEquals(slotRef.ContainerOwner, activeContainer))
            {
                if (playerInventory == null)
                {
                    LogMissingInventory();
                    return true;
                }

                QuickTransfer(slotRef, playerInventory);
                return true;
            }

            if (IsPlayerInventoryOwner(slotRef.ContainerOwner))
            {
                QuickTransfer(slotRef, activeContainer);
                return true;
            }

            return false;
        }

        private void TransferContainerToContainer(global::ItemSlotRef source, global::ItemSlotRef target)
        {
            if (source.ContainerOwner == null || target.ContainerOwner == null)
                return;

            global::ItemTransferResult result = transferService.TryTransfer(
                ToAddress(source),
                ToAddress(target));

            if (result.Success)
                NotifyTransfer(source.ContainerOwner, target.ContainerOwner, source.Category, target.Category);

            Debug.Log(
                $"{nameof(ItemInteractionService)} container transfer: Success={result.Success}, Error={result.Error}, MovedAmount={result.MovedAmount}",
                this);
        }

        private void QuickTransfer(global::ItemSlotRef source, global::IItemContainerOwner targetOwner)
        {
            if (!source.IsContainerSlot || targetOwner == null)
                return;

            if (!TryFindQuickTransferTarget(source, targetOwner, out global::ItemSlotRef target))
            {
                Debug.Log($"{nameof(ItemInteractionService)} found no quick-transfer target slot.", this);
                return;
            }

            TransferContainerToContainer(source, target);
        }

        private bool TryFindQuickTransferTarget(
            global::ItemSlotRef source,
            global::IItemContainerOwner targetOwner,
            out global::ItemSlotRef target)
        {
            target = global::ItemSlotRef.None;

            global::ItemSlot sourceSlot = source.ContainerOwner.GetSlot(source.Category, source.SlotIndex);
            global::ItemStack sourceStack = sourceSlot?.Stack;
            if (sourceStack == null || sourceStack.Definition == null)
                return false;

            global::ItemContainerSection targetSection = targetOwner.GetSection(source.Category);
            if (targetSection?.Slots == null)
                return false;

            if (sourceStack.Definition.MaxStack > 1 && sourceStack.Instance == null)
            {
                for (int i = 0; i < targetSection.Slots.Length; i++)
                {
                    global::ItemSlot slot = targetSection.GetSlot(i);
                    if (slot == null || slot.IsEmpty || slot.Stack == null || slot.Stack.IsFull)
                        continue;

                    if (!slot.Stack.CanStackWith(sourceStack))
                        continue;

                    target = global::ItemSlotRef.ForContainer(targetOwner, source.Category, i);
                    return true;
                }
            }

            for (int i = 0; i < targetSection.Slots.Length; i++)
            {
                global::ItemSlot slot = targetSection.GetSlot(i);
                if (slot == null || !slot.IsEmpty)
                    continue;

                target = global::ItemSlotRef.ForContainer(targetOwner, source.Category, i);
                return true;
            }

            return false;
        }

        private void EquipFromPlayerInventory(global::ItemSlotRef source, global::EquipmentSlotId? preferredSlot)
        {
            if (!source.IsContainerSlot || !IsPlayerInventoryOwner(source.ContainerOwner))
                return;

            if (playerEquipmentRuntime == null || playerEquipmentRuntime.Service == null)
            {
                LogMissingEquipment();
                return;
            }

            global::EquipmentOperationResult result = preferredSlot.HasValue
                ? playerEquipmentRuntime.Service.TryEquipFromInventory(source.Category, source.SlotIndex, preferredSlot.Value)
                : playerEquipmentRuntime.Service.TryEquipFromInventory(source.Category, source.SlotIndex);

            Debug.Log(
                $"{nameof(ItemInteractionService)} equip: Success={result.Success}, Error={result.Error}, TargetSlot={result.TargetSlot}",
                this);
        }

        private void UnequipToPlayerInventory(global::ItemSlotRef source)
        {
            if (!source.IsEquipmentSlot)
                return;

            global::EquipmentService service = ResolveEquipmentService(source.EquipmentOwner);
            if (service == null)
                return;

            global::EquipmentOperationResult result = service.TryUnequipToInventory(source.EquipmentSlotId);
            Debug.Log(
                $"{nameof(ItemInteractionService)} unequip: Success={result.Success}, Error={result.Error}, Slot={source.EquipmentSlotId}",
                this);
        }

        private void UnequipToContainerSlot(global::ItemSlotRef source, global::ItemSlotRef target)
        {
            if (!source.IsEquipmentSlot || !target.IsContainerSlot)
                return;

            if (!IsPlayerInventoryOwner(target.ContainerOwner))
                return;

            global::EquipmentService service = ResolveEquipmentService(source.EquipmentOwner);
            if (service == null)
                return;

            global::EquipmentOperationResult result = service.TryUnequipToInventory(
                source.EquipmentSlotId,
                target.Category,
                target.SlotIndex);

            Debug.Log(
                $"{nameof(ItemInteractionService)} unequip-to-slot: Success={result.Success}, Error={result.Error}, Slot={source.EquipmentSlotId}",
                this);
        }

        private void SwapEquippedSlots(global::ItemSlotRef source, global::ItemSlotRef target)
        {
            global::EquipmentService service = ResolveEquipmentService(source.EquipmentOwner);
            if (service == null || !ReferenceEquals(source.EquipmentOwner, target.EquipmentOwner))
                return;

            global::EquipmentOperationResult result = service.TrySwapEquippedSlots(
                source.EquipmentSlotId,
                target.EquipmentSlotId);
            Debug.Log(
                $"{nameof(ItemInteractionService)} equipment swap: Success={result.Success}, Error={result.Error}, TargetSlot={result.TargetSlot}",
                this);
        }

        private global::EquipmentService ResolveEquipmentService(global::IEquipmentRuntimeOwner owner)
        {
            global::EquipmentService service = owner?.Service ?? playerEquipmentRuntime?.Service;
            if (service == null)
                LogMissingEquipment();

            return service;
        }

        private bool IsPlayerInventoryOwner(global::IItemContainerOwner owner)
        {
            return owner != null && ReferenceEquals(owner, playerInventory);
        }

        private global::ItemSlotAddress ToAddress(global::ItemSlotRef slotRef)
        {
            return new global::ItemSlotAddress(
                slotRef.ContainerOwner.Container,
                slotRef.Category,
                slotRef.SlotIndex);
        }

        private static void NotifyTransfer(
            global::IItemContainerOwner sourceOwner,
            global::IItemContainerOwner targetOwner,
            global::ItemCategory sourceCategory,
            global::ItemCategory targetCategory)
        {
            sourceOwner?.NotifyTransferChanged(sourceCategory, targetCategory);

            if (!ReferenceEquals(sourceOwner, targetOwner))
                targetOwner?.NotifyTransferChanged(sourceCategory, targetCategory);
        }

        private void RestoreHiddenSourceView()
        {
            hiddenSourceView?.SetDragHidden(false);
            hiddenSourceView = null;
        }

        private void LogMissingInventory()
        {
            if (loggedMissingInventory)
                return;

            Debug.LogWarning($"{nameof(ItemInteractionService)} requires a PlayerInventory reference.", this);
            loggedMissingInventory = true;
        }

        private void LogMissingEquipment()
        {
            if (loggedMissingEquipment)
                return;

            Debug.LogWarning($"{nameof(ItemInteractionService)} requires a PlayerEquipmentRuntime reference.", this);
            loggedMissingEquipment = true;
        }

        private void LogMissingDragContext()
        {
            if (loggedMissingDragContext)
                return;

            Debug.LogWarning($"{nameof(ItemInteractionService)} requires an ItemDragContext reference.", this);
            loggedMissingDragContext = true;
        }

        private void LogMissingEventSources()
        {
            if (loggedMissingEventSources)
                return;

            Debug.LogWarning($"{nameof(ItemInteractionService)} found no item slot event sources. Assign Event Source Behaviours or place inventory/chest/equipment windows under this UI root.", this);
            loggedMissingEventSources = true;
        }
    }
}
