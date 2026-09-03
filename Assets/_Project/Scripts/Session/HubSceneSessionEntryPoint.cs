using Titanhold.UI.Hub;
using UnityEngine;

namespace Titanhold.Session
{
    [DisallowMultipleComponent]
    public sealed class HubSceneSessionEntryPoint : MonoBehaviour
    {
        [SerializeField] private HubRunPreparationView view;

        public HubRunPreparationView View => view;

#if UNITY_EDITOR
        public void ConfigureForEditor(HubRunPreparationView configuredView)
        {
            view = configuredView;
        }
#endif

        private void Start()
        {
            GameSessionRuntimeHost host = FindInitializedHost();
            if (host == null)
            {
                view?.SetStartInteractable(false);
                view?.SetStatus("SESSION IS NOT READY");
                Debug.LogError(
                    $"{nameof(HubSceneSessionEntryPoint)} could not find the persistent session host.",
                    this);
                return;
            }

            GameSessionService session = host.Runtime.GameSession;
            if (session.State.Phase == GameSessionPhase.TransitionToHub)
            {
                RunSessionDescriptor activeRun = session.State.ActiveRun;
                if (activeRun == null)
                {
                    RejectHubEntry("Hub transition has no active run.");
                    return;
                }

                GameSessionCommandResult entry =
                    session.TryEnterHub(activeRun.RunSessionId);
                if (!entry.Success)
                {
                    RejectHubEntry(
                        $"Could not enter Hub: {entry.Error}.");
                    return;
                }
            }

            if (session.State.Phase != GameSessionPhase.Hub)
            {
                RejectHubEntry(
                    $"Hub scene opened during {session.State.Phase}.");
                return;
            }

            view?.SetStartInteractable(true);
            RunResultSummary result = session.State.LastRunResult;
            if (result != null)
            {
                view?.SetStatus(
                    $"{result.Outcome.ToString().ToUpperInvariant()} • " +
                    $"ROUNDS: {result.CompletedRoundCount}");
            }
        }

        private void RejectHubEntry(string error)
        {
            view?.SetStartInteractable(false);
            view?.SetStatus("HUB ENTRY FAILED");
            Debug.LogError(error, this);
        }

        private static GameSessionRuntimeHost FindInitializedHost()
        {
            GameSessionRuntimeHost[] hosts =
                FindObjectsByType<GameSessionRuntimeHost>(
                    FindObjectsInactive.Include);
            for (int i = 0; i < hosts.Length; i++)
            {
                if (hosts[i] != null && hosts[i].IsInitialized)
                    return hosts[i];
            }

            return null;
        }
    }
}
