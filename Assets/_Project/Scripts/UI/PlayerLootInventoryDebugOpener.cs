using UnityEngine;

// Temporary prototype helper. Do not use in production.
public sealed class PlayerLootInventoryDebugOpener : MonoBehaviour
{
    [SerializeField] private PlayerLootInventoryPanel panel;
    [SerializeField] private KeyCode toggleKey = KeyCode.I;

    private void Awake()
    {
        panel ??= FindAnyObjectByType<PlayerLootInventoryPanel>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && panel != null && panel.IsOpen)
        {
            panel.Close();
            return;
        }

        if (!Input.GetKeyDown(toggleKey))
            return;

        if (panel == null)
            return;

        if (panel.IsOpen)
        {
            panel.Close();
        }
        else
        {
            panel.Open();
        }
    }
}
