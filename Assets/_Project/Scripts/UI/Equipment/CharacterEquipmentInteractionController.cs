using UnityEngine;
using Titanhold.UI.SectionInventory;

namespace Titanhold.UI.Equipment
{
    public sealed class CharacterEquipmentInteractionController : MonoBehaviour
    {
        [SerializeField] private CharacterEquipmentPanel equipmentPanel;
        [SerializeField] private global::PlayerEquipmentRuntime equipmentRuntime;
        [SerializeField] private InventoryDragContext dragContext;

        private bool loggedMissingPanel;
        private bool loggedMissingEquipmentRuntime;
        private bool loggedMissingDragContext;

        private void OnEnable()
        {
            if (equipmentPanel != null)
            {
                equipmentPanel.SlotRightClicked += HandleSlotRightClicked;
                equipmentPanel.SlotDropped += HandleSlotDropped;
            }
            else
            {
                LogMissingPanel();
            }
        }

        private void OnDisable()
        {
            if (equipmentPanel != null)
            {
                equipmentPanel.SlotRightClicked -= HandleSlotRightClicked;
                equipmentPanel.SlotDropped -= HandleSlotDropped;
            }
        }

        private void HandleSlotRightClicked(global::EquipmentSlotId slotId)
        {
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

            global::EquipmentOperationResult result = service.TryUnequipToInventory(slotId);
            Debug.Log(
                $"{nameof(CharacterEquipmentInteractionController)} unequip result: Success={result.Success}, Error={result.Error}, Slot={slotId}",
                this);
        }

        private void HandleSlotDropped(global::EquipmentSlotId slotId)
        {
            if (dragContext == null)
            {
                LogMissingDragContext();
                return;
            }

            if (!dragContext.HasSource)
                return;

            if (equipmentRuntime == null)
            {
                LogMissingEquipmentRuntime();
                dragContext.Clear();
                return;
            }

            global::EquipmentService service = equipmentRuntime.Service;
            if (service == null)
            {
                LogMissingEquipmentRuntime();
                dragContext.Clear();
                return;
            }

            global::EquipmentOperationResult result = service.TryEquipFromInventory(
                dragContext.SourceCategory,
                dragContext.SourceIndex,
                slotId);

            Debug.Log(
                $"{nameof(CharacterEquipmentInteractionController)} inventory-to-equipment drop result: Success={result.Success}, Error={result.Error}, Slot={slotId}",
                this);

            dragContext.Clear();
        }

        private void LogMissingPanel()
        {
            if (loggedMissingPanel)
                return;

            Debug.LogWarning($"{nameof(CharacterEquipmentInteractionController)} requires a CharacterEquipmentPanel reference.", this);
            loggedMissingPanel = true;
        }

        private void LogMissingEquipmentRuntime()
        {
            if (loggedMissingEquipmentRuntime)
                return;

            Debug.LogWarning($"{nameof(CharacterEquipmentInteractionController)} requires a PlayerEquipmentRuntime reference.", this);
            loggedMissingEquipmentRuntime = true;
        }

        private void LogMissingDragContext()
        {
            if (loggedMissingDragContext)
                return;

            Debug.LogWarning($"{nameof(CharacterEquipmentInteractionController)} requires an InventoryDragContext reference for inventory-to-equipment drag.", this);
            loggedMissingDragContext = true;
        }
    }
}
