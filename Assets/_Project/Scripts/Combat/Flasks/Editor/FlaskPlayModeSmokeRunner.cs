using System;
using System.Collections;
using Titanhold.UI.Run;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Titanhold.Combat.Flasks.Editor
{
    public static class FlaskPlayModeSmokeRunner
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/SampleScene.unity";
        private const string PendingKey =
            "Titanhold.Flasks.PlayModeSmokePending";

        private static IEnumerator routine;
        private static double deadline;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
#pragma warning disable UDR0001
            EditorApplication.playModeStateChanged -= HandlePlayModeChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeChanged;
#pragma warning restore UDR0001
        }

        [MenuItem("Tools/Titanhold/Run Flask Play Mode Smoke Test")]
        public static void Start()
        {
            Scene scene = SceneManager.GetActiveScene();
            Require(
                !EditorApplication.isPlayingOrWillChangePlaymode &&
                !scene.isDirty &&
                scene.path == ScenePath,
                "Open the saved SampleScene outside Play Mode before the flask smoke test.");
            SessionState.SetBool(PendingKey, true);
            EditorApplication.isPlaying = true;
        }

        private static void HandlePlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode &&
                SessionState.GetBool(PendingKey, false))
            {
                routine = Run();
                deadline = EditorApplication.timeSinceStartup + 20d;
                Application.runInBackground = true;
#pragma warning disable UDR0001
                EditorApplication.update -= Tick;
                EditorApplication.update += Tick;
#pragma warning restore UDR0001
            }
            else if (state == PlayModeStateChange.ExitingPlayMode &&
                     (routine != null || SessionState.GetBool(PendingKey, false)))
            {
                Stop();
            }
        }

        private static void Tick()
        {
            try
            {
                Require(
                    EditorApplication.timeSinceStartup < deadline,
                    "Flask Play Mode smoke test timed out.");
                EditorApplication.QueuePlayerLoopUpdate();
                if (routine != null && routine.MoveNext())
                    return;

                Debug.Log(
                    "Flask Play Mode smoke test passed: health/resource recovery, independent cooldowns, prefab and HUD binding.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Flask Play Mode smoke test failed: {exception}");
            }

            Stop();
            EditorApplication.isPlaying = false;
        }

        private static void Stop()
        {
            (routine as IDisposable)?.Dispose();
            routine = null;
            Time.timeScale = 1f;
            SessionState.SetBool(PendingKey, false);
            EditorApplication.update -= Tick;
        }

        private static IEnumerator Run()
        {
            yield return null;
            PlayerFlaskController flasks =
                Object.FindAnyObjectByType<PlayerFlaskController>();
            RunFlaskHudPresenter hud =
                Object.FindAnyObjectByType<RunFlaskHudPresenter>();
            Require(flasks != null && flasks.TryInitialize() && flasks.SlotCount == 2,
                "Scene player did not inherit two initialized flask slots.");
            Require(hud != null && hud.TryBind(),
                "Run flask HUD did not bind to the explicit participant.");

            Health health = flasks.Health;
            PlayerResource resource = flasks.PrimaryResource;
            health.enabled = false;
            resource.enabled = false;
            health.RestoreFull();
            resource.RestoreFull();

            health.TakeDamage(health.MaxHealth * 0.6f);
            float healthBefore = health.CurrentHealth;
            float expectedHealth = Mathf.Min(
                health.MaxHealth,
                healthBefore + health.MaxHealth * 0.5f);
            FlaskUseResult healthUse = flasks.TryUseSlot(0);
            Require(healthUse.Success &&
                    Mathf.Approximately(health.CurrentHealth, expectedHealth),
                "Health flask did not restore 50% of current maximum health.");

            Require(resource.TrySpend(resource.MaxResource * 0.75f),
                "Could not prepare primary-resource flask smoke state.");
            float resourceBefore = resource.CurrentResource;
            float expectedResource = Mathf.Min(
                resource.MaxResource,
                resourceBefore + resource.MaxResource * 0.5f);
            FlaskUseResult resourceUse = flasks.TryUseSlot(1);
            Require(resourceUse.Success &&
                    Mathf.Approximately(resource.CurrentResource, expectedResource),
                "Primary-resource flask did not restore 50% of current maximum.");
            Require(flasks.TryUseSlot(0).Status == FlaskUseStatus.CoolingDown &&
                    flasks.TryUseSlot(1).Status == FlaskUseStatus.CoolingDown,
                "Flask cooldowns were not committed independently.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
