using System;
using System.Collections.Generic;
using Titanhold.Run;
using UnityEngine;

namespace Titanhold.Session
{
    [DisallowMultipleComponent]
    public sealed class RunSceneSessionEntryPoint : MonoBehaviour
    {
        [SerializeField] private RunSceneParticipantBinding[] participants =
            Array.Empty<RunSceneParticipantBinding>();

        public IReadOnlyList<RunSceneParticipantBinding> Participants =>
            participants ?? Array.Empty<RunSceneParticipantBinding>();

#if UNITY_EDITOR
        public void ConfigureForEditor(
            RunSceneParticipantBinding[] configuredParticipants)
        {
            participants = configuredParticipants ??
                Array.Empty<RunSceneParticipantBinding>();
        }
#endif

        private void Start()
        {
            TryActivateSessionRun();
            RestoreParticipantVitals();
        }

        private void TryActivateSessionRun()
        {
            GameSessionRuntimeHost host =
                FindAnyObjectByType<GameSessionRuntimeHost>(
                    FindObjectsInactive.Include);

            // Direct SampleScene play remains a supported editor workflow.
            if (host == null || !host.IsInitialized)
                return;

            GameSessionRuntime runtime = host.Runtime;
            GameSessionState state = runtime.GameSession.State;
            if (state.Phase != GameSessionPhase.TransitionToRun)
                return;

            RunSessionDescriptor descriptor = state.ActiveRun;
            if (descriptor == null)
            {
                Debug.LogError("Run transition has no active descriptor.", this);
                return;
            }

            if (!RunSceneParticipantBindingResolver.TryResolve(
                    descriptor,
                    Participants,
                    out RunSceneParticipantBinding[] resolved,
                    out string resolutionError))
            {
                RejectEntry(runtime, descriptor, resolutionError);
                return;
            }

            for (int i = 0; i < resolved.Length; i++)
            {
                RunSceneParticipantBinding binding = resolved[i];
                if (runtime.TryGetCharacterSnapshot(
                        binding.CharacterId,
                        out _))
                {
                    CharacterSnapshotRestoreResult restore =
                        runtime.TryRestoreCharacter(
                            binding.CharacterId,
                            binding.Inventory,
                            binding.Equipment,
                            binding.Experience,
                            binding.Gold);
                    if (!restore.Success)
                    {
                        RejectEntry(
                            runtime,
                            descriptor,
                            $"Could not restore '{binding.CharacterId}': " +
                            $"{restore.Error} {restore.Detail}");
                        return;
                    }
                }
                else
                {
                    CharacterSnapshotCaptureResult capture =
                        runtime.TryCaptureCharacter(
                            binding.CharacterId,
                            binding.Inventory,
                            binding.Equipment,
                            binding.Experience,
                            binding.Gold);
                    if (!capture.Success)
                    {
                        RejectEntry(
                            runtime,
                            descriptor,
                            $"Could not capture initial '{binding.CharacterId}': " +
                            $"{capture.Error} {capture.Detail}");
                        return;
                    }
                }
            }

            if (!TryBindParticipantAbilities(
                    host,
                    runtime,
                    descriptor,
                    resolved,
                    out string abilityError))
            {
                RejectEntry(runtime, descriptor, abilityError);
                return;
            }

            GameSessionCommandResult activation =
                runtime.GameSession.TryActivateRun(descriptor.RunSessionId);
            if (!activation.Success)
            {
                RejectEntry(
                    runtime,
                    descriptor,
                    $"Could not activate run: {activation.Error}.");
            }
        }

        private void RejectEntry(
            GameSessionRuntime runtime,
            RunSessionDescriptor descriptor,
            string error)
        {
            ClearParticipantAbilityBindings();
            Debug.LogError(error, this);
            GameSessionCommandResult cancel =
                runtime.GameSession.TryCancelRunTransition(
                    descriptor.RunSessionId);
            if (!cancel.Success)
            {
                Debug.LogError(
                    $"Could not cancel rejected run entry: {cancel.Error}.",
                    this);
            }
        }

        private static bool TryBindParticipantAbilities(
            GameSessionRuntimeHost host,
            GameSessionRuntime runtime,
            RunSessionDescriptor descriptor,
            IReadOnlyList<RunSceneParticipantBinding> resolved,
            out string error)
        {
            error = string.Empty;
            if (host.AbilityDefinitions == null ||
                !host.AbilityDefinitions.IsValid)
            {
                error = "Run session has no valid ability definition catalog.";
                return false;
            }

            if (!runtime.TryGetActiveRunAbilityLoadout(
                    descriptor.RunSessionId,
                    out RunAbilityLoadoutService loadout))
            {
                error = "Run session has no active participant ability loadout.";
                return false;
            }

            IPlayerAbilitySlotBinding[] bindings =
                new IPlayerAbilitySlotBinding[resolved.Count];
            RunParticipantAbilityState[] states =
                new RunParticipantAbilityState[resolved.Count];
            for (int i = 0; i < resolved.Count; i++)
            {
                RunSceneParticipantBinding participant = resolved[i];
                GameObject participantObject = participant.Inventory.gameObject;
                IPlayerSkillCommands commands =
                    PlayerSkillCommands.Resolve(participantObject);
                bindings[i] = commands as IPlayerAbilitySlotBinding;
                if (bindings[i] == null)
                {
                    error =
                        $"Run participant '{participant.PlayerId}' has no loadout-aware ability executor.";
                    return false;
                }

                if (!loadout.TryGetParticipant(
                        participant.PlayerId,
                        out states[i]))
                {
                    error =
                        $"Run participant '{participant.PlayerId}' has no ability state.";
                    return false;
                }
            }

            for (int i = 0; i < bindings.Length; i++)
            {
                if (bindings[i].TryBindAbilitySlots(
                        states[i],
                        host.AbilityDefinitions))
                {
                    continue;
                }

                for (int rollbackIndex = 0;
                     rollbackIndex < i;
                     rollbackIndex++)
                {
                    bindings[rollbackIndex].TryClearAbilitySlotBinding();
                }

                error =
                    $"Could not bind abilities for run participant '{resolved[i].PlayerId}'.";
                return false;
            }

            return true;
        }

        private void ClearParticipantAbilityBindings()
        {
            if (participants == null)
                return;

            for (int i = 0; i < participants.Length; i++)
            {
                PlayerInventory inventory = participants[i]?.Inventory;
                if (inventory == null)
                    continue;

                IPlayerSkillCommands commands =
                    PlayerSkillCommands.Resolve(inventory.gameObject);
                if (commands is IPlayerAbilitySlotBinding binding)
                    binding.TryClearAbilitySlotBinding();
            }
        }

        private void RestoreParticipantVitals()
        {
            if (participants == null)
                return;

            for (int i = 0; i < participants.Length; i++)
            {
                PlayerInventory inventory = participants[i]?.Inventory;
                if (inventory == null)
                    continue;

                GameObject participant = inventory.gameObject;
                participant.GetComponent<Health>()?.RestoreFull();
                participant.GetComponent<PlayerResource>()?.RestoreFull();
            }
        }
    }
}
