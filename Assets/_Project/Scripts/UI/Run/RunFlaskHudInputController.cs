using Titanhold.Combat.Flasks;
using Titanhold.Session;
using UnityEngine;

namespace Titanhold.UI.Run
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RunFlaskHudView))]
    public sealed class RunFlaskHudInputController : MonoBehaviour
    {
        [SerializeField] private RunSceneSessionEntryPoint sessionEntryPoint;
        [SerializeField] private RunFlaskHudView view;
        [SerializeField] private string playerId = "player:local";

        private PlayerFlaskController flasks;
        private PlayerInput playerInput;
        private bool subscribed;

        public RunSceneSessionEntryPoint SessionEntryPoint => sessionEntryPoint;
        public RunFlaskHudView View => view;
        public string PlayerId => playerId;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            RunSceneSessionEntryPoint configuredEntryPoint,
            RunFlaskHudView configuredView,
            string configuredPlayerId)
        {
            Unsubscribe();
            sessionEntryPoint = configuredEntryPoint;
            view = configuredView;
            playerId = configuredPlayerId?.Trim() ?? string.Empty;
            flasks = null;
            playerInput = null;
            if (isActiveAndEnabled)
                Subscribe();
        }
#endif

        private void Awake()
        {
            view ??= GetComponent<RunFlaskHudView>();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            flasks = null;
            playerInput = null;
        }

        public FlaskUseResult TryUseSlot(int slotIndex)
        {
            if (!TryResolvePlayer() ||
                playerInput == null ||
                !playerInput.GameplayInputEnabled)
            {
                return new FlaskUseResult(
                    FlaskUseStatus.UserUnavailable,
                    slotIndex,
                    string.Empty,
                    0f);
            }

            return flasks.TryUseSlot(slotIndex);
        }

        private bool TryResolvePlayer()
        {
            if (flasks != null &&
                playerInput != null &&
                flasks.gameObject.activeInHierarchy)
            {
                return true;
            }

            flasks = RunFlaskHudPresenter.ResolveFlasks(
                sessionEntryPoint,
                playerId);
            playerInput = flasks != null
                ? flasks.GetComponent<PlayerInput>()
                : null;
            return flasks != null && playerInput != null;
        }

        private void Subscribe()
        {
            view ??= GetComponent<RunFlaskHudView>();
            if (view == null || subscribed)
                return;

            view.SlotPressed += HandleSlotPressed;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (view != null && subscribed)
                view.SlotPressed -= HandleSlotPressed;
            subscribed = false;
        }

        private void HandleSlotPressed(int slotIndex)
        {
            TryUseSlot(slotIndex);
        }
    }
}
