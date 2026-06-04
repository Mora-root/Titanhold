using UnityEngine;

// Temporary prototype helper. Do not use in production.
public sealed class PlayerLootInventoryDebugOpener : MonoBehaviour
{
    [SerializeField] private PlayerInventoryWindow window;
    [SerializeField] private KeyCode toggleKey = KeyCode.I;

    private void Awake()
    {
        window ??= FindAnyObjectByType<PlayerInventoryWindow>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && window != null && window.IsOpen)
        {
            window.Close();
            return;
        }

        if (!Input.GetKeyDown(toggleKey))
            return;

        if (window == null)
            return;

        if (window.IsOpen)
        {
            window.Close();
        }
        else
        {
            window.Open();
        }
    }
}
