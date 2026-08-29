using System;
using System.Collections.Generic;
using Titanhold.Combat;
using UnityEngine;

namespace Titanhold.Run
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RunFlowRuntime))]
    public sealed class ExplorationCombatExecutionAdapter : MonoBehaviour
    {
        [SerializeField] private RunFlowRuntime runFlowRuntime;
        [SerializeField] private PlayerCombat playerCombat;
        [SerializeField] private PlayerSkillExecutor playerSkillExecutor;

        public bool HasLastApplicationResult { get; private set; }
        public ExplorationKillApplicationResult LastApplicationResult { get; private set; }
        public bool HasPlayerCombatSource => playerCombat != null;
        public bool HasPlayerSkillSource => playerSkillExecutor != null;

        public event Action<ExplorationKillApplicationResult> KillBatchProcessed;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
        }

        private void Start()
        {
            RebindPlayerSources();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void RebindPlayerSources()
        {
            Unsubscribe();
            ResolveReferences();
            Subscribe();
        }

        public bool TryApplyReport(
            CombatExecutionReport report,
            out ExplorationKillApplicationResult result)
        {
            result = default;
            if (report == null)
                return false;

            List<ExplorationKillRecord> killRecords = new List<ExplorationKillRecord>();
            for (int i = 0; i < report.ResolutionCount; i++)
            {
                DamageTargetResolution resolution = report[i];
                DamageResult damageResult = resolution.Result;
                if (!damageResult.Killed || !damageResult.HasDeathContext)
                    continue;

                if (!(resolution.Target is Component targetComponent))
                    continue;

                EnemyRunContributionSource contributionSource =
                    targetComponent.GetComponent<EnemyRunContributionSource>();
                if (contributionSource == null)
                {
                    contributionSource =
                        targetComponent.GetComponentInParent<EnemyRunContributionSource>();
                }

                if (contributionSource == null)
                    continue;

                killRecords.Add(contributionSource.CreateKillRecord(damageResult.DeathContext));
            }

            if (killRecords.Count == 0)
                return false;

            ResolveReferences();
            result = runFlowRuntime.KillApplication.TryApplyBatch(killRecords);
            LastApplicationResult = result;
            HasLastApplicationResult = true;
            KillBatchProcessed?.Invoke(result);
            return true;
        }

        private void HandleExecutionResolved(CombatExecutionReport report)
        {
            TryApplyReport(report, out _);
        }

        private void Subscribe()
        {
            if (playerCombat != null)
                playerCombat.ExecutionResolved += HandleExecutionResolved;

            if (playerSkillExecutor != null)
                playerSkillExecutor.ExecutionResolved += HandleExecutionResolved;
        }

        private void Unsubscribe()
        {
            if (playerCombat != null)
                playerCombat.ExecutionResolved -= HandleExecutionResolved;

            if (playerSkillExecutor != null)
                playerSkillExecutor.ExecutionResolved -= HandleExecutionResolved;
        }

        private void ResolveReferences()
        {
            runFlowRuntime ??= GetComponent<RunFlowRuntime>();
            playerCombat ??= FindAnyObjectByType<PlayerCombat>();
            playerSkillExecutor ??= FindAnyObjectByType<PlayerSkillExecutor>();
        }
    }
}
