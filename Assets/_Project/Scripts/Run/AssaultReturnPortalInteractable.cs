using System;
using Titanhold.Combat;
using UnityEngine;

namespace Titanhold.Run
{
    [DisallowMultipleComponent]
    public sealed class AssaultReturnPortalInteractable :
        MonoBehaviour,
        ISelectable,
        IInteractable,
        IHoverable
    {
        [SerializeField] private Transform interactionPoint;
        [SerializeField, Min(0f)] private float interactionRange = 2f;

        private RunFlowRuntime runFlowRuntime;
        private AssaultArenaTransitionController transitionController;
        private AssaultTargetRegistry targetRegistry;
        private TargetVisual targetVisual;
        private int expectedRound;
        private bool initialized;

        public Transform InteractionPoint =>
            interactionPoint != null ? interactionPoint : transform;
        public float InteractionRange => interactionRange;
        public bool IsInteractable =>
            initialized &&
            runFlowRuntime != null &&
            transitionController != null &&
            targetRegistry != null &&
            runFlowRuntime.State.Phase == RunPhase.Intermission &&
            runFlowRuntime.State.RoundNumber == expectedRound;
        public bool IsSelectable => IsInteractable;
        public int ExpectedRound => expectedRound;

        public event Action<AssaultReturnPortalResult> ReturnResolved;

        private void Awake()
        {
            targetVisual = GetComponent<TargetVisual>();
            targetVisual ??= GetComponentInChildren<TargetVisual>();
        }

        public void Initialize(
            RunFlowRuntime runtime,
            AssaultArenaTransitionController controller,
            AssaultTargetRegistry registry,
            int roundNumber)
        {
            runFlowRuntime = runtime;
            transitionController = controller;
            targetRegistry = registry;
            expectedRound = roundNumber;
            initialized = runtime != null &&
                          controller != null &&
                          registry != null &&
                          roundNumber > 0;
        }

        public AssaultReturnPortalResult TryInteract(GameObject interactor)
        {
            CombatActorReference entrant = ResolveEntrant(interactor);
            AssaultReturnPortalResult result;

            if (!initialized)
            {
                result = AssaultReturnPortalResult.Failed(
                    AssaultReturnPortalError.NotInitialized,
                    entrant,
                    expectedRound);
            }
            else if (!entrant.IsValid || !entrant.IsPlayer)
            {
                result = AssaultReturnPortalResult.Failed(
                    AssaultReturnPortalError.InvalidEntrant,
                    entrant,
                    expectedRound);
            }
            else if (!targetRegistry.TryGet(entrant, out _))
            {
                result = AssaultReturnPortalResult.Failed(
                    AssaultReturnPortalError.EntrantNotRegistered,
                    entrant,
                    expectedRound);
            }
            else if (expectedRound <= 0)
            {
                result = AssaultReturnPortalResult.Failed(
                    AssaultReturnPortalError.InvalidExpectedRound,
                    entrant,
                    expectedRound);
            }
            else if (runFlowRuntime.State.RoundNumber != expectedRound)
            {
                result = AssaultReturnPortalResult.Failed(
                    AssaultReturnPortalError.StalePortal,
                    entrant,
                    expectedRound);
            }
            else if (runFlowRuntime.State.Phase != RunPhase.Intermission)
            {
                result = AssaultReturnPortalResult.Failed(
                    AssaultReturnPortalError.InvalidPhase,
                    entrant,
                    expectedRound);
            }
            else
            {
                AssaultArenaTransitionResult transition =
                    transitionController.TryReturnToExploration();
                result = transition.Success
                    ? AssaultReturnPortalResult.Succeeded(
                        entrant,
                        expectedRound,
                        transition)
                    : AssaultReturnPortalResult.Failed(
                        AssaultReturnPortalError.TransitionRejected,
                        entrant,
                        expectedRound,
                        transition);
            }

            ReturnResolved?.Invoke(result);
            return result;
        }

        public void Interact(GameObject interactor)
        {
            TryInteract(interactor);
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

        private static CombatActorReference ResolveEntrant(GameObject interactor)
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
        }
    }
}
