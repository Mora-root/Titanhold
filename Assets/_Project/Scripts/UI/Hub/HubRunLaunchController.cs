using System;
using System.Collections;
using Titanhold.Run;
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
        [SerializeField]
        private string characterArchetypeId = "archetype:warrior";
        [SerializeField] private string startingAbilityId = "ability:spin";
        [SerializeField] private string difficultyId = "difficulty:prototype";
        [SerializeField] private string runSceneName = "SampleScene";

        private bool launchInProgress;
        private string pendingRunSessionId = string.Empty;
        private RunStartReadinessService pendingReadiness;
        private bool readinessSubscribed;

        public event Action<string, string>
            StartingAbilitySelectionRequired;

        public bool HasRequiredReferences => view != null && sessionHost != null;
        public HubRunPreparationView View => view;
        public GameSessionRuntimeHost SessionHost => sessionHost;
        public string PlayerId => playerId;
        public string CharacterId => characterId;
        public string CharacterArchetypeId => characterArchetypeId;
        public string StartingAbilityId => startingAbilityId;
        public string DifficultyId => difficultyId;
        public string RunSceneName => runSceneName;
        public bool IsWaitingForStartingAbility =>
            launchInProgress && pendingReadiness != null &&
            !pendingReadiness.IsSealed;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            HubRunPreparationView configuredView,
            GameSessionRuntimeHost configuredSessionHost,
            string configuredPlayerId,
            string configuredCharacterId,
            string configuredCharacterArchetypeId,
            string configuredStartingAbilityId,
            string configuredDifficultyId,
            string configuredRunSceneName)
        {
            view = configuredView;
            sessionHost = configuredSessionHost;
            playerId = configuredPlayerId;
            characterId = configuredCharacterId;
            characterArchetypeId = configuredCharacterArchetypeId;
            startingAbilityId = configuredStartingAbilityId;
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

            if (launchInProgress && pendingReadiness != null &&
                pendingReadiness.IsSealed)
            {
                BeginRunSceneLoad();
            }
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

        private void OnDestroy()
        {
            UnsubscribeReadiness();
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
                    new RunParticipantSelection(
                        playerId,
                        characterId,
                        characterArchetypeId,
                        startingAbilityId)
                });
            GameSessionCommandResult result =
                sessionHost.Runtime.GameSession.TryBeginRun(command);
            if (!result.Success)
            {
                view.SetStatus($"RUN REJECTED: {result.Error}");
                return;
            }

            launchInProgress = true;
            pendingRunSessionId = result.RunSessionId;
            view.SetStartInteractable(false);
            if (!sessionHost.Runtime.TryGetActiveRunStartReadiness(
                    result.RunSessionId,
                    out pendingReadiness))
            {
                TryCancelPendingLaunch(
                    "RUN PREPARATION FAILED",
                    "Active run start readiness is unavailable.");
                return;
            }

            if (pendingReadiness.IsSealed)
            {
                BeginRunSceneLoad();
                return;
            }

            pendingReadiness.ReadinessSealed += HandleReadinessSealed;
            readinessSubscribed = true;
            view.SetStatus("CHOOSE STARTING ABILITY");
            StartingAbilitySelectionRequired?.Invoke(
                result.RunSessionId,
                playerId);
        }

        private void HandleReadinessSealed()
        {
            if (isActiveAndEnabled)
                BeginRunSceneLoad();
        }

        private void BeginRunSceneLoad()
        {
            if (!launchInProgress || pendingRunSessionId.Length == 0 ||
                pendingReadiness == null || !pendingReadiness.IsSealed)
            {
                return;
            }

            string runSessionId = pendingRunSessionId;
            UnsubscribeReadiness();
            pendingReadiness = null;
            view.SetStatus("LOADING RUN...");
            StartCoroutine(LoadRunScene(runSessionId));
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

            ResetLaunchState("RUN SCENE IS UNAVAILABLE");
        }

        public bool TryCancelPendingLaunch(string status, string reason)
        {
            if (!launchInProgress)
                return false;

            string runSessionId = pendingRunSessionId;
            UnsubscribeReadiness();
            pendingReadiness = null;
            if (runSessionId.Length > 0)
            {
                GameSessionCommandResult cancel =
                    sessionHost.Runtime.GameSession.TryCancelRunTransition(
                        runSessionId);
                if (!cancel.Success)
                {
                    Debug.LogError(
                        $"Could not cancel invalid run preparation: {cancel.Error}.",
                        this);
                }
            }

            Debug.LogError(reason, this);
            ResetLaunchState(status);
            return true;
        }

        private void ResetLaunchState(string status)
        {
            launchInProgress = false;
            pendingRunSessionId = string.Empty;
            view.SetStartInteractable(true);
            view.SetStatus(status);
        }

        private void UnsubscribeReadiness()
        {
            if (readinessSubscribed && pendingReadiness != null)
            {
                pendingReadiness.ReadinessSealed -= HandleReadinessSealed;
            }

            readinessSubscribed = false;
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
