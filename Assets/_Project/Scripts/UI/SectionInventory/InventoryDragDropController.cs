using UnityEngine;

namespace Titanhold.UI.SectionInventory
{
    public sealed class InventoryDragDropController : MonoBehaviour
    {
        [SerializeField] private PlayerInventoryWindow inventoryWindow;
        [SerializeField] private global::PlayerInventory playerInventory;
        [SerializeField] private global::PlayerEquipmentRuntime equipmentRuntime;
        [SerializeField] private ItemDragContext dragContext;

        private bool loggedMissingWindow;
        private bool loggedMissingInventory;
        private bool loggedMissingEquipmentRuntime;
        private bool loggedMissingDragContext;

        private void OnEnable()
        {
            if (inventoryWindow != null)
            {
                inventoryWindow.SlotDragStarted += HandleSlotDragStarted;
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
                inventoryWindow.SlotDragStarted -= HandleSlotDragStarted;
                inventoryWindow.SlotDropped -= HandleSlotDropped;
                inventoryWindow.SlotDragEnded -= HandleSlotDragEnded;
            }

            ClearSource();
        }

        private void HandleSlotDragStarted(global::ItemCategory category, int slotIndex)
        {
            if (dragContext != null)
                dragContext.BeginInventory(category, slotIndex);
            else
                LogMissingDragContext();
        }

        private void HandleSlotDropped(global::ItemCategory targetCategory, int targetIndex)
        {
            if (dragContext == null)
            {
                LogMissingDragContext();
                return;
            }

            if (!dragContext.HasSource)
                return;

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
        }

        private void ClearSource()
        {
            if (dragContext != null)
                dragContext.Clear();
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
