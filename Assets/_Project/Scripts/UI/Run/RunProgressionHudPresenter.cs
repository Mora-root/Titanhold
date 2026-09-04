using System.Collections;
using Titanhold.Run;
using UnityEngine;

namespace Titanhold.UI.Run
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RunProgressionHudView))]
    public sealed class RunProgressionHudPresenter : MonoBehaviour
    {
        [SerializeField] private RunProgressionCombatAdapter progressionAdapter;
        [SerializeField] private RunProgressionHudView view;
        [SerializeField] private string playerId = "player:local";
        [SerializeField, Min(0f)] private float levelUpVisibleDuration = 2f;

        private RunProgressionService progression;
        private Coroutine hideLevelUpCoroutine;
        private bool hasStarted;

        public RunProgressionCombatAdapter ProgressionAdapter =>
            progressionAdapter;
        public RunProgressionHudView View => view;
        public string PlayerId => playerId;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            RunProgressionCombatAdapter configuredAdapter,
            RunProgressionHudView configuredView,
            string configuredPlayerId,
            float configuredLevelUpDuration)
        {
            progressionAdapter = configuredAdapter;
            view = configuredView;
            playerId = configuredPlayerId?.Trim() ?? string.Empty;
            levelUpVisibleDuration = Mathf.Max(0f, configuredLevelUpDuration);
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
            StopHideCoroutine();
            view?.HideLevelUp();
        }

        public bool TryBind()
        {
            if (progression != null)
            {
                Refresh();
                return true;
            }

            progressionAdapter ??=
                FindAnyObjectByType<RunProgressionCombatAdapter>(
                    FindObjectsInactive.Include);
            view ??= GetComponent<RunProgressionHudView>();
            if (progressionAdapter == null ||
                view == null ||
                string.IsNullOrWhiteSpace(playerId) ||
                !progressionAdapter.TryInitialize())
            {
                Debug.LogError(
                    $"{nameof(RunProgressionHudPresenter)} could not bind its run progression source.",
                    this);
                return false;
            }

            progression = progressionAdapter.Progression;
            if (!progression.TryGetParticipant(
                    playerId,
                    out _))
            {
                Debug.LogError(
                    $"Run progression participant '{playerId}' is missing.",
                    this);
                progression = null;
                return false;
            }

            progression.StateChanged += HandleStateChanged;
            progressionAdapter.ExperienceAwarded +=
                HandleExperienceAwarded;
            Refresh();
            return true;
        }

        public void Refresh()
        {
            if (progression == null ||
                view == null ||
                !progression.TryGetParticipant(
                    playerId,
                    out RunParticipantProgressionState state))
            {
                return;
            }

            bool hasNextLevel = progression.TryGetExperienceRequirement(
                playerId,
                out int experienceRequired);
            view.RenderProgression(
                state.Level,
                state.Experience,
                experienceRequired,
                isMaximumLevel: !hasNextLevel,
                state.Gold);
        }

        private void HandleStateChanged(
            RunParticipantProgressionState state)
        {
            if (state != null && string.Equals(
                    state.PlayerId,
                    playerId,
                    System.StringComparison.Ordinal))
                Refresh();
        }

        private void HandleExperienceAwarded(
            string awardedPlayerId,
            RunProgressionResult result)
        {
            if (!string.Equals(
                    awardedPlayerId,
                    playerId,
                    System.StringComparison.Ordinal) ||
                !result.Success ||
                result.LevelsGained <= 0 ||
                result.State == null)
            {
                return;
            }

            view.ShowLevelUp(
                result.State.Level,
                result.LevelsGained);
            StopHideCoroutine();
            hideLevelUpCoroutine = StartCoroutine(HideLevelUpAfterDelay());
        }

        private IEnumerator HideLevelUpAfterDelay()
        {
            if (levelUpVisibleDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    levelUpVisibleDuration);
            }

            view?.HideLevelUp();
            hideLevelUpCoroutine = null;
        }

        private void Unbind()
        {
            if (progression != null)
                progression.StateChanged -= HandleStateChanged;

            if (progressionAdapter != null)
            {
                progressionAdapter.ExperienceAwarded -=
                    HandleExperienceAwarded;
            }

            progression = null;
        }

        private void StopHideCoroutine()
        {
            if (hideLevelUpCoroutine != null)
                StopCoroutine(hideLevelUpCoroutine);

            hideLevelUpCoroutine = null;
        }
    }
}
