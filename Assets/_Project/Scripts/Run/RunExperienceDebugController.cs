using UnityEngine;

namespace Titanhold.Run
{
    [DisallowMultipleComponent]
    public sealed class RunExperienceDebugController : MonoBehaviour
    {
        private const int MinimumExperienceGrant = 1;

        [SerializeField, Min(MinimumExperienceGrant)]
        private int experiencePerGrant = 500;
        [SerializeField] private KeyCode grantKey = KeyCode.F6;

        private RunProgressionParticipantGateway progressionGateway;

        public int ExperiencePerGrant => experiencePerGrant;
        public KeyCode GrantKey => grantKey;

        private void Awake()
        {
            TryResolveGateway(out _);
        }

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Input.GetKeyDown(grantKey))
                GrantExperienceFromDebugCommand();
#endif
        }

        [ContextMenu("Debug/Grant Run Experience")]
        public void GrantExperienceFromDebugCommand()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!TryGrantExperience(out RunProgressionResult result))
            {
                Debug.LogWarning(
                    "RunXP debug grant failed because the participant " +
                    "progression gateway is not ready or rejected the grant.",
                    this);
                return;
            }

            RunParticipantProgressionState state = result.State;
            Debug.Log(
                $"Granted {result.ExperienceApplied} RunXP to " +
                $"'{progressionGateway.PlayerId}'. Run Level {state.Level}, " +
                $"current XP {state.Experience}.",
                this);
#endif
        }

        public bool TryGrantExperience(out RunProgressionResult result)
        {
            result = default;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (experiencePerGrant < MinimumExperienceGrant ||
                !TryResolveGateway(out RunProgressionParticipantGateway gateway))
            {
                return false;
            }

            return gateway.TryGrantExperience(experiencePerGrant, out result);
#else
            return false;
#endif
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int configuredExperiencePerGrant,
            KeyCode configuredGrantKey)
        {
            experiencePerGrant = Mathf.Max(
                MinimumExperienceGrant,
                configuredExperiencePerGrant);
            grantKey = configuredGrantKey;
        }
#endif

        private bool TryResolveGateway(
            out RunProgressionParticipantGateway gateway)
        {
            progressionGateway ??=
                GetComponent<RunProgressionParticipantGateway>();
            gateway = progressionGateway;
            return gateway != null && gateway.IsBound;
        }

        private void OnValidate()
        {
            experiencePerGrant = Mathf.Max(
                MinimumExperienceGrant,
                experiencePerGrant);
        }
    }
}
