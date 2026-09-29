using System;
using System.Collections.Generic;
using Titanhold.Combat;
using Titanhold.Session;
using UnityEngine;

namespace Titanhold.Run
{
    [DisallowMultipleComponent]
    public sealed class RunChapterCombatProgressAdapter : MonoBehaviour
    {
        [SerializeField] private RunChapterFlowRuntime chapterFlowRuntime;
        [SerializeField] private RunSceneSessionEntryPoint sessionEntryPoint;

        private readonly List<CombatSubscription> subscriptions = new();
        private bool hasStarted;

        public RunChapterProgressApplicationService ProgressApplication
        {
            get;
            private set;
        }

        public bool IsInitialized => ProgressApplication != null;
        public int SubscriptionCount => subscriptions.Count;
        public RunChapterFlowRuntime ChapterFlowRuntime => chapterFlowRuntime;
        public RunSceneSessionEntryPoint SessionEntryPoint => sessionEntryPoint;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            RunChapterFlowRuntime configuredRuntime,
            RunSceneSessionEntryPoint configuredEntryPoint)
        {
            chapterFlowRuntime = configuredRuntime;
            sessionEntryPoint = configuredEntryPoint;
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
            ProgressApplication = null;
        }

        public bool TryInitialize()
        {
            if (IsInitialized)
                return true;

            chapterFlowRuntime ??=
                GetComponent<RunChapterFlowRuntime>();
            chapterFlowRuntime ??=
                FindAnyObjectByType<RunChapterFlowRuntime>(
                    FindObjectsInactive.Include);
            if (chapterFlowRuntime == null)
            {
                Debug.LogError(
                    $"{nameof(RunChapterCombatProgressAdapter)} requires a " +
                    $"chapter flow runtime.",
                    this);
                return false;
            }

            sessionEntryPoint ??=
                FindAnyObjectByType<RunSceneSessionEntryPoint>(
                    FindObjectsInactive.Include);
            if (sessionEntryPoint == null)
            {
                Debug.LogError(
                    $"{nameof(RunChapterCombatProgressAdapter)} requires a " +
                    $"run scene session entry point.",
                    this);
                return false;
            }

            return TryInitialize(
                chapterFlowRuntime.ProgressApplication,
                sessionEntryPoint.Participants);
        }

        public bool TryInitialize(
            RunChapterProgressApplicationService progressApplication)
        {
            if (progressApplication == null)
                return false;

            if (IsInitialized)
                return ReferenceEquals(ProgressApplication, progressApplication);

            ProgressApplication = progressApplication;
            return true;
        }

        public bool TryInitialize(
            RunChapterProgressApplicationService progressApplication,
            IReadOnlyList<RunSceneParticipantBinding> participants)
        {
            if (!TryInitialize(progressApplication) ||
                participants == null ||
                participants.Count == 0)
            {
                ProgressApplication = null;
                return false;
            }

            if (TryBindParticipants(participants))
                return true;

            ClearBindings();
            ProgressApplication = null;
            return false;
        }

        public bool TryApplyReport(
            string participantId,
            CombatActorReference expectedSource,
            CombatExecutionReport report,
            out RunChapterProgressApplicationResult result)
        {
            return TryApplyReport(
                participantId,
                expectedSource,
                report,
                Time.timeAsDouble,
                out result);
        }

        public bool TryApplyReport(
            string participantId,
            CombatActorReference expectedSource,
            CombatExecutionReport report,
            double simulationTime,
            out RunChapterProgressApplicationResult result)
        {
            result = default;
            if (!IsInitialized ||
                string.IsNullOrWhiteSpace(participantId) ||
                !expectedSource.IsValid ||
                !expectedSource.IsPlayer ||
                report == null ||
                !report.ExecutionId.IsValid)
            {
                return false;
            }

            HashSet<EnemyRunContributionSource> acceptedSources = new();
            List<RunChapterProgressContribution> contributions = new();
            for (int i = 0; i < report.ResolutionCount; i++)
            {
                DamageTargetResolution resolution = report[i];
                DamageResult damageResult = resolution.Result;
                if (!damageResult.Killed ||
                    !damageResult.HasDeathContext ||
                    damageResult.DeathContext.ExecutionId !=
                    report.ExecutionId ||
                    damageResult.DeathContext.Source != expectedSource ||
                    resolution.Target is not Component targetComponent)
                {
                    continue;
                }

                EnemyRunContributionSource contributionSource =
                    targetComponent.GetComponent<EnemyRunContributionSource>();
                contributionSource ??=
                    targetComponent.GetComponentInParent<
                        EnemyRunContributionSource>();
                if (contributionSource == null ||
                    contributionSource.ThreatAmount <= 0f ||
                    !acceptedSources.Add(contributionSource))
                {
                    continue;
                }

                ExplorationKillRecord record =
                    contributionSource.CreateKillRecord(
                        damageResult.DeathContext);
                contributions.Add(new RunChapterProgressContribution(
                    record.DefeatedActor.ActorId,
                    record.Contribution.ThreatAmount));
            }

            if (contributions.Count == 0)
                return false;

            RunChapterProgressCommand command = new(
                $"combat:{report.ExecutionId.Value}",
                participantId,
                contributions);
            result = ProgressApplication.TryApply(command, simulationTime);
            return true;
        }

        private bool TryBindParticipants(
            IReadOnlyList<RunSceneParticipantBinding> participants)
        {
            ClearBindings();
            for (int i = 0; i < participants.Count; i++)
            {
                RunSceneParticipantBinding binding = participants[i];
                if (binding == null || !binding.IsValid)
                {
                    Debug.LogError(
                        $"Run chapter participant binding {i} is invalid.",
                        this);
                    return false;
                }

                GameObject participant = binding.Inventory.gameObject;
                PlayerCombat combat = participant.GetComponent<PlayerCombat>();
                IPlayerSkillCommands skills =
                    PlayerSkillCommands.Resolve(participant);
                if (combat == null && skills == null)
                {
                    Debug.LogError(
                        $"Run participant '{binding.PlayerId}' has no combat " +
                        $"execution source.",
                        participant);
                    return false;
                }

                if (combat != null)
                    AddSubscription(binding.PlayerId, combat.ActorReference, combat);

                if (skills != null)
                    AddSubscription(binding.PlayerId, skills.ActorReference, skills);
            }

            return true;
        }

        private void AddSubscription(
            string participantId,
            CombatActorReference actor,
            PlayerCombat combat)
        {
            Action<CombatExecutionReport> handler = report =>
                TryApplyReport(participantId, actor, report, out _);
            combat.ExecutionResolved += handler;
            subscriptions.Add(new CombatSubscription(combat, null, handler));
        }

        private void AddSubscription(
            string participantId,
            CombatActorReference actor,
            IPlayerSkillCommands skills)
        {
            Action<CombatExecutionReport> handler = report =>
                TryApplyReport(participantId, actor, report, out _);
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
