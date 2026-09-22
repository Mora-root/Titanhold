using System;
using Titanhold.Session;
using UnityEngine;

namespace Titanhold.UI.Run
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RunCombatHudView))]
    public sealed class RunCombatHudInputController : MonoBehaviour
    {
        [SerializeField] private RunSceneSessionEntryPoint sessionEntryPoint;
        [SerializeField] private RunCombatHudView view;
        [SerializeField] private string playerId = "player:local";

        private PlayerBrain playerBrain;
        private bool isSubscribed;

        public RunSceneSessionEntryPoint SessionEntryPoint => sessionEntryPoint;
        public RunCombatHudView View => view;
        public string PlayerId => playerId;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            RunSceneSessionEntryPoint configuredEntryPoint,
            RunCombatHudView configuredView,
            string configuredPlayerId)
        {
            Unsubscribe();
            sessionEntryPoint = configuredEntryPoint;
            view = configuredView;
            playerId = configuredPlayerId?.Trim() ?? string.Empty;
            playerBrain = null;
            if (isActiveAndEnabled)
                Subscribe();
        }
#endif

        private void Awake()
        {
            view ??= GetComponent<RunCombatHudView>();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            playerBrain = null;
        }

        public bool TryUseAbilitySlot(int slotIndex)
        {
            if (slotIndex < 0 ||
                (!IsResolvedPlayerValid() && !TryResolvePlayer()))
            {
                return false;
            }

            return playerBrain.TrySubmitSkillSlotCommand(slotIndex);
        }

        private void Subscribe()
        {
            view ??= GetComponent<RunCombatHudView>();
            if (view == null || isSubscribed)
                return;

            view.AbilitySlotPressed += HandleAbilitySlotPressed;
            isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (view != null && isSubscribed)
                view.AbilitySlotPressed -= HandleAbilitySlotPressed;

            isSubscribed = false;
        }

        private void HandleAbilitySlotPressed(int slotIndex)
        {
            TryUseAbilitySlot(slotIndex);
        }

        private bool TryResolvePlayer()
        {
            playerBrain = null;
            if (sessionEntryPoint == null ||
                string.IsNullOrWhiteSpace(playerId))
            {
                return false;
            }

            for (int i = 0;
                 i < sessionEntryPoint.Participants.Count;
                 i++)
            {
                RunSceneParticipantBinding participant =
                    sessionEntryPoint.Participants[i];
                if (participant == null ||
                    !participant.IsValid ||
                    !string.Equals(
                        participant.PlayerId,
                        playerId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                playerBrain =
                    participant.Inventory.GetComponent<PlayerBrain>();
                return IsResolvedPlayerValid();
            }

            return false;
        }

        private bool IsResolvedPlayerValid()
        {
            return playerBrain != null && playerBrain.gameObject.activeInHierarchy;
        }
    }
}
