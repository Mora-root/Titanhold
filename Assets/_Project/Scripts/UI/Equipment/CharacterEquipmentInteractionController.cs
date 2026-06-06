using UnityEngine;

namespace Titanhold.UI.Equipment
{
    public sealed class CharacterEquipmentInteractionController : MonoBehaviour
    {
        [SerializeField] private CharacterEquipmentPanel equipmentPanel;
        [SerializeField] private global::PlayerEquipmentRuntime equipmentRuntime;

        private bool loggedMissingPanel;
        private bool loggedMissingEquipmentRuntime;

        private void OnEnable()
        {
            if (equipmentPanel != null)
            {
                equipmentPanel.SlotRightClicked += HandleSlotRightClicked;
            }
            else
            {
                LogMissingPanel();
            }
        }

        private void OnDisable()
        {
            if (equipmentPanel != null)
                equipmentPanel.SlotRightClicked -= HandleSlotRightClicked;
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
    }
}
