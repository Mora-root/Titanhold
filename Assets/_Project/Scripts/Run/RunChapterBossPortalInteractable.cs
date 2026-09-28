using System;
using Titanhold.Combat;
using UnityEngine;

namespace Titanhold.Run
{
    [DisallowMultipleComponent]
    public sealed class RunChapterBossPortalInteractable :
        MonoBehaviour,
        ISelectable,
        IInteractable,
        IHoverable
    {
        [SerializeField] private Transform interactionPoint;
        [SerializeField, Min(0f)] private float interactionRange = 2f;

        private RunChapterFlowRuntime chapterFlowRuntime;
        private TargetVisual targetVisual;
        private int expectedChapterNumber;
        private bool initialized;

        public Transform InteractionPoint =>
            interactionPoint != null ? interactionPoint : transform;
        public float InteractionRange => interactionRange;
        public int ExpectedChapterNumber => expectedChapterNumber;
        public bool IsInteractable
        {
            get
            {
                if (!initialized || chapterFlowRuntime == null)
                    return false;

                RunChapterFlowPresentationSnapshot snapshot =
                    chapterFlowRuntime.CapturePresentationSnapshot();
                return snapshot.IsBossPortalAvailable &&
                       snapshot.ChapterNumber == expectedChapterNumber;
            }
        }
        public bool IsSelectable => IsInteractable;

        public event Action<RunChapterBossTransitionApplicationResult>
            EntryResolved;

        private void Awake()
        {
            ResolveVisual();
        }

        public void Initialize(
            RunChapterFlowRuntime runtime,
            int chapterNumber)
        {
            chapterFlowRuntime = runtime;
            expectedChapterNumber = chapterNumber;
            initialized = runtime != null && chapterNumber > 0;
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
            if (!IsInteractable ||
                !chapterFlowRuntime.TryResolveParticipantId(
                    interactor,
                    out string participantId))
            {
                return;
            }

            string eventId =
                $"portal:{chapterFlowRuntime.RunId}:chapter:" +
                $"{expectedChapterNumber}:participant:{participantId}:" +
                Guid.NewGuid().ToString("N");
            RunChapterBossPortalEntryCommand command = new(
                eventId,
                participantId,
                expectedChapterNumber,
                Time.timeAsDouble);
            RunChapterBossTransitionApplicationResult result =
                chapterFlowRuntime.TryEnterBossPortal(command);
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
