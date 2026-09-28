using System;
using UnityEngine;

namespace Titanhold.Run
{
    [DisallowMultipleComponent]
    public sealed class RunChapterFlowRuntime : MonoBehaviour
    {
        [SerializeField] private RunChapterFlowDefinition definition;

        private RunChapterFlowService service;

        public RunChapterFlowDefinition Definition => definition;
        public RunChapterFlowService Service
        {
            get
            {
                EnsureInitialized();
                return service;
            }
        }

        public RunChapterFlowState State => Service.State;

        public event Action<RunChapterFlowState> StateChanged;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void Update()
        {
            if (service == null ||
                service.State.Phase != RunChapterPhase.RiftCollapse)
            {
                return;
            }

            service.TryAdvanceTime(Time.timeAsDouble);
        }

        private void OnDestroy()
        {
            if (service != null)
                service.StateChanged -= HandleStateChanged;
        }

        public RunChapterFlowResult TryAddProgress(float amount)
        {
            return Service.TryAddProgress(amount, Time.timeAsDouble);
        }

        public RunChapterFlowResult TryEnterBossPortal()
        {
            return Service.TryEnterBossPortal(Time.timeAsDouble);
        }

        public void EnsureInitialized()
        {
            if (service != null)
                return;

            if (definition == null)
            {
                throw new InvalidOperationException(
                    "Run chapter flow definition is missing.");
            }

            if (!definition.TryCreateConfiguration(
                    out RunChapterFlowConfiguration configuration,
                    out string error))
            {
                throw new InvalidOperationException(error);
            }

            service = new RunChapterFlowService(configuration);
            service.StateChanged += HandleStateChanged;
        }

        private void HandleStateChanged(RunChapterFlowState state)
        {
            StateChanged?.Invoke(state);
        }
    }
}
