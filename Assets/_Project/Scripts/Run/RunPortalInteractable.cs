using System;
using Titanhold.Combat;
using UnityEngine;

namespace Titanhold.Run
{
    [DisallowMultipleComponent]
    public sealed class RunPortalInteractable : MonoBehaviour, ISelectable, IInteractable, IHoverable
    {
        [SerializeField] private RunFlowRuntime runFlowRuntime;
        [SerializeField] private Transform interactionPoint;
        [SerializeField, Min(0f)] private float interactionRange = 2f;

        private TargetVisual targetVisual;
        private int expectedRound;
        private bool initialized;

        public Transform InteractionPoint => interactionPoint != null ? interactionPoint : transform;
        public float InteractionRange => interactionRange;
        public bool IsInteractable =>
            initialized &&
            runFlowRuntime != null &&
            runFlowRuntime.State.Phase == RunPhase.PortalOpen &&
            runFlowRuntime.State.RoundNumber == expectedRound;
        public bool IsSelectable => IsInteractable;
        public int ExpectedRound => expectedRound;

        public event Action<RunPortalEntryResult> EntryResolved;

        private void Awake()
        {
            ResolveVisual();
        }

        public void Initialize(RunFlowRuntime runtime, int roundNumber)
        {
            runFlowRuntime = runtime;
            expectedRound = roundNumber;
            initialized = runtime != null && roundNumber > 0;
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

        public void Interact(GameObject interactor)
        {
            if (!IsInteractable)
                return;

            PlayerCombat playerCombat = interactor != null
                ? interactor.GetComponentInParent<PlayerCombat>()
                : null;
            CombatActorReference entrant = playerCombat != null
                ? playerCombat.ActorReference
                : CombatActorReference.Unknown;
            RunPortalEntryCommand command = new RunPortalEntryCommand(
                entrant,
                expectedRound);
            RunPortalEntryResult result = runFlowRuntime.PortalEntry.TryEnter(command);
            EntryResolved?.Invoke(result);
        }

        private void ResolveVisual()
        {
            targetVisual = GetComponent<TargetVisual>();
            targetVisual ??= GetComponentInChildren<TargetVisual>();
        }

        private void OnValidate()
        {
            interactionRange = Mathf.Max(0f, interactionRange);
        }
    }
}
