using UnityEngine;

// Temporary prototype helper. Do not use in production.
public sealed class CampBrokenDebugRestorer : MonoBehaviour
{
    [SerializeField] private CampBrokenState campBrokenState;
    [SerializeField] private KeyCode restoreKey = KeyCode.R;

    private void Awake()
    {
        campBrokenState ??= GetComponent<CampBrokenState>();
        campBrokenState ??= FindAnyObjectByType<CampBrokenState>();
    }

    private void Update()
    {
        if (!Input.GetKeyDown(restoreKey))
            return;

        campBrokenState?.RestoreCamp();
    }
}
