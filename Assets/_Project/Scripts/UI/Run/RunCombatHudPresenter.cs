using System;
using Titanhold.Combat;
using Titanhold.Combat.Abilities;
using Titanhold.Run;
using Titanhold.Session;
using UnityEngine;

namespace Titanhold.UI.Run
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RunCombatHudView))]
    public sealed class RunCombatHudPresenter : MonoBehaviour
    {
        [SerializeField] private RunSceneSessionEntryPoint sessionEntryPoint;
        [SerializeField] private RunCombatHudView view;
        [SerializeField] private string playerId = "player:local";
        [SerializeField] private string combatResourceId = "resource:rage";
        [SerializeField] private string combatResourceLabel = "Rage";

        private RunAbilityLoadoutService abilityLoadout;
        private RunParticipantAbilityState abilityState;
        private RunCombatResourceService combatResources;
        private AbilityDefinitionCatalog abilityDefinitions;
        private IPlayerAbilityCooldownSource cooldownSource;
        private bool hasStarted;

        public RunSceneSessionEntryPoint SessionEntryPoint => sessionEntryPoint;
        public RunCombatHudView View => view;
        public string PlayerId => playerId;
        public string CombatResourceId => combatResourceId;
        public bool IsBound =>
            abilityLoadout != null &&
            abilityState != null &&
            combatResources != null &&
            abilityDefinitions != null &&
            cooldownSource != null;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            RunSceneSessionEntryPoint configuredEntryPoint,
            RunCombatHudView configuredView,
            string configuredPlayerId,
            string configuredCombatResourceId,
            string configuredCombatResourceLabel)
        {
            sessionEntryPoint = configuredEntryPoint;
            view = configuredView;
            playerId = configuredPlayerId?.Trim() ?? string.Empty;
            combatResourceId =
                configuredCombatResourceId?.Trim() ?? string.Empty;
            combatResourceLabel =
                configuredCombatResourceLabel?.Trim() ?? string.Empty;
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
            Unbind();
            ClearPresentation();
        }

        private void Update()
        {
            if (!IsBound)
                return;

            double now = Time.timeAsDouble;
            int count = Mathf.Min(
                view.AbilitySlotCount,
                abilityState.AbilitySlotCount);
            for (int slotIndex = 0; slotIndex < count; slotIndex++)
            {
                if (cooldownSource.TryGetAbilityCooldown(
                        slotIndex,
                        now,
                        out AbilityCooldownSnapshot cooldown))
                {
                    view.TryRenderCooldown(slotIndex, cooldown);
                }
                else
                {
                    view.TryRenderReady(slotIndex);
                }
            }
        }

        public bool TryBind()
        {
            if (IsBound)
            {
                RefreshAll();
                return true;
            }

            view ??= GetComponent<RunCombatHudView>();
            sessionEntryPoint ??=
                FindAnyObjectByType<RunSceneSessionEntryPoint>(
                    FindObjectsInactive.Include);
            GameSessionRuntimeHost host =
                FindAnyObjectByType<GameSessionRuntimeHost>(
                    FindObjectsInactive.Include);
            if (view == null ||
                sessionEntryPoint == null ||
                host == null ||
                !host.IsInitialized ||
                string.IsNullOrWhiteSpace(playerId) ||
                string.IsNullOrWhiteSpace(combatResourceId) ||
                host.AbilityDefinitions == null ||
                !host.AbilityDefinitions.IsValid)
            {
                ClearPresentation();
                return false;
            }

            RunSessionDescriptor descriptor =
                host.Runtime.GameSession.State.ActiveRun;
            if (descriptor == null ||
                !host.Runtime.TryGetActiveRunAbilityLoadout(
                    descriptor.RunSessionId,
                    out RunAbilityLoadoutService resolvedLoadout) ||
                !host.Runtime.TryGetActiveRunCombatResources(
                    descriptor.RunSessionId,
                    out RunCombatResourceService resolvedResources) ||
                !resolvedLoadout.TryGetParticipant(
                    playerId,
                    out RunParticipantAbilityState resolvedState) ||
                !TryResolveCooldownSource(
                    sessionEntryPoint,
                    playerId,
                    out IPlayerAbilityCooldownSource resolvedCooldownSource))
            {
                ClearPresentation();
                return false;
            }

            abilityLoadout = resolvedLoadout;
            abilityState = resolvedState;
            combatResources = resolvedResources;
            abilityDefinitions = host.AbilityDefinitions;
            cooldownSource = resolvedCooldownSource;
            abilityLoadout.StateChanged += HandleAbilityStateChanged;
            combatResources.ResourceChanged += HandleCombatResourceChanged;
            RefreshAll();
            return true;
        }

        public void RefreshAll()
        {
            if (!IsBound)
            {
                ClearPresentation();
                return;
            }

            RefreshAbilities();
            RefreshCombatResource();
        }

        private void RefreshAbilities()
        {
            int count = Mathf.Min(
                view.AbilitySlotCount,
                abilityState.AbilitySlotCount);
            for (int slotIndex = 0;
                 slotIndex < view.AbilitySlotCount;
                 slotIndex++)
            {
                if (slotIndex >= count ||
                    !abilityState.TryGetAbilitySlot(
                        slotIndex,
                        out string abilityId) ||
                    string.IsNullOrEmpty(abilityId))
                {
                    view.TryRenderAbility(
                        slotIndex,
                        string.Empty,
                        string.Empty,
                        null);
                    continue;
                }

                string displayName = string.Empty;
                Sprite icon = null;
                if (abilityDefinitions.TryResolve(
                        abilityId,
                        out IAbilityDefinition definition) &&
                    definition is IAbilityPresentationDefinition presentation)
                {
                    displayName = presentation.DisplayName;
                    icon = presentation.Icon;
                }

                view.TryRenderAbility(
                    slotIndex,
                    abilityId,
                    displayName,
                    icon);
            }
        }

        private void RefreshCombatResource()
        {
            if (combatResources.TryGetResource(
                    playerId,
                    combatResourceId,
                    out CombatResourceSnapshot resource))
            {
                view.RenderCombatResource(
                    combatResourceLabel,
                    resource.Current,
                    resource.Maximum);
            }
            else
            {
                view.RenderCombatResourceUnavailable(
                    combatResourceLabel);
            }
        }

        private void HandleAbilityStateChanged(
            RunParticipantAbilityState state)
        {
            if (ReferenceEquals(state, abilityState))
                RefreshAbilities();
        }

        private void HandleCombatResourceChanged(
            string changedPlayerId,
            CombatResourceSnapshot resource)
        {
            if (string.Equals(
                    changedPlayerId,
                    playerId,
                    StringComparison.Ordinal) &&
                string.Equals(
                    resource.ResourceId,
                    combatResourceId,
                    StringComparison.Ordinal))
            {
                view.RenderCombatResource(
                    combatResourceLabel,
                    resource.Current,
                    resource.Maximum);
            }
        }

        private void Unbind()
        {
            if (abilityLoadout != null)
                abilityLoadout.StateChanged -= HandleAbilityStateChanged;
            if (combatResources != null)
                combatResources.ResourceChanged -= HandleCombatResourceChanged;

            abilityLoadout = null;
            abilityState = null;
            combatResources = null;
            abilityDefinitions = null;
            cooldownSource = null;
        }

        private void ClearPresentation()
        {
            if (view == null)
                return;

            view.Clear();
            view.RenderCombatResourceUnavailable(combatResourceLabel);
        }

        private static bool TryResolveCooldownSource(
            RunSceneSessionEntryPoint entryPoint,
            string requestedPlayerId,
            out IPlayerAbilityCooldownSource source)
        {
            source = null;
            for (int i = 0; i < entryPoint.Participants.Count; i++)
            {
                RunSceneParticipantBinding binding =
                    entryPoint.Participants[i];
                if (binding == null ||
                    !string.Equals(
                        binding.PlayerId,
                        requestedPlayerId,
                        StringComparison.Ordinal) ||
                    binding.Inventory == null)
                {
                    continue;
                }

                source = PlayerSkillCommands.Resolve(
                    binding.Inventory.gameObject) as
                    IPlayerAbilityCooldownSource;
                return source != null;
            }

            return false;
        }
    }
}
