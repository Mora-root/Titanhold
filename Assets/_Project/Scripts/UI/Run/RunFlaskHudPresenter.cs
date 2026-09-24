using System;
using Titanhold.Combat.Flasks;
using Titanhold.Session;
using UnityEngine;

namespace Titanhold.UI.Run
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RunFlaskHudView))]
    public sealed class RunFlaskHudPresenter : MonoBehaviour
    {
        [SerializeField] private RunSceneSessionEntryPoint sessionEntryPoint;
        [SerializeField] private RunFlaskHudView view;
        [SerializeField] private string playerId = "player:local";
        [SerializeField] private string[] keyLabels = { "Q", "E" };

        private PlayerFlaskController flasks;
        private bool hasStarted;

        public RunSceneSessionEntryPoint SessionEntryPoint => sessionEntryPoint;
        public RunFlaskHudView View => view;
        public string PlayerId => playerId;
        public bool IsBound => flasks != null && flasks.IsInitialized;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            RunSceneSessionEntryPoint configuredEntryPoint,
            RunFlaskHudView configuredView,
            string configuredPlayerId,
            string[] configuredKeyLabels)
        {
            sessionEntryPoint = configuredEntryPoint;
            view = configuredView;
            playerId = configuredPlayerId?.Trim() ?? string.Empty;
            keyLabels = configuredKeyLabels ?? Array.Empty<string>();
            flasks = null;
        }
#endif

        private void Start()
        {
            hasStarted = true;
            TryBind();
        }

        private void OnEnable()
        {
            if (hasStarted)
                TryBind();
        }

        private void OnDisable()
        {
            flasks = null;
            view?.Clear();
        }

        private void Update()
        {
            if (!IsBound && !TryBind())
                return;

            double now = Time.timeAsDouble;
            int count = Mathf.Min(view.SlotCount, flasks.SlotCount);
            for (int i = 0; i < count; i++)
            {
                if (flasks.TryGetCooldown(i, now, out FlaskCooldownSnapshot cooldown))
                    view.TryRenderCooldown(i, cooldown);
                else
                    view.TryRenderReady(i);
            }
        }

        public bool TryBind()
        {
            view ??= GetComponent<RunFlaskHudView>();
            sessionEntryPoint ??=
                FindAnyObjectByType<RunSceneSessionEntryPoint>(
                    FindObjectsInactive.Include);
            flasks = ResolveFlasks(sessionEntryPoint, playerId);
            if (view == null ||
                flasks == null ||
                !flasks.TryInitialize())
            {
                view?.Clear();
                flasks = null;
                return false;
            }

            for (int i = 0; i < view.SlotCount; i++)
            {
                if (flasks.TryGetDefinition(i, out FlaskDefinition definition))
                {
                    view.TryRenderContent(
                        i,
                        definition.FlaskId,
                        definition.DisplayName,
                        definition.Icon,
                        i < keyLabels.Length ? keyLabels[i] : string.Empty);
                }
                else
                {
                    view.TryRenderContent(i, string.Empty, string.Empty, null, string.Empty);
                }
            }

            return true;
        }

        internal static PlayerFlaskController ResolveFlasks(
            RunSceneSessionEntryPoint entryPoint,
            string requestedPlayerId)
        {
            if (entryPoint == null || string.IsNullOrWhiteSpace(requestedPlayerId))
                return null;

            for (int i = 0; i < entryPoint.Participants.Count; i++)
            {
                RunSceneParticipantBinding participant = entryPoint.Participants[i];
                if (participant == null ||
                    participant.Inventory == null ||
                    !string.Equals(
                        participant.PlayerId,
                        requestedPlayerId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                return participant.Inventory.GetComponent<PlayerFlaskController>();
            }

            return null;
        }
    }
}
