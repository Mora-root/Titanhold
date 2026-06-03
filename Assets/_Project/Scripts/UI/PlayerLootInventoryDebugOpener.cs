using UnityEngine;

// Temporary prototype helper. Do not use in production.
public sealed class PlayerLootInventoryDebugOpener : MonoBehaviour
{
    [SerializeField] private PlayerLootInventoryPanel panel;
    [SerializeField] private KeyCode toggleKey = KeyCode.I;

    private bool isOpen;

    private void Awake()
    {
        panel ??= FindAnyObjectByType<PlayerLootInventoryPanel>();
    }

    private void Update()
    {
        if (!Input.GetKeyDown(toggleKey))
            return;

        if (panel == null)
            return;

        if (isOpen)
        {
            panel.Close();
            isOpen = false;
        }
        else
        {
            panel.Open();
            isOpen = true;
        }
    }
}
