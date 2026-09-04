using System;
using Titanhold.UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Titanhold.UI.Equipment
{
    public sealed class CharacterWindowController : MonoBehaviour,
        IEscapePriorityWindow
    {
        [SerializeField] private CharacterEquipmentPanel characterPanel;
        [SerializeField] private GameObject characterWindowRoot;
        [SerializeField] private Button closeButton;
        [SerializeField] private bool startOpen;
        [SerializeField] private KeyCode toggleKey = KeyCode.C;
        [SerializeField] private bool closeOnEscape = true;

        private bool loggedMissingWindowRoot;
        private bool loggedSelfRoot;

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

        public void Configure(CharacterEquipmentPanel panel, GameObject root = null)
        {
            characterPanel = panel;

            if (root != null)
                characterWindowRoot = root;
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
                characterPanel?.RefreshAll();

            if (!invokeEvents)
                return;

            if (open)
                Opened?.Invoke();
            else
                Closed?.Invoke();
        }

        private GameObject ResolveWindowRoot()
        {
            if (characterPanel != null)
                return characterPanel.gameObject;

            return characterWindowRoot;
        }

        private void LogMissingWindowRoot()
        {
            if (loggedMissingWindowRoot)
                return;

            Debug.LogWarning($"{nameof(CharacterWindowController)} requires a CharacterEquipmentPanel or CharacterWindowRoot reference.", this);
            loggedMissingWindowRoot = true;
        }

        private void LogSelfRoot()
        {
            if (loggedSelfRoot)
                return;

            Debug.LogWarning($"{nameof(CharacterWindowController)} cannot close its own GameObject. Put it on an always-active UI root and assign the character window root separately.", this);
            loggedSelfRoot = true;
        }
    }
}
