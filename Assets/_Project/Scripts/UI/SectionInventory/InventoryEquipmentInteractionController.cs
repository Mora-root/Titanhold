using UnityEngine;

namespace Titanhold.UI.SectionInventory
{
    public sealed class InventoryEquipmentInteractionController : MonoBehaviour
    {
        [SerializeField] private PlayerInventoryWindow inventoryWindow;
        [SerializeField] private global::PlayerEquipmentRuntime equipmentRuntime;

        private bool loggedMissingWindow;
        private bool loggedMissingEquipmentRuntime;

        private void OnEnable()
        {
            if (inventoryWindow != null)
            {
                inventoryWindow.SlotRightClicked += HandleSlotRightClicked;
            }
            else
            {
                LogMissingWindow();
            }
        }

        private void OnDisable()
        {
            if (inventoryWindow != null)
                inventoryWindow.SlotRightClicked -= HandleSlotRightClicked;
        }

        private void HandleSlotRightClicked(global::ItemCategory category, int slotIndex)
        {
            if (category != global::ItemCategory.Equipment)
                return;

            if (equipmentRuntime == null)
            {
                LogMissingEquipmentRuntime();
                return;
            }

            global::EquipmentService service = equipmentRuntime.Service;
            if (service == null)
            {
                LogMissingEquipmentRuntime();
                return;
            }

            global::EquipmentOperationResult result = service.TryEquipFromInventory(category, slotIndex);
            Debug.Log(
                $"{nameof(InventoryEquipmentInteractionController)} equip result: Success={result.Success}, Error={result.Error}, TargetSlot={result.TargetSlot}",
                this);
        }

        private void LogMissingWindow()
        {
            if (loggedMissingWindow)
                return;

            Debug.LogWarning($"{nameof(InventoryEquipmentInteractionController)} requires a PlayerInventoryWindow reference.", this);
            loggedMissingWindow = true;
        }

        private void LogMissingEquipmentRuntime()
        {
            if (loggedMissingEquipmentRuntime)
                return;

            Debug.LogWarning($"{nameof(InventoryEquipmentInteractionController)} requires a PlayerEquipmentRuntime reference.", this);
            loggedMissingEquipmentRuntime = true;
        }
    }
}
