using UnityEngine;

public sealed class PlayerEquipmentRuntime : MonoBehaviour, IEquipmentRuntimeOwner
{
    [SerializeField] private PlayerInventory playerInventory;

    private CharacterEquipment equipment;
    private EquipmentService equipmentService;

    public CharacterEquipment Equipment
    {
        get
        {
            EnsureInitialized();
            return equipment;
        }
    }

    public EquipmentService Service
    {
        get
        {
            EnsureInitialized();
            return equipmentService;
        }
    }

    private void Awake()
    {
        EnsureInitialized();
    }

    public void SetPlayerInventory(PlayerInventory inventory)
    {
        if (ReferenceEquals(playerInventory, inventory))
            return;

        playerInventory = inventory;
        equipmentService = null;
        EnsureInitialized();
    }

    public void EnsureInitialized()
    {
        if (playerInventory == null)
            playerInventory = GetComponent<PlayerInventory>();

        equipment ??= new CharacterEquipment();

        if (equipmentService == null)
            equipmentService = new EquipmentService(playerInventory, equipment);
    }

    [ContextMenu("Debug Equip First Equipment Slot")]
    private void DebugEquipFirstEquipmentSlot()
    {
        EnsureInitialized();

        EquipmentOperationResult result = equipmentService.TryEquipFromInventory(ItemCategory.Equipment, 0);
        Debug.Log(FormatResult(nameof(DebugEquipFirstEquipmentSlot), result), this);
    }

    [ContextMenu("Debug Unequip MainHand")]
    private void DebugUnequipMainHand()
    {
        EnsureInitialized();

        EquipmentOperationResult result = equipmentService.TryUnequipToInventory(EquipmentSlotId.MainHand);
        Debug.Log(FormatResult(nameof(DebugUnequipMainHand), result), this);
    }

    private static string FormatResult(string operationName, EquipmentOperationResult result)
    {
        string itemId = result.EquippedInstance != null ? result.EquippedInstance.InstanceId : "none";
        return $"{operationName}: Success={result.Success}, Error={result.Error}, TargetSlot={result.TargetSlot}, Item={itemId}, Message={result.Message}";
    }
}
