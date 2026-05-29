using UnityEngine;

// Temporary prototype helper. Do not use in production.
public sealed class CampCoreDebugDamage : MonoBehaviour
{
    [SerializeField] private CampCore campCore;
    [SerializeField] private KeyCode killKey = KeyCode.K;

    private void Awake()
    {
        campCore ??= FindAnyObjectByType<CampCore>();
    }

    private void Update()
    {
        if (!Input.GetKeyDown(killKey))
            return;

        if (campCore == null || campCore.Health == null)
            return;

        campCore.Health.TakeDamage(campCore.Health.CurrentHealth);
    }
}
