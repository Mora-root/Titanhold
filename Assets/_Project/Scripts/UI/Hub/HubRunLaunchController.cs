using System;
using System.Collections;
using Titanhold.Session;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Titanhold.UI.Hub
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HubRunPreparationView))]
    public sealed class HubRunLaunchController : MonoBehaviour
    {
        [SerializeField] private HubRunPreparationView view;
        [SerializeField] private GameSessionRuntimeHost sessionHost;
        [SerializeField] private string playerId = "player:local";
        [SerializeField] private string characterId = "character:warrior";
        [SerializeField] private string difficultyId = "difficulty:prototype";
        [SerializeField] private string runSceneName = "SampleScene";

        private bool launchInProgress;

        public bool HasRequiredReferences => view != null && sessionHost != null;
        public HubRunPreparationView View => view;
        public GameSessionRuntimeHost SessionHost => sessionHost;
        public string PlayerId => playerId;
        public string CharacterId => characterId;
        public string DifficultyId => difficultyId;
        public string RunSceneName => runSceneName;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            HubRunPreparationView configuredView,
            GameSessionRuntimeHost configuredSessionHost,
            string configuredPlayerId,
            string configuredCharacterId,
            string configuredDifficultyId,
            string configuredRunSceneName)
        {
            view = configuredView;
            sessionHost = configuredSessionHost;
            playerId = configuredPlayerId;
            characterId = configuredCharacterId;
            difficultyId = configuredDifficultyId;
            runSceneName = configuredRunSceneName;
        }
#endif

        private void Awake()
        {
            if (view == null)
                view = GetComponent<HubRunPreparationView>();
        }

        private void OnEnable()
        {
            if (view != null)
                view.StartRequested += HandleStartRequested;
        }

        private void OnDisable()
        {
            if (view != null)
                view.StartRequested -= HandleStartRequested;
        }

        private void Start()
        {
            TryResolveSessionHost();
        }

        private void HandleStartRequested()
        {
            if (launchInProgress)
                return;

            if (view == null || !TryResolveSessionHost())
            {
                view?.SetStatus("SESSION IS NOT READY");
                Debug.LogError(
                    $"{nameof(HubRunLaunchController)} requires an initialized session host and view.",
                    this);
                return;
            }

            RunLaunchCommand command = new(
                difficultyId,
                CreateRunSeed(),
                new[]
                {
                    new RunParticipantSelection(playerId, characterId)
                });
            GameSessionCommandResult result =
                sessionHost.Runtime.GameSession.TryBeginRun(command);
            if (!result.Success)
            {
                view.SetStatus($"RUN REJECTED: {result.Error}");
                return;
            }

            launchInProgress = true;
            view.SetStartInteractable(false);
            view.SetStatus("LOADING RUN...");
            StartCoroutine(LoadRunScene(result.RunSessionId));
        }

        private IEnumerator LoadRunScene(string runSessionId)
        {
            AsyncOperation operation = null;
            try
            {
                operation = SceneManager.LoadSceneAsync(
                    runSceneName,
                    LoadSceneMode.Single);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Could not begin run scene load: {exception}", this);
            }

            if (operation != null)
            {
                yield return operation;
                yield break;
            }

            GameSessionCommandResult cancel =
                sessionHost.Runtime.GameSession.TryCancelRunTransition(
                    runSessionId);
            if (!cancel.Success)
            {
                Debug.LogError(
                    $"Could not cancel failed run transition: {cancel.Error}.",
                    this);
            }

            launchInProgress = false;
            view.SetStartInteractable(true);
            view.SetStatus("RUN SCENE IS UNAVAILABLE");
        }

        private static int CreateRunSeed()
        {
            byte[] bytes = Guid.NewGuid().ToByteArray();
            return BitConverter.ToInt32(bytes, 0);
        }

        private bool TryResolveSessionHost()
        {
            if (sessionHost != null && sessionHost.IsInitialized)
                return true;

            GameSessionRuntimeHost[] hosts =
                FindObjectsByType<GameSessionRuntimeHost>(
                    FindObjectsInactive.Include);
            for (int i = 0; i < hosts.Length; i++)
            {
                if (hosts[i] == null || !hosts[i].IsInitialized)
                    continue;

                sessionHost = hosts[i];
                return true;
            }

            return false;
        }
    }
}
