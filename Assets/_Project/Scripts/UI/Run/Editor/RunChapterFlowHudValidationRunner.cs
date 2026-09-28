using System;
using TMPro;
using Titanhold.Run;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Titanhold.UI.Run.Editor
{
    public static class RunChapterFlowHudValidationRunner
    {
        [MenuItem("Tools/Titanhold/Validate Run Chapter Flow HUD")]
        public static void Validate()
        {
            GameObject root = new("RunChapterFlowHud_Validation");
            try
            {
                RunChapterFlowHudView view =
                    root.AddComponent<RunChapterFlowHudView>();
                Slider slider = CreateChild<Slider>(root, "Progress");
                TextMeshProUGUI title = CreateText(root, "Title");
                TextMeshProUGUI progress = CreateText(root, "ProgressText");
                TextMeshProUGUI stage = CreateText(root, "Stage");
                TextMeshProUGUI collapse = CreateText(root, "Collapse");
                TextMeshProUGUI instability = CreateText(root, "Instability");
                TextMeshProUGUI scaling = CreateText(root, "Scaling");
                TextMeshProUGUI portal = CreateText(root, "Portal");
                view.ConfigureForEditor(
                    slider,
                    title,
                    progress,
                    stage,
                    collapse,
                    instability,
                    scaling,
                    portal);

                RunChapterFlowService flow = new(
                    new RunChapterFlowConfiguration(
                        chapterNumber: 2,
                        maximumProgress: 200f,
                        normalizedEscalationThresholds: new[]
                        {
                            0f,
                            0.2f,
                            0.4f,
                            0.6f,
                            0.8f
                        },
                        collapseDurationSeconds: 90d,
                        instabilityStackIntervalSeconds: 10d,
                        bossHealthBonusPerStack: 1.5625f,
                        bossDamageBonusPerStack: 0.625f));
                RunChapterFlowPresentationProjection projection = new(
                    flow.State);
                Assert(flow.TryAddProgress(200f, 10d).Success &&
                       flow.TryAdvanceTime(57.8d).Success,
                    "Could not prepare an authoritative HUD snapshot.");
                RunChapterFlowPresentationSnapshot semanticSnapshot =
                    projection.Capture();
                Assert(view.Render(semanticSnapshot),
                    "Configured HUD rejected a valid snapshot.");
                AssertApproximately(slider.value, 1f,
                    "Normalized progress");
                Assert(progress.text == "200/200" &&
                       stage.text == "STAGE 5/5" &&
                       collapse.text == "RIFT COLLAPSE 43s" &&
                       instability.text == "INSTABILITY 4/9",
                    "HUD did not render authoritative progress values.");
                Assert(scaling.text.Contains("7.25") &&
                       scaling.text.Contains("3.50"),
                    "HUD recalculated or lost authoritative boss scaling.");
                Assert(collapse.gameObject.activeSelf &&
                       portal.gameObject.activeSelf &&
                       portal.text == "BOSS PORTAL AVAILABLE",
                    "HUD ignored semantic portal availability.");

                Assert(flow.TryEnterBossPortal(60d).Success,
                    "Could not prepare an unavailable portal snapshot.");
                Assert(view.Render(projection.Capture()) &&
                       !collapse.gameObject.activeSelf &&
                       !portal.gameObject.activeSelf,
                    "HUD ignored semantic portal availability.");

                Debug.Log("Run Chapter Flow HUD validation passed.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static T CreateChild<T>(GameObject parent, string name)
            where T : Component
        {
            GameObject child = new(name, typeof(RectTransform));
            child.transform.SetParent(parent.transform, false);
            return child.AddComponent<T>();
        }

        private static TextMeshProUGUI CreateText(
            GameObject parent,
            string name)
        {
            return CreateChild<TextMeshProUGUI>(parent, name);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static void AssertApproximately(
            float actual,
            float expected,
            string label)
        {
            if (Math.Abs(actual - expected) <= 0.0001f)
                return;

            throw new InvalidOperationException(
                $"{label} failed. Expected {expected}, got {actual}.");
        }
    }
}
