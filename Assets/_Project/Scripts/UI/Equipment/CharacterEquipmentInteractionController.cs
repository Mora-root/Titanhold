using UnityEngine;
using Titanhold.UI.Common;
using Titanhold.UI.SectionInventory;

namespace Titanhold.UI.Equipment
{
    public sealed class CharacterEquipmentInteractionController : MonoBehaviour
    {
        [SerializeField] private CharacterEquipmentPanel equipmentPanel;
        [SerializeField] private global::PlayerEquipmentRuntime equipmentRuntime;
        [SerializeField] private ItemDragContext dragContext;
        [SerializeField] private ItemDragVisual dragVisual;

        private bool loggedMissingPanel;
        private bool loggedMissingEquipmentRuntime;
        private bool loggedMissingDragContext;
        private CharacterEquipmentSlotView hiddenSourceSlotView;

        private void OnEnable()
        {
            if (equipmentPanel != null)
            {
                equipmentPanel.SlotRightClicked += HandleSlotRightClicked;
                equipmentPanel.SlotDropped += HandleSlotDropped;
                equipmentPanel.SlotDragStartedWithView += HandleSlotDragStarted;
                equipmentPanel.SlotDragEnded += HandleSlotDragEnded;
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
                equipmentPanel.SlotDragStartedWithView -= HandleSlotDragStarted;
                equipmentPanel.SlotDragEnded -= HandleSlotDragEnded;
            }

            RestoreHiddenSourceSlot();
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

            if (dragContext.SourceKind != ItemDragSourceKind.InventorySlot)
            {
                dragContext.Clear();
                RestoreHiddenSourceSlot();
                HideDragVisual();
                return;
            }

            if (equipmentRuntime == null)
            {
                LogMissingEquipmentRuntime();
                dragContext.Clear();
                RestoreHiddenSourceSlot();
                HideDragVisual();
                return;
            }

            global::EquipmentService service = equipmentRuntime.Service;
            if (service == null)
            {
                LogMissingEquipmentRuntime();
                dragContext.Clear();
                RestoreHiddenSourceSlot();
                HideDragVisual();
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
            RestoreHiddenSourceSlot();
            HideDragVisual();
        }

        private void HandleSlotDragStarted(CharacterEquipmentSlotView slotView, global::EquipmentSlotId slotId)
        {
            hiddenSourceSlotView = slotView;
            hiddenSourceSlotView?.SetDragHidden(true);

            if (dragContext != null)
                dragContext.BeginEquipment(slotId);
            else
                LogMissingDragContext();

            ShowEquipmentDragVisual(slotId);
        }

        private void HandleSlotDragEnded()
        {
            if (dragContext != null)
                dragContext.Clear();

            RestoreHiddenSourceSlot();
            HideDragVisual();
        }

        private void ShowEquipmentDragVisual(global::EquipmentSlotId slotId)
        {
            if (dragVisual == null || equipmentRuntime == null || equipmentRuntime.Equipment == null)
                return;

            global::ItemInstance item = equipmentRuntime.Equipment.GetEquipped(slotId);
            global::ItemDefinition definition = item?.Definition;
            if (definition == null)
                return;

            dragVisual.Show(definition.Icon, 1);
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

            Debug.LogWarning($"{nameof(CharacterEquipmentInteractionController)} requires an ItemDragContext reference.", this);
            loggedMissingDragContext = true;
        }
    }
}
