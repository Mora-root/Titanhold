using System;
using Titanhold.UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Titanhold.UI.Containers
{
    public sealed class ChestWindowController : MonoBehaviour
    {
        [SerializeField] private ItemContainerWindow chestWindow;
        [SerializeField] private GameObject chestWindowRoot;
        [SerializeField] private Button closeButton;
        [SerializeField] private ItemContainerWindowController linkedInventoryWindowController;
        [SerializeField] private ItemInteractionContext interactionContext;
        [SerializeField] private bool startOpen;
        [SerializeField] private bool closeOnEscape = true;

        private global::ChestInventory activeChest;
        private bool loggedMissingRoot;
        private bool loggedSelfRoot;

        public event Action Opened;
        public event Action Closed;

        public global::ChestInventory ActiveChest => activeChest;

        public bool IsOpen
        {
            get
            {
                GameObject root = ResolveRoot();
                return root != null && root.activeSelf;
            }
        }

        private void Awake()
        {
            ApplyState(startOpen, false);
        }

        private void OnEnable()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
        }

        private void OnDisable()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);
        }

        public void Configure(
            ItemContainerWindow window,
            ItemInteractionContext context,
            ItemContainerWindowController inventoryWindowController = null,
            GameObject root = null)
        {
            chestWindow = window;
            interactionContext = context;
            linkedInventoryWindowController = inventoryWindowController;

            if (root != null)
                chestWindowRoot = root;
        }

        private void Update()
        {
            if (closeOnEscape && IsOpen && Input.GetKeyDown(KeyCode.Escape))
                Close();
        }

        public void Open(global::ChestInventory chest)
        {
            if (chest == null)
                return;

            activeChest = chest;
            chestWindow?.SetOwner((global::IItemContainerOwner)chest);
            interactionContext?.SetContainerMode(ItemInteractionMode.Chest, chest);
            linkedInventoryWindowController?.Open();
            ApplyState(true, true);
        }

        public void Close()
        {
            ApplyState(false, true);
        }

        private void ApplyState(bool open, bool invokeEvents)
        {
            GameObject root = ResolveRoot();
            if (root == null)
            {
                LogMissingRoot();
                return;
            }

            if (!open && root == gameObject)
            {
                LogSelfRoot();
                return;
            }

            bool wasOpen = root.activeSelf;
            if (wasOpen == open)
                return;

            root.SetActive(open);

            if (open)
            {
                chestWindow?.Refresh();
            }
            else
            {
                interactionContext?.ClearIfContainer(activeChest);
                activeChest = null;
            }

            if (!invokeEvents)
                return;

            if (open)
                Opened?.Invoke();
            else
                Closed?.Invoke();
        }

        private GameObject ResolveRoot()
        {
            if (chestWindow != null)
                return chestWindow.gameObject;

            return chestWindowRoot;
        }

        private void LogMissingRoot()
        {
            if (loggedMissingRoot)
                return;

            Debug.LogWarning($"{nameof(ChestWindowController)} requires an ItemContainerWindow or ChestWindowRoot reference.", this);
            loggedMissingRoot = true;
        }

        private void LogSelfRoot()
        {
            if (loggedSelfRoot)
                return;

            Debug.LogWarning($"{nameof(ChestWindowController)} cannot close its own GameObject. Put it on an always-active UI root.", this);
            loggedSelfRoot = true;
        }
    }
}
