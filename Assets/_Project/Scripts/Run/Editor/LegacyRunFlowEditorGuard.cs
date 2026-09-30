using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Titanhold.Run.Editor
{
    internal static class LegacyRunFlowEditorGuard
    {
        public static bool IsChapterCutoverActive()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
                return false;

            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                RunChapterFlowRuntime[] runtimes =
                    roots[rootIndex].GetComponentsInChildren<
                        RunChapterFlowRuntime>(true);
                for (int runtimeIndex = 0;
                     runtimeIndex < runtimes.Length;
                     runtimeIndex++)
                {
                    RunChapterFlowRuntime runtime = runtimes[runtimeIndex];
                    if (runtime != null && runtime.isActiveAndEnabled)
                        return true;
                }
            }

            return false;
        }

        public static void RequireChapterCutoverInactive(string operation)
        {
            if (!IsChapterCutoverActive())
                return;

            throw new InvalidOperationException(
                $"Cannot {operation} while the chapter cutover is active. " +
                "Disable the chapter flow explicitly as part of a rollback first.");
        }
    }
}
