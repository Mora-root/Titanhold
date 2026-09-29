using System;
using UnityEngine;

namespace Titanhold.Run
{
    [DisallowMultipleComponent]
    public sealed class RunChapterExitPortalInteractable :
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
                return snapshot.IsExitPortalAvailable &&
                       snapshot.ChapterNumber == expectedChapterNumber;
            }
        }
        public bool IsSelectable => IsInteractable;

        public event Action<RunChapterExitPortalRequest> ExitRequested;

        private void Awake()
        {
            targetVisual = GetComponent<TargetVisual>();
            targetVisual ??= GetComponentInChildren<TargetVisual>();
        }

        public void Initialize(
            RunChapterFlowRuntime runtime,
            int chapterNumber)
        {
            chapterFlowRuntime = runtime;
            expectedChapterNumber = chapterNumber;
            initialized = runtime != null && chapterNumber > 0;
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

            RunChapterExitPortalRequest request = new(
                "exit-portal:" + chapterFlowRuntime.RunId +
                ":chapter:" + expectedChapterNumber +
                ":participant:" + participantId + ":" +
                Guid.NewGuid().ToString("N"),
                chapterFlowRuntime.RunId,
                participantId,
                expectedChapterNumber);
            ExitRequested?.Invoke(request);
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

        private void OnValidate()
        {
            interactionRange = Mathf.Max(0f, interactionRange);
        }
    }
}
