using System;
using Titanhold.Combat;
using UnityEngine;

namespace Titanhold.Run
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(WorldLootDropEmitter))]
    public sealed class AssaultRewardChestInteractable :
        MonoBehaviour,
        ISelectable,
        IInteractable,
        IHoverable
    {
        [SerializeField] private Transform interactionPoint;
        [SerializeField, Min(0f)] private float interactionRange = 2f;
        [SerializeField] private WorldLootDropEmitter dropEmitter;
        [SerializeField, Min(0f)] private float openedLifetime = 0.35f;

        private RunFlowRuntime runFlowRuntime;
        private AssaultTargetRegistry targetRegistry;
        private TargetVisual targetVisual;
        private AssaultEncounterId encounterId;
        private int expectedRound;
        private bool initialized;

        public Transform InteractionPoint =>
            interactionPoint != null ? interactionPoint : transform;
        public float InteractionRange => interactionRange;
        public bool IsInteractable =>
            initialized &&
            runFlowRuntime != null &&
            targetRegistry != null &&
            runFlowRuntime.State.Phase == RunPhase.Intermission &&
            runFlowRuntime.State.RoundNumber == expectedRound &&
            runFlowRuntime.AssaultReward.State.HasReward &&
            !runFlowRuntime.AssaultReward.State.IsClaimed &&
            runFlowRuntime.AssaultReward.State.EncounterId == encounterId;
        public bool IsSelectable => IsInteractable;
        public AssaultEncounterId EncounterId => encounterId;
        public int ExpectedRound => expectedRound;

        public event Action<AssaultRewardChestResult> OpenResolved;

        private void Awake()
        {
            dropEmitter ??= GetComponent<WorldLootDropEmitter>();
            targetVisual = GetComponent<TargetVisual>();
            targetVisual ??= GetComponentInChildren<TargetVisual>();
        }

        public void Initialize(
            RunFlowRuntime runtime,
            AssaultTargetRegistry registry,
            AssaultEncounterId rewardEncounterId,
            int roundNumber)
        {
            runFlowRuntime = runtime;
            targetRegistry = registry;
            encounterId = rewardEncounterId;
            expectedRound = roundNumber;
            initialized = runtime != null &&
                          registry != null &&
                          rewardEncounterId.IsValid &&
                          roundNumber > 0;
        }

        public AssaultRewardChestResult TryOpen(GameObject interactor)
        {
            CombatActorReference entrant = ResolveEntrant(interactor);
            WorldLootEmissionError emissionError =
                WorldLootEmissionError.EmptyDrops;
            AssaultRewardChestResult result;

            if (!initialized)
            {
                result = Fail(AssaultRewardChestError.NotInitialized, entrant);
            }
            else if (!entrant.IsValid || !entrant.IsPlayer)
            {
                result = Fail(AssaultRewardChestError.InvalidEntrant, entrant);
            }
            else if (!targetRegistry.TryGet(entrant, out _))
            {
                result = Fail(
                    AssaultRewardChestError.EntrantNotRegistered,
                    entrant);
            }
            else if (expectedRound <= 0)
            {
                result = Fail(
                    AssaultRewardChestError.InvalidExpectedRound,
                    entrant);
            }
            else if (!encounterId.IsValid)
            {
                result = Fail(
                    AssaultRewardChestError.InvalidEncounterId,
                    entrant);
            }
            else if (runFlowRuntime.State.Phase != RunPhase.Intermission)
            {
                result = Fail(AssaultRewardChestError.InvalidPhase, entrant);
            }
            else if (runFlowRuntime.State.RoundNumber != expectedRound ||
                     !runFlowRuntime.AssaultReward.State.HasReward ||
                     runFlowRuntime.AssaultReward.State.EncounterId != encounterId ||
                     runFlowRuntime.AssaultReward.State.RoundNumber != expectedRound)
            {
                result = Fail(AssaultRewardChestError.StaleChest, entrant);
            }
            else if (runFlowRuntime.AssaultReward.State.IsClaimed)
            {
                result = Fail(
                    AssaultRewardChestError.RewardAlreadyClaimed,
                    entrant);
            }
            else if (dropEmitter == null ||
                     !dropEmitter.CanEmit(
                         runFlowRuntime.AssaultReward.State.Drops,
                         out emissionError))
            {
                result = AssaultRewardChestResult.Failed(
                    AssaultRewardChestError.InvalidDropConfiguration,
                    entrant,
                    encounterId,
                    expectedRound,
                    emissionResult: WorldLootEmissionResult.Failed(
                        dropEmitter == null
                            ? WorldLootEmissionError.EmptyDrops
                            : emissionError));
            }
            else
            {
                AssaultRewardResult rewardResult =
                    runFlowRuntime.AssaultReward.TryClaim(
                        new ClaimAssaultRewardCommand(
                            encounterId,
                            expectedRound,
                            entrant));
                if (!rewardResult.Success)
                {
                    result = AssaultRewardChestResult.Failed(
                        AssaultRewardChestError.ClaimRejected,
                        entrant,
                        encounterId,
                        expectedRound,
                        rewardResult);
                }
                else
                {
                    WorldLootEmissionResult emissionResult =
                        dropEmitter.TryEmit(
                            runFlowRuntime.AssaultReward.State.Drops);
                    if (!emissionResult.Success)
                    {
                        result = AssaultRewardChestResult.Failed(
                            AssaultRewardChestError.EmissionRejected,
                            entrant,
                            encounterId,
                            expectedRound,
                            rewardResult,
                            emissionResult);
                    }
                    else
                    {
                        DisableInteraction();
                        Destroy(gameObject, openedLifetime);
                        result = AssaultRewardChestResult.Succeeded(
                            entrant,
                            encounterId,
                            expectedRound,
                            rewardResult,
                            emissionResult);
                    }
                }
            }

            OpenResolved?.Invoke(result);
            return result;
        }

        public void Interact(GameObject interactor)
        {
            TryOpen(interactor);
        }

        public void OnSelected()
        {
            targetVisual?.SetSelected(IsSelectable);
        }

        public void OnDeselected()
        {
            targetVisual?.SetSelected(false);
        }

        public void OnHoverEnter()
        {
            targetVisual?.SetHover(IsInteractable);
        }

        public void OnHoverExit()
        {
            targetVisual?.SetHover(false);
        }

        private AssaultRewardChestResult Fail(
            AssaultRewardChestError error,
            CombatActorReference entrant)
        {
            return AssaultRewardChestResult.Failed(
                error,
                entrant,
                encounterId,
                expectedRound);
        }

        private void DisableInteraction()
        {
            targetVisual?.SetHover(false);
            targetVisual?.SetSelected(false);

            Collider[] colliders = GetComponentsInChildren<Collider>();
            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = false;
        }

        private static CombatActorReference ResolveEntrant(
            GameObject interactor)
        {
            PlayerCombat playerCombat = interactor != null
                ? interactor.GetComponentInParent<PlayerCombat>()
                : null;
            return playerCombat != null
                ? playerCombat.ActorReference
                : CombatActorReference.Unknown;
        }

        private void OnValidate()
        {
            interactionRange = Mathf.Max(0f, interactionRange);
            openedLifetime = Mathf.Max(0f, openedLifetime);
            dropEmitter ??= GetComponent<WorldLootDropEmitter>();
        }
    }
}
