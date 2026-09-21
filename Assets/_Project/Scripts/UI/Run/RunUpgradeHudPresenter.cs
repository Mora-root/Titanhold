using System;
using System.Collections;
using Titanhold.Run;
using Titanhold.Session;
using UnityEngine;

namespace Titanhold.UI.Run
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RunUpgradeHudView))]
    public sealed class RunUpgradeHudPresenter : MonoBehaviour
    {
        [SerializeField] private RunUpgradeHudView view;
        [SerializeField] private string playerId = "player:local";

        private RunUpgradeChoiceService choices;
        private RunUpgradeHudModelBuilder modelBuilder;
        private bool hasStarted;

        public RunUpgradeHudView View => view;
        public string PlayerId => playerId?.Trim() ?? string.Empty;
        public bool IsBound => choices != null;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            RunUpgradeHudView configuredView,
            string configuredPlayerId)
        {
            view = configuredView;
            playerId = configuredPlayerId?.Trim() ?? string.Empty;
        }
#endif

        private IEnumerator Start()
        {
            hasStarted = true;
            yield return null;
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
        }

        public bool TryBind()
        {
            view ??= GetComponent<RunUpgradeHudView>();
            if (choices != null)
            {
                Refresh();
                return true;
            }

            if (view == null || PlayerId.Length == 0)
                return false;

            GameSessionRuntimeHost host =
                FindAnyObjectByType<GameSessionRuntimeHost>(
                    FindObjectsInactive.Include);
            GameSessionState session = host != null && host.IsInitialized
                ? host.Runtime.GameSession.State
                : null;
            if (session?.ActiveRun == null ||
                host.RunUpgrades == null ||
                !host.RunUpgrades.IsValid ||
                !host.Runtime.TryGetActiveRunUpgradeChoices(
                    session.ActiveRun.RunSessionId,
                    out RunUpgradeChoiceService activeChoices) ||
                !activeChoices.TryGetParticipant(PlayerId, out _))
            {
                view.Clear();
                return false;
            }

            choices = activeChoices;
            modelBuilder = new RunUpgradeHudModelBuilder(host.RunUpgrades);
            choices.ChoiceResolved += HandleChoiceResolved;
            Refresh();
            return true;
        }

        public void Refresh()
        {
            if (choices == null || modelBuilder == null || view == null ||
                !choices.TryGetParticipant(
                    PlayerId,
                    out RunParticipantUpgradeState participant))
            {
                return;
            }

            if (!modelBuilder.TryBuild(
                    participant,
                    out RunUpgradeHudModel model,
                    out RunUpgradeHudPresentationError error) ||
                !view.Render(model))
            {
                Debug.LogError(
                    $"Run upgrade HUD could not render participant '{PlayerId}': {error}.",
                    this);
            }
        }

        private void HandleChoiceResolved(
            RunUpgradeChoiceState choice,
            string selectedUpgradeId,
            RunParticipantUpgradeState participant)
        {
            if (choice != null && string.Equals(
                    choice.PlayerId,
                    PlayerId,
                    StringComparison.Ordinal))
            {
                Refresh();
            }
        }

        private void Unbind()
        {
            if (choices != null)
                choices.ChoiceResolved -= HandleChoiceResolved;

            choices = null;
            modelBuilder = null;
        }
    }
}
