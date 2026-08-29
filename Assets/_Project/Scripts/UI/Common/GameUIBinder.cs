using Titanhold.UI.Containers;
using Titanhold.UI.Equipment;
using Titanhold.UI.SectionInventory;
using UnityEngine;

namespace Titanhold.UI.Common
{
    public sealed class GameUIBinder : MonoBehaviour
    {
        [Header("Player Root")]
        [SerializeField] private Transform playerRoot;

        [Header("Player Runtime")]
        [SerializeField] private global::PlayerInventory playerInventory;
        [SerializeField] private global::PlayerEquipmentRuntime playerEquipmentRuntime;
        [SerializeField] private global::PlayerGold playerGold;
        [SerializeField] private global::PlayerInput playerInput;
        [SerializeField] private global::PlayerChestInteractionController chestInteractionController;

        [Header("UI Core")]
        [SerializeField] private ItemInteractionContext interactionContext;
        [SerializeField] private ItemInteractionService interactionService;
        [SerializeField] private ItemDragContext itemDragContext;
        [SerializeField] private ItemDragVisual itemDragVisual;
        [SerializeField] private ItemTooltipController itemTooltipController;

        [Header("Player Inventory UI")]
        [SerializeField] private PlayerInventoryWindow playerInventoryWindow;
        [SerializeField] private ItemContainerWindow playerContainerWindow;
        [SerializeField] private InventoryWindowController inventoryWindowController;
        [SerializeField] private ItemContainerWindowController playerContainerWindowController;

        [Header("Character UI")]
        [SerializeField] private CharacterEquipmentPanel characterEquipmentPanel;
        [SerializeField] private CharacterWindowController characterWindowController;

        [Header("Chest UI")]
        [SerializeField] private ItemContainerWindow chestContainerWindow;
        [SerializeField] private ChestWindowController chestWindowController;

        [Header("Views")]
        [SerializeField] private GoldAmountView[] goldAmountViews;

        [Header("Binding")]
        [SerializeField] private bool autoResolveMissingReferences = true;
        [SerializeField] private bool bindOnAwake = true;
        [SerializeField] private bool bindOnStart = true;
        [SerializeField] private bool logWarnings = true;

        private bool loggedMissingPlayerInventory;
        private bool loggedMissingEquipmentRuntime;
        private bool loggedMissingInteractionService;
        private bool loggedMissingInteractionContext;
        private bool loggedMissingDragContext;

        private void Awake()
        {
            if (bindOnAwake)
                Bind();
        }

        private void Start()
        {
            if (bindOnStart)
                Bind();
        }

        [ContextMenu("Bind UI References")]
        public void Bind()
        {
            if (autoResolveMissingReferences)
                ResolveMissingReferences();

            ApplyBindings();
            LogMissingRequiredReferences();
        }

        private void ResolveMissingReferences()
        {
            playerInventory = ResolveFromPlayer(playerInventory);
            playerEquipmentRuntime = ResolveFromPlayer(playerEquipmentRuntime);
            playerGold = ResolveFromPlayer(playerGold);
            playerInput = ResolveFromPlayer(playerInput);
            chestInteractionController = ResolveFromPlayer(chestInteractionController);

            interactionContext = ResolveFromUi(interactionContext);
            interactionService = ResolveFromUi(interactionService);
            itemDragContext = ResolveFromUi(itemDragContext);
            itemDragVisual = ResolveFromUi(itemDragVisual);
            itemTooltipController = ResolveFromUi(itemTooltipController);

            playerInventoryWindow = ResolveFromUi(playerInventoryWindow);
            inventoryWindowController = ResolveFromUi(inventoryWindowController);
            playerContainerWindowController = ResolveFromUi(playerContainerWindowController);
            characterEquipmentPanel = ResolveFromUi(characterEquipmentPanel);
            characterWindowController = ResolveFromUi(characterWindowController);
            chestWindowController = ResolveFromUi(chestWindowController);

            ResolveContainerWindowsByName();

            if (goldAmountViews == null || goldAmountViews.Length == 0)
                goldAmountViews = GetComponentsInChildren<GoldAmountView>(true);
        }

        private void ApplyBindings()
        {
            playerEquipmentRuntime?.SetPlayerInventory(playerInventory);
            playerInput?.SetItemDragContext(itemDragContext);

            playerInventoryWindow?.SetPlayerInventory(playerInventory);
            playerContainerWindow?.SetOwner(playerInventory);
            characterEquipmentPanel?.SetEquipmentRuntime(playerEquipmentRuntime);

            inventoryWindowController?.Configure(playerInventoryWindow);
            playerContainerWindowController?.Configure(playerContainerWindow);
            characterWindowController?.Configure(characterEquipmentPanel);
            chestWindowController?.Configure(chestContainerWindow, interactionContext, playerContainerWindowController);

            chestInteractionController?.Configure(
                playerContainerWindowController,
                inventoryWindowController,
                chestWindowController,
                playerRoot);

            if (goldAmountViews != null)
            {
                foreach (GoldAmountView goldAmountView in goldAmountViews)
                {
                    goldAmountView?.SetPlayerGold(playerGold);
                }
            }

            BindTooltipController();

            interactionService?.Configure(
                playerInventory,
                playerEquipmentRuntime,
                interactionContext,
                itemDragContext,
                itemDragVisual);

            interactionService?.RefreshEventSourceSubscriptions();
        }

        private void BindTooltipController()
        {
            if (itemTooltipController == null)
                return;

            foreach (InventorySlotView slotView in GetComponentsInChildren<InventorySlotView>(true))
            {
                slotView?.SetTooltipController(itemTooltipController);
            }

            foreach (CharacterEquipmentSlotView slotView in GetComponentsInChildren<CharacterEquipmentSlotView>(true))
            {
                slotView?.SetTooltipController(itemTooltipController);
            }
        }

        private T ResolveFromPlayer<T>(T current) where T : Component
        {
            if (current != null || playerRoot == null)
                return current;

            T component = playerRoot.GetComponent<T>();
            if (component != null)
                return component;

            return playerRoot.GetComponentInChildren<T>(true);
        }

        private T ResolveFromUi<T>(T current) where T : Component
        {
            if (current != null)
                return current;

            return GetComponentInChildren<T>(true);
        }

        private void ResolveContainerWindowsByName()
        {
            if (playerContainerWindow != null && chestContainerWindow != null)
                return;

            ItemContainerWindow[] windows = GetComponentsInChildren<ItemContainerWindow>(true);
            foreach (ItemContainerWindow window in windows)
            {
                if (window == null)
                    continue;

                string windowName = window.name;
                if (playerContainerWindow == null &&
                    windowName.IndexOf("Inventory", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    playerContainerWindow = window;
                    continue;
                }

                if (chestContainerWindow == null &&
                    windowName.IndexOf("Chest", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    chestContainerWindow = window;
                }
            }
        }

        private void LogMissingRequiredReferences()
        {
            if (!logWarnings)
                return;

            LogMissing(playerInventory == null, ref loggedMissingPlayerInventory, "PlayerInventory");
            LogMissing(playerEquipmentRuntime == null, ref loggedMissingEquipmentRuntime, "PlayerEquipmentRuntime");
            LogMissing(interactionService == null, ref loggedMissingInteractionService, "ItemInteractionService");
            LogMissing(interactionContext == null, ref loggedMissingInteractionContext, "ItemInteractionContext");
            LogMissing(itemDragContext == null, ref loggedMissingDragContext, "ItemDragContext");
        }

        private void LogMissing(bool isMissing, ref bool logged, string label)
        {
            if (!isMissing || logged)
                return;

            Debug.LogWarning($"{nameof(GameUIBinder)} could not resolve {label}. Assign it explicitly or set Player Root/UI children correctly.", this);
            logged = true;
        }
    }
}
