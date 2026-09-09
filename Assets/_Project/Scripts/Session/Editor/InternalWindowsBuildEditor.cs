using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Titanhold.Session.Editor
{
    public static class InternalWindowsBuildEditor
    {
        private const string OutputPath =
            "Builds/InternalWindows/Titanhold.exe";

        [MenuItem("Tools/Titanhold/Configure Internal Windows Player")]
        public static void ConfigurePlayer()
        {
            try
            {
                PlayerSettings.defaultScreenWidth = 1280;
                PlayerSettings.defaultScreenHeight = 720;
                PlayerSettings.defaultIsNativeResolution = false;
                PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
                PlayerSettings.resizableWindow = true;
                AssetDatabase.SaveAssets();
                Debug.Log("Internal Windows Player settings configured.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Internal Windows Player configuration failed: {exception}");
            }
        }

        [MenuItem("Tools/Titanhold/Build Internal Windows Development Player")]
        public static void Build()
        {
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    throw new InvalidOperationException(
                        "Exit Play Mode before building the Player.");
                }

                string[] scenes = GetEnabledScenes();
                if (scenes.Length == 0)
                    throw new InvalidOperationException("No build scenes are enabled.");

                string directory = Path.GetDirectoryName(OutputPath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                BuildPlayerOptions options = new()
                {
                    scenes = scenes,
                    locationPathName = OutputPath,
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.Development |
                              BuildOptions.AllowDebugging |
                              BuildOptions.StrictMode
                };
                BuildReport report = BuildPipeline.BuildPlayer(options);
                BuildSummary summary = report.summary;
                if (summary.result != BuildResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Player build ended with {summary.result} and " +
                        $"{summary.totalErrors} error(s).");
                }

                Debug.Log(
                    $"Internal Windows Development Player built at " +
                    $"'{OutputPath}' ({summary.totalSize} bytes)." );
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Internal Windows Development Player build failed: {exception}");
            }
        }

        private static string[] GetEnabledScenes()
        {
            List<string> scenes = new();
            EditorBuildSettingsScene[] configured = EditorBuildSettings.scenes;
            for (int i = 0; i < configured.Length; i++)
            {
                if (configured[i].enabled &&
                    !string.IsNullOrWhiteSpace(configured[i].path))
                {
                    scenes.Add(configured[i].path);
                }
            }

            return scenes.ToArray();
        }
    }
}
