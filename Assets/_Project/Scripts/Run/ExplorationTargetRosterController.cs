using System.Collections.Generic;
using Titanhold.Combat;
using Titanhold.Enemies;
using Titanhold.Session;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Titanhold.Run
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ExplorationTargetRegistry))]
    public sealed class ExplorationTargetRosterController : MonoBehaviour
    {
        [SerializeField] private RunSceneSessionEntryPoint sessionEntryPoint;
        [SerializeField] private ExplorationTargetRegistry targetRegistry;

        public RunSceneSessionEntryPoint SessionEntryPoint => sessionEntryPoint;
        public ExplorationTargetRegistry TargetRegistry => targetRegistry;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            RunSceneSessionEntryPoint entryPoint,
            ExplorationTargetRegistry registry)
        {
            sessionEntryPoint = entryPoint;
            targetRegistry = registry;
        }
#endif

        private void Start()
        {
            ResolveReferences();
            if (!TryRegisterParticipants())
            {
                enabled = false;
                return;
            }

            BindExistingExplorationEnemies();
        }

        private void OnDisable()
        {
            targetRegistry?.Clear();
        }

        private bool TryRegisterParticipants()
        {
            if (sessionEntryPoint == null || targetRegistry == null)
            {
                Debug.LogError(
                    $"{nameof(ExplorationTargetRosterController)} requires " +
                    "a session entry point and target registry.",
                    this);
                return false;
            }

            targetRegistry.Clear();
            IReadOnlyList<RunSceneParticipantBinding> participants =
                sessionEntryPoint.Participants;
            if (participants.Count == 0)
            {
                Debug.LogError(
                    "Exploration target roster has no participants.",
                    this);
                return false;
            }

            for (int i = 0; i < participants.Count; i++)
            {
                RunSceneParticipantBinding binding = participants[i];
                GameObject participant = binding?.Inventory != null
                    ? binding.Inventory.gameObject
                    : null;
                PlayerCombat combat = participant != null
                    ? participant.GetComponent<PlayerCombat>()
                    : null;
                ITargetable target = participant != null
                    ? participant.GetComponent<ITargetable>()
                    : null;
                if (binding == null || !binding.IsValid || combat == null ||
                    target == null ||
                    !targetRegistry.TryRegister(
                        combat.ActorReference,
                        target))
                {
                    targetRegistry.Clear();
                    Debug.LogError(
                        $"Could not register exploration participant at index {i}.",
                        this);
                    return false;
                }
            }

            return true;
        }

        private void BindExistingExplorationEnemies()
        {
            ExplorationAggroTargetProvider[] providers =
                FindObjectsByType<ExplorationAggroTargetProvider>(
                    FindObjectsInactive.Include);
            Scene ownScene = gameObject.scene;
            for (int i = 0; i < providers.Length; i++)
            {
                ExplorationAggroTargetProvider provider = providers[i];
                if (provider.gameObject.scene == ownScene)
                    provider.Bind(targetRegistry);
            }
        }

        private void ResolveReferences()
        {
            targetRegistry ??= GetComponent<ExplorationTargetRegistry>();
            sessionEntryPoint ??=
                FindAnyObjectByType<RunSceneSessionEntryPoint>(
                    FindObjectsInactive.Include);
        }
    }
}
