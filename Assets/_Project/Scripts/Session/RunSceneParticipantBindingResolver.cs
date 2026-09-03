using System.Collections.Generic;

namespace Titanhold.Session
{
    public static class RunSceneParticipantBindingResolver
    {
        public static bool TryResolve(
            RunSessionDescriptor descriptor,
            IReadOnlyList<RunSceneParticipantBinding> bindings,
            out RunSceneParticipantBinding[] resolved,
            out string error)
        {
            resolved = null;
            error = string.Empty;
            if (descriptor == null)
            {
                error = "Run session descriptor is missing.";
                return false;
            }

            if (bindings == null || bindings.Count == 0)
            {
                error = "Run scene has no participant bindings.";
                return false;
            }

            HashSet<PlayerInventory> usedPlayerRuntimes = new();
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
                     bindingIndex < bindings.Count;
                     bindingIndex++)
                {
                    RunSceneParticipantBinding candidate = bindings[bindingIndex];
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

                if (!usedPlayerRuntimes.Add(match.Inventory))
                {
                    error =
                        "One player runtime was assigned to multiple participants.";
                    return false;
                }

                resolved[participantIndex] = match;
            }

            return true;
        }
    }
}
