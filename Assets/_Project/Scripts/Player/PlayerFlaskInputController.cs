using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerFlaskController))]
public sealed class PlayerFlaskInputController : MonoBehaviour
{
    [SerializeField] private PlayerFlaskController flasks;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private KeyCode healthFlaskKey = KeyCode.Q;
    [SerializeField] private KeyCode resourceFlaskKey = KeyCode.E;

    public PlayerFlaskController Flasks => flasks;
    public PlayerInput PlayerInput => playerInput;
    public KeyCode HealthFlaskKey => healthFlaskKey;
    public KeyCode ResourceFlaskKey => resourceFlaskKey;

#if UNITY_EDITOR
    public void ConfigureForEditor(
        PlayerFlaskController configuredFlasks,
        PlayerInput configuredInput,
        KeyCode configuredHealthKey,
        KeyCode configuredResourceKey)
    {
        flasks = configuredFlasks;
        playerInput = configuredInput;
        healthFlaskKey = configuredHealthKey;
        resourceFlaskKey = configuredResourceKey;
    }
#endif

    private void Awake()
    {
        flasks ??= GetComponent<PlayerFlaskController>();
        playerInput ??= GetComponent<PlayerInput>();
    }

    private void Update()
    {
        if (flasks == null ||
            playerInput == null ||
            !playerInput.GameplayInputEnabled)
        {
            return;
        }

        if (UnityEngine.Input.GetKeyDown(healthFlaskKey))
            flasks.TryUseSlot(0);
        if (UnityEngine.Input.GetKeyDown(resourceFlaskKey))
            flasks.TryUseSlot(1);
    }
}
