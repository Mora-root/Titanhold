using UnityEngine;

namespace Titanhold.UI.SectionInventory
{
    public sealed class InventoryDragDropController : MonoBehaviour
    {
        [SerializeField] private PlayerInventoryWindow inventoryWindow;
        [SerializeField] private global::PlayerInventory playerInventory;

        private bool hasSource;
        private global::ItemCategory sourceCategory;
        private int sourceIndex = -1;
        private bool loggedMissingWindow;
        private bool loggedMissingInventory;

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
            hasSource = true;
            sourceCategory = category;
            sourceIndex = slotIndex;
        }

        private void HandleSlotDropped(global::ItemCategory targetCategory, int targetIndex)
        {
            if (!hasSource)
                return;

            if (playerInventory == null)
            {
                LogMissingInventory();
                ClearSource();
                return;
            }

            global::ItemTransferResult result = playerInventory.TryTransfer(
                sourceCategory,
                sourceIndex,
                targetCategory,
                targetIndex);

            Debug.Log(
                $"{nameof(InventoryDragDropController)} transfer result: Success={result.Success}, Error={result.Error}, MovedAmount={result.MovedAmount}",
                this);

            ClearSource();
        }

        private void HandleSlotDragEnded()
        {
            ClearSource();
        }

        private void ClearSource()
        {
            hasSource = false;
            sourceCategory = default;
            sourceIndex = -1;
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
    }
}
