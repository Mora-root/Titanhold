using System;
using System.Collections.Generic;
using UnityEngine;

namespace Titanhold.Run
{
    [DisallowMultipleComponent]
    public sealed class RunParticipantDefeatController : MonoBehaviour
    {
        [SerializeField] private RunFlowRuntime runFlowRuntime;
        [SerializeField] private Health[] participantHealth =
            Array.Empty<Health>();

        public RunFlowRuntime RunFlowRuntime => runFlowRuntime;
        public IReadOnlyList<Health> ParticipantHealth =>
            participantHealth ?? Array.Empty<Health>();
        public bool HasRequiredReferences
        {
            get
            {
                if (runFlowRuntime == null ||
                    participantHealth == null ||
                    participantHealth.Length == 0)
                {
                    return false;
                }

                HashSet<Health> uniqueParticipants = new();
                for (int i = 0; i < participantHealth.Length; i++)
                {
                    if (participantHealth[i] == null ||
                        !uniqueParticipants.Add(participantHealth[i]))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            RunFlowRuntime configuredRunFlowRuntime,
            Health[] configuredParticipantHealth)
        {
            runFlowRuntime = configuredRunFlowRuntime;
            participantHealth = configuredParticipantHealth ??
                Array.Empty<Health>();
        }
#endif

        private void OnEnable()
        {
            if (participantHealth == null)
                return;

            for (int i = 0; i < participantHealth.Length; i++)
            {
                if (participantHealth[i] != null)
                    participantHealth[i].OnDeath += HandleParticipantDeath;
            }
        }

        private void OnDisable()
        {
            if (participantHealth == null)
                return;

            for (int i = 0; i < participantHealth.Length; i++)
            {
                if (participantHealth[i] != null)
                    participantHealth[i].OnDeath -= HandleParticipantDeath;
            }
        }

        private void HandleParticipantDeath()
        {
            if (!HasRequiredReferences || runFlowRuntime.State.IsTerminal)
            {
                return;
            }

            for (int i = 0; i < participantHealth.Length; i++)
            {
                if (participantHealth[i].IsAlive)
                    return;
            }

            RunFlowTransitionResult result =
                runFlowRuntime.Service.TryFailRun();
            if (!result.Success)
            {
                Debug.LogWarning(
                    $"Run defeat command failed: {result.Error}.",
                    this);
            }
        }
    }
}
