using UnityEngine;
using Titanhold.UI.Common;

namespace Titanhold.UI.SectionInventory
{
    public sealed class InventoryDragDropController : MonoBehaviour
    {
        [SerializeField] private PlayerInventoryWindow inventoryWindow;
        [SerializeField] private global::PlayerInventory playerInventory;
        [SerializeField] private global::PlayerEquipmentRuntime equipmentRuntime;
        [SerializeField] private ItemDragContext dragContext;
        [SerializeField] private ItemDragVisual dragVisual;

        private bool loggedMissingWindow;
        private bool loggedMissingInventory;
        private bool loggedMissingEquipmentRuntime;
        private bool loggedMissingDragContext;
        private InventorySlotView hiddenSourceSlotView;

        private void OnEnable()
        {
            if (inventoryWindow != null)
            {
                inventoryWindow.SlotDragStartedWithView += HandleSlotDragStarted;
                inventoryWindow.SlotDropped += HandleSlotDropped;
                inventoryWindow.SlotDragEnded += HandleSlotDragEnded;
            }
            else
            {
                LogMissingWindow();
            }
        }

        private void OnDisable()
        {
            if (inventoryWindow != null)
            {
                inventoryWindow.SlotDragStartedWithView -= HandleSlotDragStarted;
                inventoryWindow.SlotDropped -= HandleSlotDropped;
                inventoryWindow.SlotDragEnded -= HandleSlotDragEnded;
            }

            RestoreHiddenSourceSlot();
            ClearSource();
        }

        private void HandleSlotDragStarted(InventorySlotView slotView, global::ItemCategory category, int slotIndex)
        {
            hiddenSourceSlotView = slotView;
            hiddenSourceSlotView?.SetDragHidden(true);

            if (dragContext != null)
                dragContext.BeginInventory(category, slotIndex);
            else
                LogMissingDragContext();

            ShowInventoryDragVisual(category, slotIndex);
        }

        private void HandleSlotDropped(global::ItemCategory targetCategory, int targetIndex)
        {
            if (dragContext == null)
            {
                LogMissingDragContext();
                RestoreHiddenSourceSlot();
                HideDragVisual();
                return;
            }

            if (!dragContext.HasSource)
            {
                RestoreHiddenSourceSlot();
                HideDragVisual();
                return;
            }

            switch (dragContext.SourceKind)
            {
                case ItemDragSourceKind.InventorySlot:
                    TransferInventoryToInventory(targetCategory, targetIndex);
                    break;
                case ItemDragSourceKind.EquipmentSlot:
                    TransferEquipmentToInventory(targetCategory, targetIndex);
                    break;
            }

            ClearSource();
            RestoreHiddenSourceSlot();
            HideDragVisual();
        }

        private void TransferInventoryToInventory(global::ItemCategory targetCategory, int targetIndex)
        {
            if (playerInventory == null)
            {
                LogMissingInventory();
                return;
            }

            global::ItemTransferResult result = playerInventory.TryTransfer(
                dragContext.SourceCategory,
                dragContext.SourceIndex,
                targetCategory,
                targetIndex);

            Debug.Log(
                $"{nameof(InventoryDragDropController)} transfer result: Success={result.Success}, Error={result.Error}, MovedAmount={result.MovedAmount}",
                this);
        }

        private void TransferEquipmentToInventory(global::ItemCategory targetCategory, int targetIndex)
        {
            if (equipmentRuntime == null || equipmentRuntime.Service == null)
            {
                LogMissingEquipmentRuntime();
                return;
            }

            global::EquipmentOperationResult result = equipmentRuntime.Service.TryUnequipToInventory(
                dragContext.SourceEquipmentSlotId,
                targetCategory,
                targetIndex);

            Debug.Log(
                $"{nameof(InventoryDragDropController)} equipment-to-inventory drop result: Success={result.Success}, Error={result.Error}, Slot={dragContext.SourceEquipmentSlotId}",
                this);
        }

        private void HandleSlotDragEnded()
        {
            ClearSource();
            RestoreHiddenSourceSlot();
            HideDragVisual();
        }

        private void ClearSource()
        {
            if (dragContext != null)
                dragContext.Clear();
        }

        private void ShowInventoryDragVisual(global::ItemCategory category, int slotIndex)
        {
            if (dragVisual == null || playerInventory == null)
                return;

            global::ItemSlot slot = playerInventory.GetSlot(category, slotIndex);
            global::ItemStack stack = slot?.Stack;
            global::ItemDefinition definition = stack?.Definition;
            if (definition == null)
                return;

            dragVisual.Show(definition.Icon, stack.Amount);
        }

        private void HideDragVisual()
        {
            if (dragVisual != null)
                dragVisual.Hide();
        }

        private void RestoreHiddenSourceSlot()
        {
            if (hiddenSourceSlotView != null)
                hiddenSourceSlotView.SetDragHidden(false);

            hiddenSourceSlotView = null;
        }

        private void LogMissingWindow()
        {
            if (loggedMissingWindow)
                return;

            Debug.LogWarning($"{nameof(InventoryDragDropController)} requires a PlayerInventoryWindow reference.", this);
            loggedMissingWindow = true;
        }

        private void LogMissingInventory()
        {
            if (loggedMissingInventory)
                return;

            Debug.LogWarning($"{nameof(InventoryDragDropController)} requires a PlayerInventory reference.", this);
            loggedMissingInventory = true;
        }

        private void LogMissingEquipmentRuntime()
        {
            if (loggedMissingEquipmentRuntime)
                return;

            Debug.LogWarning($"{nameof(InventoryDragDropController)} requires a PlayerEquipmentRuntime reference for equipment-to-inventory drag.", this);
            loggedMissingEquipmentRuntime = true;
        }

        private void LogMissingDragContext()
        {
            if (loggedMissingDragContext)
                return;

            Debug.LogWarning($"{nameof(InventoryDragDropController)} requires an ItemDragContext reference.", this);
            loggedMissingDragContext = true;
        }
    }
}
