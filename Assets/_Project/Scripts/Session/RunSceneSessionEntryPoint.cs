using System;
using System.Collections.Generic;
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

            if (!TryResolveBindings(
                    descriptor,
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

        private bool TryResolveBindings(
            RunSessionDescriptor descriptor,
            out RunSceneParticipantBinding[] resolved,
            out string error)
        {
            resolved = null;
            error = string.Empty;
            if (participants == null || participants.Length == 0)
            {
                error = "Run scene has no participant bindings.";
                return false;
            }

            HashSet<RunSceneParticipantBinding> used = new();
            resolved = new RunSceneParticipantBinding[
                descriptor.Participants.Count];
            for (int participantIndex = 0;
                 participantIndex < descriptor.Participants.Count;
                 participantIndex++)
            {
                RunParticipantSelection participant =
                    descriptor.Participants[participantIndex];
                RunSceneParticipantBinding match = null;
                for (int bindingIndex = 0;
                     bindingIndex < participants.Length;
                     bindingIndex++)
                {
                    RunSceneParticipantBinding candidate =
                        participants[bindingIndex];
                    if (candidate == null || !candidate.IsValid ||
                        !candidate.Matches(participant))
                    {
                        continue;
                    }

                    if (match != null)
                    {
                        error =
                            $"Participant '{participant.PlayerId}' has duplicate scene bindings.";
                        return false;
                    }

                    match = candidate;
                }

                if (match == null)
                {
                    error =
                        $"Participant '{participant.PlayerId}' has no valid scene binding.";
                    return false;
                }

                if (!used.Add(match))
                {
                    error = "One scene binding was assigned to multiple participants.";
                    return false;
                }

                resolved[participantIndex] = match;
            }

            return true;
        }

        private void RejectEntry(
            GameSessionRuntime runtime,
            RunSessionDescriptor descriptor,
            string error)
        {
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
    }
}
