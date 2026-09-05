using System;
using System.Collections.Generic;
using Titanhold.Combat;
using Titanhold.Session;
using UnityEngine;

namespace Titanhold.Run
{
    [DisallowMultipleComponent]
    public sealed class RunProgressionCombatAdapter : MonoBehaviour
    {
        [SerializeField] private RunSceneSessionEntryPoint sessionEntryPoint;
        [SerializeField] private RunProgressionDefinition progressionDefinition;

        private readonly HashSet<CombatExecutionId> rewardedExecutions = new();
        private readonly List<RunProgressionParticipantGateway> participantGateways =
            new();
        private readonly List<CombatSubscription> subscriptions = new();

        public RunProgressionService Progression { get; private set; }
        public bool IsInitialized => Progression != null;
        public bool IsSessionBacked { get; private set; }
        public RunSceneSessionEntryPoint SessionEntryPoint => sessionEntryPoint;
        public RunProgressionDefinition ProgressionDefinition =>
            progressionDefinition;

        public event Action<string, RunProgressionResult> ExperienceAwarded;

        private bool hasStarted;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            RunSceneSessionEntryPoint configuredEntryPoint,
            RunProgressionDefinition configuredDefinition)
        {
            sessionEntryPoint = configuredEntryPoint;
            progressionDefinition = configuredDefinition;
        }
#endif

        private void Start()
        {
            hasStarted = true;
            TryInitialize();
        }

        private void OnEnable()
        {
            if (hasStarted && !IsInitialized)
                TryInitialize();
        }

        private void OnDisable()
        {
            ClearBindings();
            Progression = null;
            IsSessionBacked = false;
            rewardedExecutions.Clear();
        }

        public bool TryInitialize()
        {
            if (IsInitialized)
                return true;

            sessionEntryPoint ??=
                FindAnyObjectByType<RunSceneSessionEntryPoint>(
                    FindObjectsInactive.Include);
            if (sessionEntryPoint == null)
            {
                Debug.LogError(
                    $"{nameof(RunProgressionCombatAdapter)} requires a run scene session entry point.",
                    this);
                return false;
            }

            string definitionError = "Definition reference is missing.";
            RunExperienceCurve localCurve = null;
            if (progressionDefinition == null ||
                !progressionDefinition.TryBuildCurve(
                    out localCurve,
                    out definitionError))
            {
                Debug.LogError(
                    $"Run progression definition is invalid: {definitionError}",
                    this);
                return false;
            }

            IReadOnlyList<RunSceneParticipantBinding> bindings =
                sessionEntryPoint.Participants;
            GameSessionRuntimeHost host =
                FindAnyObjectByType<GameSessionRuntimeHost>(
                    FindObjectsInactive.Include);
            GameSessionRuntime runtime =
                host != null && host.IsInitialized ? host.Runtime : null;
            RunSessionDescriptor descriptor =
                runtime?.GameSession.State.ActiveRun;

            if (runtime != null && descriptor != null)
            {
                if (host.RunProgression != progressionDefinition)
                {
                    Debug.LogError(
                        "Run scene and session host use different progression definitions.",
                        this);
                    return false;
                }

                if (!RunSceneParticipantBindingResolver.TryResolve(
                        descriptor,
                        bindings,
                        out RunSceneParticipantBinding[] resolved,
                        out string resolutionError) ||
                    !runtime.TryGetActiveRunProgression(
                        descriptor.RunSessionId,
                        out RunProgressionService sessionProgression))
                {
                    Debug.LogError(
                        "Could not bind run progression to the active session. " +
                        resolutionError,
                        this);
                    return false;
                }

                return TryInitialize(
                    sessionProgression,
                    resolved,
                    sessionBacked: true);
            }

            RunProgressionService localProgression = new(localCurve);
            for (int i = 0; i < bindings.Count; i++)
            {
                RunSceneParticipantBinding binding = bindings[i];
                if (binding == null || !binding.IsValid)
                {
                    Debug.LogError(
                        $"Direct scene participant binding {i} is invalid.",
                        this);
                    return false;
                }

                RunProgressionResult registration =
                    localProgression.TryRegisterParticipant(
                        new RunParticipantIdentity(
                            binding.PlayerId,
                            binding.CharacterId));
                if (!registration.Success)
                {
                    Debug.LogError(
                        $"Could not register direct scene participant '{binding.PlayerId}': " +
                        $"{registration.Error}.",
                        this);
                    return false;
                }
            }

            return TryInitialize(
                localProgression,
                bindings,
                sessionBacked: false);
        }

        public bool TryInitialize(
            RunProgressionService progression,
            IReadOnlyList<RunSceneParticipantBinding> bindings,
            bool sessionBacked)
        {
            if (IsInitialized ||
                progression == null ||
                bindings == null ||
                bindings.Count == 0)
            {
                return false;
            }

            Progression = progression;
            IsSessionBacked = sessionBacked;
            if (TryBindParticipants(bindings))
                return true;

            Progression = null;
            IsSessionBacked = false;
            return false;
        }

        public bool TryApplyReport(
            string playerId,
            CombatActorReference expectedSource,
            CombatExecutionReport report,
            out RunProgressionResult result)
        {
            result = default;
            if (Progression == null ||
                string.IsNullOrWhiteSpace(playerId) ||
                !expectedSource.IsValid ||
                report == null ||
                !report.ExecutionId.IsValid ||
                rewardedExecutions.Contains(report.ExecutionId))
            {
                return false;
            }

            HashSet<EnemyRewardSource> rewardedEnemies = new();
            long totalExperience = 0;
            for (int i = 0; i < report.ResolutionCount; i++)
            {
                DamageTargetResolution resolution = report[i];
                DamageResult damageResult = resolution.Result;
                if (!damageResult.Killed ||
                    !damageResult.HasDeathContext ||
                    damageResult.DeathContext.ExecutionId != report.ExecutionId ||
                    damageResult.DeathContext.Source != expectedSource ||
                    resolution.Target is not Component targetComponent)
                {
                    continue;
                }

                EnemyRewardSource reward =
                    targetComponent.GetComponent<EnemyRewardSource>();
                reward ??=
                    targetComponent.GetComponentInParent<EnemyRewardSource>();
                if (reward == null ||
                    reward.RunExperienceAmount <= 0 ||
                    !rewardedEnemies.Add(reward))
                {
                    continue;
                }

                totalExperience += reward.RunExperienceAmount;
                if (totalExperience > int.MaxValue)
                {
                    Debug.LogError(
                        "One combat execution produced more run experience than can be represented.",
                        this);
                    return false;
                }
            }

            if (totalExperience <= 0)
                return false;

            result = Progression.TryGrantExperience(
                playerId,
                (int)totalExperience);
            if (!result.Success)
                return false;

            rewardedExecutions.Add(report.ExecutionId);
            ExperienceAwarded?.Invoke(playerId, result);
            return true;
        }

        private bool TryBindParticipants(
            IReadOnlyList<RunSceneParticipantBinding> bindings)
        {
            ClearBindings();
            for (int i = 0; i < bindings.Count; i++)
            {
                RunSceneParticipantBinding binding = bindings[i];
                if (binding == null ||
                    !binding.IsValid ||
                    !Progression.TryGetParticipant(
                        binding.PlayerId,
                        out RunParticipantProgressionState state) ||
                    !string.Equals(
                        state.CharacterId,
                        binding.CharacterId,
                        StringComparison.Ordinal))
                {
                    Debug.LogError(
                        $"Run participant binding {i} does not match progression state.",
                        this);
                    ClearBindings();
                    return false;
                }

                GameObject participant = binding.Inventory.gameObject;
                PlayerCombat combat = participant.GetComponent<PlayerCombat>();
                IPlayerSkillCommands skills = PlayerSkillCommands.Resolve(participant);
                if (combat == null && skills == null)
                {
                    Debug.LogError(
                        $"Run participant '{binding.PlayerId}' has no combat execution source.",
                        participant);
                    ClearBindings();
                    return false;
                }

                RunProgressionParticipantGateway gateway =
                    participant.GetComponent<RunProgressionParticipantGateway>();
                gateway ??=
                    participant.AddComponent<RunProgressionParticipantGateway>();
                if (!gateway.TryBind(Progression, binding.PlayerId))
                {
                    Debug.LogError(
                        $"Could not bind run progression gateway for participant '{binding.PlayerId}'.",
                        participant);
                    ClearBindings();
                    return false;
                }

                participantGateways.Add(gateway);

                if (combat != null)
                    AddSubscription(binding.PlayerId, combat.ActorReference, combat);

                if (skills != null)
                    AddSubscription(binding.PlayerId, skills.ActorReference, skills);
            }

            return true;
        }

        private void AddSubscription(
            string playerId,
            CombatActorReference actor,
            PlayerCombat combat)
        {
            Action<CombatExecutionReport> handler = report =>
                TryApplyReport(playerId, actor, report, out _);
            combat.ExecutionResolved += handler;
            subscriptions.Add(new CombatSubscription(combat, null, handler));
        }

        private void AddSubscription(
            string playerId,
            CombatActorReference actor,
            IPlayerSkillCommands skills)
        {
            Action<CombatExecutionReport> handler = report =>
                TryApplyReport(playerId, actor, report, out _);
            skills.ExecutionResolved += handler;
            subscriptions.Add(new CombatSubscription(null, skills, handler));
        }

        private void ClearBindings()
        {
            for (int i = 0; i < subscriptions.Count; i++)
            {
                CombatSubscription subscription = subscriptions[i];
                if (subscription.Combat != null)
                {
                    subscription.Combat.ExecutionResolved -=
                        subscription.Handler;
                }

                if (subscription.Skills != null)
                {
                    subscription.Skills.ExecutionResolved -=
                        subscription.Handler;
                }
            }

            subscriptions.Clear();
            for (int i = 0; i < participantGateways.Count; i++)
            {
                RunProgressionParticipantGateway gateway =
                    participantGateways[i];
                if (gateway != null)
                    gateway.Unbind(Progression);
            }

            participantGateways.Clear();
        }

        private readonly struct CombatSubscription
        {
            public CombatSubscription(
                PlayerCombat combat,
                IPlayerSkillCommands skills,
                Action<CombatExecutionReport> handler)
            {
                Combat = combat;
                Skills = skills;
                Handler = handler;
            }

            public PlayerCombat Combat { get; }
            public IPlayerSkillCommands Skills { get; }
            public Action<CombatExecutionReport> Handler { get; }
        }
    }
}
