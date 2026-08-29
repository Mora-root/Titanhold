using System;
using UnityEngine;
using UnityEngine.UI;

namespace Titanhold.UI.SectionInventory
{
    public sealed class InventoryWindowController : MonoBehaviour
    {
        [SerializeField] private PlayerInventoryWindow inventoryWindow;
        [SerializeField] private GameObject inventoryWindowRoot;
        [SerializeField] private Button closeButton;
        [SerializeField] private bool startOpen;
        [SerializeField] private KeyCode toggleKey = KeyCode.I;
        [SerializeField] private bool closeOnEscape = true;

        private bool loggedMissingWindowRoot;

        public event Action Opened;
        public event Action Closed;

        public bool IsOpen
        {
            get
            {
                GameObject root = ResolveWindowRoot();
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

        public void Configure(PlayerInventoryWindow window, GameObject root = null)
        {
            inventoryWindow = window;

            if (root != null)
                inventoryWindowRoot = root;
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                Toggle();

            if (closeOnEscape && IsOpen && Input.GetKeyDown(KeyCode.Escape))
                Close();
        }

        public void Open()
        {
            ApplyState(true, true);
        }

        public void Close()
        {
            ApplyState(false, true);
        }

        public void Toggle()
        {
            ApplyState(!IsOpen, true);
        }

        private void ApplyState(bool open, bool invokeEvents)
        {
            GameObject root = ResolveWindowRoot();
            if (root == null)
            {
                LogMissingWindowRoot();
                return;
            }

            bool wasOpen = root.activeSelf;
            if (wasOpen == open)
                return;

            root.SetActive(open);

            if (open)
                inventoryWindow?.Refresh();

            if (!invokeEvents)
                return;

            if (open)
                Opened?.Invoke();
            else
                Closed?.Invoke();
        }

        private GameObject ResolveWindowRoot()
        {
            if (inventoryWindow != null)
                return inventoryWindow.gameObject;

            return inventoryWindowRoot;
        }

        private void LogMissingWindowRoot()
        {
            if (loggedMissingWindowRoot)
                return;

            Debug.LogWarning($"{nameof(InventoryWindowController)} requires a PlayerInventoryWindow or InventoryWindowRoot reference.", this);
            loggedMissingWindowRoot = true;
        }
    }
}
