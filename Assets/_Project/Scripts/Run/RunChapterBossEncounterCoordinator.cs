using System;
using Titanhold.Combat;
using Titanhold.Enemies;
using UnityEngine;

namespace Titanhold.Run
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RunChapterFlowRuntime))]
    public sealed class RunChapterBossEncounterCoordinator : MonoBehaviour
    {
        [SerializeField] private RunChapterFlowRuntime chapterFlowRuntime;
        [SerializeField] private MonoBehaviour arenaGatewaySource;
        [SerializeField] private AssaultTargetRegistry targetRegistry;
        [SerializeField] private EnemyDefinitionCatalog enemyDefinitions;
        [SerializeField] private GameObject bossPrefab;
        [SerializeField] private Transform bossSpawnPoint;
        [SerializeField] private PlayerBrain localPlayer;

        private readonly EnemyScalingApplicator scalingApplicator = new();
        private IAssaultArenaGateway arenaGateway;
        private EnemyDeathNotifier activeBossDeathNotifier;
        private bool isStartingEncounter;

        public RunChapterFlowRuntime ChapterFlowRuntime => chapterFlowRuntime;
        public MonoBehaviour ArenaGatewaySource => arenaGatewaySource;
        public AssaultTargetRegistry TargetRegistry => targetRegistry;
        public EnemyDefinitionCatalog EnemyDefinitions => enemyDefinitions;
        public GameObject BossPrefab => bossPrefab;
        public Transform BossSpawnPoint => bossSpawnPoint;
        public PlayerBrain LocalPlayer => localPlayer;
        public GameObject ActiveBoss { get; private set; }
        public bool IsEncounterActive => ActiveBoss != null;
        public bool IsConfigured =>
            chapterFlowRuntime != null &&
            arenaGatewaySource is IAssaultArenaGateway &&
            targetRegistry != null &&
            enemyDefinitions != null &&
            enemyDefinitions.IsValid &&
            bossPrefab != null &&
            bossSpawnPoint != null &&
            localPlayer != null;

        public event Action<RunChapterBossEncounterStartResult>
            EncounterStarted;
        public event Action<RunChapterBossEncounterStartResult>
            EncounterStartFailed;
        public event Action<RunChapterFlowResult> BossDefeated;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            RunChapterFlowRuntime configuredChapterFlowRuntime,
            MonoBehaviour configuredArenaGatewaySource,
            AssaultTargetRegistry configuredTargetRegistry,
            EnemyDefinitionCatalog configuredEnemyDefinitions,
            GameObject configuredBossPrefab,
            Transform configuredBossSpawnPoint,
            PlayerBrain configuredLocalPlayer)
        {
            chapterFlowRuntime = configuredChapterFlowRuntime;
            arenaGatewaySource = configuredArenaGatewaySource;
            targetRegistry = configuredTargetRegistry;
            enemyDefinitions = configuredEnemyDefinitions;
            bossPrefab = configuredBossPrefab;
            bossSpawnPoint = configuredBossSpawnPoint;
            localPlayer = configuredLocalPlayer;
            arenaGateway = null;
        }
#endif

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            if (ActiveBoss != null)
            {
                activeBossDeathNotifier =
                    ActiveBoss.GetComponentInChildren<EnemyDeathNotifier>(true);
                if (activeBossDeathNotifier != null)
                    activeBossDeathNotifier.Died += HandleBossDied;
            }

            if (chapterFlowRuntime != null)
            {
                chapterFlowRuntime.BossTransitionRequested +=
                    HandleBossTransitionRequested;
            }
        }

        private void Start()
        {
            if (chapterFlowRuntime != null &&
                chapterFlowRuntime.TransitionApplication.HasTransitionRequest &&
                chapterFlowRuntime.State.Phase ==
                    RunChapterPhase.TransitionToBoss)
            {
                StartEncounter(
                    chapterFlowRuntime.TransitionApplication.TransitionRequest);
            }
        }

        private void OnDisable()
        {
            if (chapterFlowRuntime != null)
            {
                chapterFlowRuntime.BossTransitionRequested -=
                    HandleBossTransitionRequested;
            }

            DetachBossDeathNotifier();
        }

        public RunChapterBossEncounterStartResult TryStartPendingEncounter()
        {
            ResolveReferences();
            if (chapterFlowRuntime == null ||
                !chapterFlowRuntime.TransitionApplication.HasTransitionRequest)
            {
                return RunChapterBossEncounterStartResult.Failed(
                    RunChapterBossEncounterStartError.InvalidTransitionRequest);
            }

            return TryStartEncounter(
                chapterFlowRuntime.TransitionApplication.TransitionRequest);
        }

        public RunChapterBossEncounterStartResult TryStartEncounter(
            RunChapterBossTransitionRequest request)
        {
            if (!request.IsValid)
            {
                return RunChapterBossEncounterStartResult.Failed(
                    RunChapterBossEncounterStartError.InvalidTransitionRequest);
            }

            if (isStartingEncounter || IsEncounterActive)
            {
                return RunChapterBossEncounterStartResult.Failed(
                    RunChapterBossEncounterStartError.EncounterAlreadyStarted,
                    request);
            }

            ResolveReferences();
            if (chapterFlowRuntime == null)
            {
                return RunChapterBossEncounterStartResult.Failed(
                    RunChapterBossEncounterStartError.InvalidChapterPhase,
                    request);
            }

            RunChapterBossTransitionApplicationService transitionApplication =
                chapterFlowRuntime.TransitionApplication;
            if (!transitionApplication.HasTransitionRequest ||
                !string.Equals(
                    request.EventId,
                    transitionApplication.TransitionRequest.EventId,
                    StringComparison.Ordinal))
            {
                return RunChapterBossEncounterStartResult.Failed(
                    RunChapterBossEncounterStartError.InvalidTransitionRequest,
                    request);
            }

            request = transitionApplication.TransitionRequest;
            if (chapterFlowRuntime.State.Phase !=
                    RunChapterPhase.TransitionToBoss ||
                !string.Equals(
                    request.RunId,
                    chapterFlowRuntime.RunId,
                    StringComparison.Ordinal) ||
                request.ChapterNumber !=
                    chapterFlowRuntime.State.ChapterNumber)
            {
                return RunChapterBossEncounterStartResult.Failed(
                    RunChapterBossEncounterStartError.InvalidChapterPhase,
                    request);
            }

            IAssaultArenaGateway gateway = ResolveGateway();
            if (gateway == null)
            {
                return RunChapterBossEncounterStartResult.Failed(
                    RunChapterBossEncounterStartError.MissingArenaGateway,
                    request);
            }

            PlayerBrain player = ResolvePlayer();
            if (player == null)
            {
                return RunChapterBossEncounterStartResult.Failed(
                    RunChapterBossEncounterStartError.MissingPlayer,
                    request);
            }

            if (!chapterFlowRuntime.TryResolveParticipantId(
                    player.gameObject,
                    out string participantId))
            {
                return RunChapterBossEncounterStartResult.Failed(
                    RunChapterBossEncounterStartError.MissingParticipant,
                    request);
            }

            if (!player.TryGetComponent(out ITargetable playerTarget))
            {
                return RunChapterBossEncounterStartResult.Failed(
                    RunChapterBossEncounterStartError.MissingPlayerTarget,
                    request);
            }

            if (targetRegistry == null)
            {
                return RunChapterBossEncounterStartResult.Failed(
                    RunChapterBossEncounterStartError.MissingTargetRegistry,
                    request);
            }

            if (bossPrefab == null)
            {
                return RunChapterBossEncounterStartResult.Failed(
                    RunChapterBossEncounterStartError.MissingBossPrefab,
                    request);
            }

            if (bossSpawnPoint == null)
            {
                return RunChapterBossEncounterStartResult.Failed(
                    RunChapterBossEncounterStartError.MissingBossSpawnPoint,
                    request);
            }

            if (enemyDefinitions == null || !enemyDefinitions.IsValid)
            {
                return RunChapterBossEncounterStartResult.Failed(
                    RunChapterBossEncounterStartError.InvalidEnemyDefinitions,
                    request);
            }

            isStartingEncounter = true;
            AssaultArenaTravelResult travel = default;
            EnemyDefinitionInitializationResult initialization = default;
            EnemyScalingResult scaling = default;
            try
            {
                PreparePlayerForTravel(player);
                travel = gateway.TryEnter(player.transform);
                if (!travel.Success)
                {
                    return RunChapterBossEncounterStartResult.Failed(
                        RunChapterBossEncounterStartError.ArenaTravelRejected,
                        request,
                        travel);
                }

                targetRegistry.Clear();
                CombatActorReference playerActor = new(
                    participantId,
                    CombatActorKind.Player);
                if (!targetRegistry.TryRegister(playerActor, playerTarget))
                {
                    RollBackTravel(gateway, player);
                    return RunChapterBossEncounterStartResult.Failed(
                        RunChapterBossEncounterStartError.
                            TargetRegistrationRejected,
                        request,
                        travel);
                }

                ActiveBoss = Instantiate(
                    bossPrefab,
                    bossSpawnPoint.position,
                    bossSpawnPoint.rotation);
                initialization =
                    EnemyDefinitionInstanceInitializer.TryInitialize(
                        ActiveBoss,
                        enemyDefinitions);
                if (!initialization.Success)
                {
                    RollBackEncounterStart(gateway, player);
                    return RunChapterBossEncounterStartResult.Failed(
                        RunChapterBossEncounterStartError.
                            EnemyDefinitionInitializationRejected,
                        request,
                        travel,
                        initialization);
                }

                Health health = ActiveBoss.GetComponentInChildren<Health>(true);
                EnemyCombat combat =
                    ActiveBoss.GetComponentInChildren<EnemyCombat>(true);
                EnemyScalingSnapshot scalingSnapshot = new(
                    request.ChapterNumber,
                    request.BossScaling.HealthMultiplier,
                    request.BossScaling.DamageMultiplier);
                scaling = scalingApplicator.TryApply(
                    health,
                    combat,
                    scalingSnapshot,
                    restoreFullHealth: true);
                if (!scaling.Success)
                {
                    RollBackEncounterStart(gateway, player);
                    return RunChapterBossEncounterStartResult.Failed(
                        RunChapterBossEncounterStartError.BossScalingRejected,
                        request,
                        travel,
                        initialization,
                        scaling);
                }

                AssaultAggroTargetProvider targetProvider =
                    ActiveBoss.GetComponentInChildren<
                        AssaultAggroTargetProvider>(true);
                if (targetProvider == null)
                {
                    RollBackEncounterStart(gateway, player);
                    return RunChapterBossEncounterStartResult.Failed(
                        RunChapterBossEncounterStartError.
                            MissingBossTargetProvider,
                        request,
                        travel,
                        initialization,
                        scaling);
                }

                targetProvider.Bind(targetRegistry);
                if (!targetProvider.IsBound ||
                    targetProvider.GetTarget() == null)
                {
                    RollBackEncounterStart(gateway, player);
                    return RunChapterBossEncounterStartResult.Failed(
                        RunChapterBossEncounterStartError.
                            BossTargetBindingRejected,
                        request,
                        travel,
                        initialization,
                        scaling);
                }

                activeBossDeathNotifier =
                    ActiveBoss.GetComponentInChildren<EnemyDeathNotifier>(true);
                if (activeBossDeathNotifier == null ||
                    !activeBossDeathNotifier.isActiveAndEnabled)
                {
                    RollBackEncounterStart(gateway, player);
                    return RunChapterBossEncounterStartResult.Failed(
                        RunChapterBossEncounterStartError.
                            MissingBossDeathNotifier,
                        request,
                        travel,
                        initialization,
                        scaling);
                }

                activeBossDeathNotifier.Died += HandleBossDied;
                RunChapterFlowResult flowResult =
                    chapterFlowRuntime.Service.TryStartBoss();
                if (!flowResult.Success)
                {
                    RollBackEncounterStart(gateway, player);
                    return RunChapterBossEncounterStartResult.Failed(
                        RunChapterBossEncounterStartError.ChapterFlowRejected,
                        request,
                        travel,
                        initialization,
                        scaling,
                        flowResult);
                }

                return RunChapterBossEncounterStartResult.Succeeded(
                    request,
                    travel,
                    initialization,
                    scaling,
                    flowResult);
            }
            finally
            {
                isStartingEncounter = false;
            }
        }

        private void HandleBossTransitionRequested(
            RunChapterBossTransitionRequest request)
        {
            StartEncounter(request);
        }

        private void StartEncounter(RunChapterBossTransitionRequest request)
        {
            RunChapterBossEncounterStartResult result =
                TryStartEncounter(request);
            if (result.Success)
            {
                EncounterStarted?.Invoke(result);
                return;
            }

            Debug.LogError(
                $"Chapter boss encounter could not start: {result.Error}.",
                this);
            EncounterStartFailed?.Invoke(result);
        }

        private void HandleBossDied(EnemyDeathNotifier notifier)
        {
            if (notifier == null || notifier != activeBossDeathNotifier)
                return;

            DetachBossDeathNotifier();
            ActiveBoss = null;
            targetRegistry?.Clear();
            RunChapterFlowResult result =
                chapterFlowRuntime.Service.TryDefeatBoss();
            if (!result.Success)
            {
                Debug.LogError(
                    $"Chapter boss death was rejected: {result.Error}.",
                    this);
                return;
            }

            BossDefeated?.Invoke(result);
        }

        private void RollBackEncounterStart(
            IAssaultArenaGateway gateway,
            PlayerBrain player)
        {
            DetachBossDeathNotifier();
            if (ActiveBoss != null)
            {
                ActiveBoss.SetActive(false);
                Destroy(ActiveBoss);
            }

            ActiveBoss = null;
            targetRegistry?.Clear();
            RollBackTravel(gateway, player);
        }

        private static void RollBackTravel(
            IAssaultArenaGateway gateway,
            PlayerBrain player)
        {
            if (gateway != null && player != null)
                gateway.TryReturn(player.transform);
        }

        private void DetachBossDeathNotifier()
        {
            if (activeBossDeathNotifier != null)
                activeBossDeathNotifier.Died -= HandleBossDied;

            activeBossDeathNotifier = null;
        }

        private static void PreparePlayerForTravel(PlayerBrain player)
        {
            player.Stop();
            player.ClearAllSelections();
            player.ClearQueuedAction();
            player.Input.ClearAll();
        }

        private void ResolveReferences()
        {
            chapterFlowRuntime ??= GetComponent<RunChapterFlowRuntime>();
            targetRegistry ??= GetComponent<AssaultTargetRegistry>();
            ResolveGateway();
            ResolvePlayer();
        }

        private IAssaultArenaGateway ResolveGateway()
        {
            if (arenaGateway != null)
                return arenaGateway;

            if (arenaGatewaySource is IAssaultArenaGateway configuredGateway)
            {
                arenaGateway = configuredGateway;
                return arenaGateway;
            }

            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is not IAssaultArenaGateway discoveredGateway)
                    continue;

                arenaGatewaySource = behaviours[i];
                arenaGateway = discoveredGateway;
                return arenaGateway;
            }

            return null;
        }

        private PlayerBrain ResolvePlayer()
        {
            localPlayer ??= FindAnyObjectByType<PlayerBrain>();
            return localPlayer;
        }

        private void OnValidate()
        {
            if (arenaGatewaySource != null &&
                arenaGatewaySource is not IAssaultArenaGateway)
            {
                arenaGatewaySource = null;
            }
        }
    }
}
