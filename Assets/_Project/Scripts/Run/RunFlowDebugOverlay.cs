using UnityEngine;

namespace Titanhold.Run
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RunFlowRuntime))]
    public sealed class RunFlowDebugOverlay : MonoBehaviour
    {
        [SerializeField] private RunFlowRuntime runFlowRuntime;
        [SerializeField] private ExplorationCombatExecutionAdapter executionAdapter;
        [SerializeField] private bool visible = true;
        [SerializeField] private Rect screenRect = new Rect(12f, 12f, 320f, 112f);

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnGUI()
        {
            if (!visible)
                return;

            ResolveReferences();
            if (runFlowRuntime == null)
                return;

            RunFlowState state = runFlowRuntime.State;
            string lastBatch = "none";
            if (executionAdapter != null && executionAdapter.HasLastApplicationResult)
            {
                ExplorationKillApplicationResult result = executionAdapter.LastApplicationResult;
                lastBatch = result.Success
                    ? $"ok ({result.AcceptedKillCount} kills)"
                    : $"rejected: {result.Error}";
            }

            GUI.Box(
                screenRect,
                $"Run Flow (new)\n" +
                $"Phase: {state.Phase} | Round: {state.RoundNumber}\n" +
                $"Threat: {state.CurrentThreat:0.##}/{state.MaxThreat:0.##}\n" +
                $"Instability: {state.RiftInstability.Points} (level {state.RiftInstability.Level})\n" +
                $"Last batch: {lastBatch}");
        }

        private void ResolveReferences()
        {
            runFlowRuntime ??= GetComponent<RunFlowRuntime>();
            executionAdapter ??= GetComponent<ExplorationCombatExecutionAdapter>();
        }
    }
}
