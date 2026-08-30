using System;
using Titanhold.Combat;
using UnityEngine;

namespace Titanhold.Run
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RunFlowRuntime))]
    [RequireComponent(typeof(AssaultWaveSpawner))]
    [RequireComponent(typeof(AssaultTargetRegistry))]
    public sealed class AssaultArenaTransitionController : MonoBehaviour
    {
        [SerializeField] private RunFlowRuntime runFlowRuntime;
        [SerializeField] private AssaultWaveSpawner waveSpawner;
        [SerializeField] private AssaultTargetRegistry targetRegistry;
        [SerializeField] private MonoBehaviour arenaGatewaySource;
        [SerializeField] private PlayerBrain localPlayer;

        private IAssaultArenaGateway arenaGateway;
        private bool isHandlingTransition;

        public bool HasGateway => ResolveGateway() != null;
        public bool HasPlayer => ResolvePlayer() != null;

        public event Action<AssaultArenaTransitionResult> EnteredAssault;
        public event Action<AssaultArenaTransitionResult> ReturnedToExploration;
        public event Action<AssaultArenaTransitionResult> TransitionFailed;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            if (runFlowRuntime != null)
                runFlowRuntime.StateChanged += HandleRunFlowStateChanged;
        }

        private void Start()
        {
            if (runFlowRuntime != null &&
                runFlowRuntime.State.Phase == RunPhase.TransitionToAssault)
            {
                TryEnterAssault();
            }
        }

        private void OnDisable()
        {
            if (runFlowRuntime != null)
                runFlowRuntime.StateChanged -= HandleRunFlowStateChanged;
        }

        public AssaultArenaTransitionResult TryEnterAssault()
        {
            if (isHandlingTransition)
            {
                return AssaultArenaTransitionResult.Failed(
                    AssaultArenaTransitionError.InvalidPhase);
            }

            ResolveReferences();
            if (runFlowRuntime == null)
                return Fail(AssaultArenaTransitionError.MissingRuntime);

            if (runFlowRuntime.State.Phase != RunPhase.TransitionToAssault)
                return Fail(AssaultArenaTransitionError.InvalidPhase);

            if (waveSpawner == null)
                return Fail(AssaultArenaTransitionError.MissingWaveSpawner);

            IAssaultArenaGateway gateway = ResolveGateway();
            if (gateway == null)
                return Fail(AssaultArenaTransitionError.MissingGateway);

            PlayerBrain player = ResolvePlayer();
            if (player == null)
                return Fail(AssaultArenaTransitionError.MissingPlayer);

            if (targetRegistry == null)
                return Fail(AssaultArenaTransitionError.MissingTargetRegistry);

            if (!player.TryGetComponent(out ITargetable playerTarget))
                return Fail(AssaultArenaTransitionError.MissingPlayerTarget);

            CombatActorReference playerActor = new CombatActorReference(
                $"player:{player.gameObject.GetEntityId()}",
                CombatActorKind.Player);
            targetRegistry.Clear();
            if (!targetRegistry.TryRegister(playerActor, playerTarget))
            {
                return Fail(
                    AssaultArenaTransitionError.TargetRegistrationRejected);
            }

            isHandlingTransition = true;
            try
            {
                PreparePlayerForTravel(player);
                AssaultArenaTravelResult travel = gateway.TryEnter(
                    player.transform);
                if (!travel.Success)
                {
                    targetRegistry.Clear();
                    return Fail(
                        AssaultArenaTransitionError.GatewayRejected,
                        travelResult: travel);
                }

                AssaultWaveStartResult wave = waveSpawner.TryStartWave();
                if (!wave.Success)
                {
                    AssaultArenaTravelResult rollback = gateway.TryReturn(
                        player.transform);
                    targetRegistry.Clear();
                    return Fail(
                        AssaultArenaTransitionError.WaveRejected,
                        travelResult: rollback,
                        waveResult: wave);
                }

                AssaultArenaTransitionResult result =
                    AssaultArenaTransitionResult.Succeeded(travel, wave);
                EnteredAssault?.Invoke(result);
                return result;
            }
            finally
            {
                isHandlingTransition = false;
            }
        }

        public AssaultArenaTransitionResult TryReturnToExploration()
        {
            if (isHandlingTransition)
            {
                return AssaultArenaTransitionResult.Failed(
                    AssaultArenaTransitionError.InvalidPhase);
            }

            ResolveReferences();
            if (runFlowRuntime == null)
                return Fail(AssaultArenaTransitionError.MissingRuntime);

            if (runFlowRuntime.State.Phase != RunPhase.Intermission &&
                runFlowRuntime.State.Phase != RunPhase.ReturningToExploration)
            {
                return Fail(AssaultArenaTransitionError.InvalidPhase);
            }

            IAssaultArenaGateway gateway = ResolveGateway();
            if (gateway == null)
                return Fail(AssaultArenaTransitionError.MissingGateway);

            PlayerBrain player = ResolvePlayer();
            if (player == null)
                return Fail(AssaultArenaTransitionError.MissingPlayer);

            isHandlingTransition = true;
            try
            {
                RunFlowTransitionResult beginReturn = default;
                if (runFlowRuntime.State.Phase == RunPhase.Intermission)
                {
                    beginReturn = runFlowRuntime.Service.TryBeginReturnToExploration();
                    if (!beginReturn.Success)
                    {
                        return Fail(
                            AssaultArenaTransitionError.FlowRejected,
                            flowResult: beginReturn);
                    }
                }

                PreparePlayerForTravel(player);
                AssaultArenaTravelResult travel = gateway.TryReturn(
                    player.transform);
                if (!travel.Success)
                {
                    return Fail(
                        AssaultArenaTransitionError.GatewayRejected,
                        travelResult: travel,
                        flowResult: beginReturn);
                }

                RunFlowTransitionResult resume =
                    runFlowRuntime.Service.TryResumeExploration();
                if (!resume.Success)
                {
                    return Fail(
                        AssaultArenaTransitionError.FlowRejected,
                        travelResult: travel,
                        flowResult: resume);
                }

                targetRegistry?.Clear();

                AssaultArenaTransitionResult result =
                    AssaultArenaTransitionResult.Succeeded(
                        travel,
                        flowResult: resume);
                ReturnedToExploration?.Invoke(result);
                return result;
            }
            finally
            {
                isHandlingTransition = false;
            }
        }

        private void HandleRunFlowStateChanged(RunFlowState state)
        {
            if (state.Phase == RunPhase.TransitionToAssault)
                TryEnterAssault();
        }

        private AssaultArenaTransitionResult Fail(
            AssaultArenaTransitionError error,
            AssaultArenaTravelResult travelResult = default,
            AssaultWaveStartResult waveResult = default,
            RunFlowTransitionResult flowResult = default)
        {
            AssaultArenaTransitionResult result =
                AssaultArenaTransitionResult.Failed(
                    error,
                    travelResult,
                    waveResult,
                    flowResult);
            TransitionFailed?.Invoke(result);
            return result;
        }

        private void PreparePlayerForTravel(PlayerBrain player)
        {
            player.Stop();
            player.ClearAllSelections();
            player.Input.ClearAll();
        }

        private void ResolveReferences()
        {
            runFlowRuntime ??= GetComponent<RunFlowRuntime>();
            waveSpawner ??= GetComponent<AssaultWaveSpawner>();
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
                if (behaviours[i] is IAssaultArenaGateway discoveredGateway)
                {
                    arenaGatewaySource = behaviours[i];
                    arenaGateway = discoveredGateway;
                    return arenaGateway;
                }
            }

            return null;
        }

        private PlayerBrain ResolvePlayer()
        {
            if (localPlayer == null)
                localPlayer = FindAnyObjectByType<PlayerBrain>();

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
