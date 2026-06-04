using UnityEngine;

// Temporary prototype helper. Do not use in production.
public sealed class PlayerEquipmentPanelDebugOpener : MonoBehaviour
{
    [SerializeField] private PlayerEquipmentPanel panel;
    [SerializeField] private KeyCode toggleKey = KeyCode.C;

    private void Awake()
    {
        panel ??= FindAnyObjectByType<PlayerEquipmentPanel>();
    }

    private void Update()
    {
        if (!Input.GetKeyDown(toggleKey))
            return;

        if (panel == null)
            return;

        if (panel.IsOpen)
            panel.Close();
        else
            panel.Open();
    }
}
