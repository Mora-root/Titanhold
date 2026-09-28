using Titanhold.Run;
using UnityEngine;

namespace Titanhold.UI.Run
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RunChapterFlowHudView))]
    public sealed class RunChapterFlowHudPresenter : MonoBehaviour
    {
        [SerializeField] private RunChapterFlowRuntime chapterFlowRuntime;
        [SerializeField] private RunChapterFlowHudView view;

        public RunChapterFlowRuntime ChapterFlowRuntime => chapterFlowRuntime;
        public RunChapterFlowHudView View => view;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            RunChapterFlowRuntime configuredRuntime,
            RunChapterFlowHudView configuredView)
        {
            chapterFlowRuntime = configuredRuntime;
            view = configuredView;
        }
#endif

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            RenderCurrentSnapshot();
        }

        private void Update()
        {
            RenderCurrentSnapshot();
        }

        public bool RenderCurrentSnapshot()
        {
            if (chapterFlowRuntime == null || view == null)
                return false;

            return view.Render(
                chapterFlowRuntime.CapturePresentationSnapshot());
        }

        private void ResolveReferences()
        {
            view ??= GetComponent<RunChapterFlowHudView>();
            chapterFlowRuntime ??=
                FindAnyObjectByType<RunChapterFlowRuntime>(
                    FindObjectsInactive.Include);
        }
    }
}
