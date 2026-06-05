using UnityEngine;

// Temporary prototype helper. Do not use in production.
public sealed class PlayerEquipmentDebugEquipper : MonoBehaviour
{
    [SerializeField] private PlayerEquipment playerEquipment;
    [SerializeField] private ItemDefinition itemToEquip;
    [SerializeField] private KeyCode equipKey = KeyCode.O;
    [SerializeField] private KeyCode unequipKey = KeyCode.P;

    private void Awake()
    {
        playerEquipment ??= GetComponent<PlayerEquipment>();
        playerEquipment ??= FindAnyObjectByType<PlayerEquipment>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(equipKey))
        {
            if (playerEquipment != null && itemToEquip != null)
                playerEquipment.AutoEquip(itemToEquip);
        }

        if (Input.GetKeyDown(unequipKey))
        {
            if (playerEquipment != null && itemToEquip != null)
                playerEquipment.Unequip(playerEquipment.GetPreferredSlot(itemToEquip));
        }
    }
}
