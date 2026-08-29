using System;
using UnityEngine;
using UnityEngine.UI;

namespace Titanhold.UI.Containers
{
    public sealed class ItemContainerWindowController : MonoBehaviour
    {
        [SerializeField] private ItemContainerWindow window;
        [SerializeField] private GameObject windowRoot;
        [SerializeField] private Button closeButton;
        [SerializeField] private bool startOpen;
        [SerializeField] private bool useToggleKey = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.I;
        [SerializeField] private bool closeOnEscape = true;

        private bool loggedMissingRoot;
        private bool loggedSelfRoot;

        public event Action Opened;
        public event Action Closed;

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

        public void Configure(ItemContainerWindow containerWindow, GameObject root = null)
        {
            window = containerWindow;

            if (root != null)
                windowRoot = root;
        }

        private void Update()
        {
            if (useToggleKey && Input.GetKeyDown(toggleKey))
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
                window?.Refresh();

            if (!invokeEvents)
                return;

            if (open)
                Opened?.Invoke();
            else
                Closed?.Invoke();
        }

        private GameObject ResolveRoot()
        {
            if (window != null)
                return window.gameObject;

            return windowRoot;
        }

        private void LogMissingRoot()
        {
            if (loggedMissingRoot)
                return;

            Debug.LogWarning($"{nameof(ItemContainerWindowController)} requires an ItemContainerWindow or WindowRoot reference.", this);
            loggedMissingRoot = true;
        }

        private void LogSelfRoot()
        {
            if (loggedSelfRoot)
                return;

            Debug.LogWarning($"{nameof(ItemContainerWindowController)} cannot close its own GameObject. Put it on an always-active UI root.", this);
            loggedSelfRoot = true;
        }
    }
}
