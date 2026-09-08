using System;
using Titanhold.Combat.Effects;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Combat.Editor
{
    public static class TimedStatEffectReceiverWiringEditor
    {
        private static readonly string[] ActiveEnemyPrefabPaths =
        {
            "Assets/_Project/Prefabs/Enemy/Skelet.prefab",
            "Assets/_Project/Prefabs/Enemy/Skelet_Warrior.prefab",
            "Assets/_Project/Prefabs/Enemy/Skelet_Assault.prefab",
            "Assets/_Project/Prefabs/Enemy/Skelet_Boss_Prototype.prefab"
        };

        [MenuItem("Tools/Titanhold/Install Timed Stat Effect Receiver Wiring")]
        public static void Install()
        {
            RequireEditMode();

            for (int i = 0; i < ActiveEnemyPrefabPaths.Length; i++)
                InstallPrefab(ActiveEnemyPrefabPaths[i]);

            AssetDatabase.SaveAssets();
            Debug.Log(
                "Timed stat effect receiver wiring installed on active enemy prefabs.");
        }

        [MenuItem("Tools/Titanhold/Validate Timed Stat Effect Receiver Wiring")]
        public static void Validate()
        {
            RequireEditMode();

            for (int i = 0; i < ActiveEnemyPrefabPaths.Length; i++)
                ValidatePrefabAsset(ActiveEnemyPrefabPaths[i]);

            Debug.Log(
                "Timed stat effect receiver wiring validation passed.");
        }

        private static void InstallPrefab(string prefabPath)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
            if (contents == null)
                throw new InvalidOperationException($"Could not load {prefabPath}.");

            try
            {
                CharacterStats stats = RequireOrCreateSingleStats(
                    contents,
                    prefabPath);
                Health health = RequireSingleHealth(contents, prefabPath);
                TimedStackingStatEffectReceiver[] receivers =
                    contents.GetComponentsInChildren<TimedStackingStatEffectReceiver>(true);

                if (receivers.Length > 1)
                {
                    throw new InvalidOperationException(
                        $"{prefabPath} contains more than one timed stat effect receiver.");
                }

                TimedStackingStatEffectReceiver receiver = receivers.Length == 0
                    ? stats.gameObject.AddComponent<TimedStackingStatEffectReceiver>()
                    : receivers[0];

                if (receiver.gameObject != stats.gameObject)
                {
                    throw new InvalidOperationException(
                        $"{prefabPath} has a timed stat effect receiver outside its CharacterStats object.");
                }

                SerializedObject serializedReceiver = new(receiver);
                SerializedProperty statsProperty =
                    serializedReceiver.FindProperty("characterStats");
                if (statsProperty == null)
                {
                    throw new InvalidOperationException(
                        "TimedStackingStatEffectReceiver.characterStats is missing.");
                }

                statsProperty.objectReferenceValue = stats;
                serializedReceiver.ApplyModifiedPropertiesWithoutUndo();

                SerializedObject serializedHealth = new(health);
                SerializedProperty healthStatsProperty =
                    serializedHealth.FindProperty("characterStats");
                if (healthStatsProperty == null)
                {
                    throw new InvalidOperationException(
                        "Health.characterStats is missing.");
                }

                healthStatsProperty.objectReferenceValue = stats;
                serializedHealth.ApplyModifiedPropertiesWithoutUndo();
                ValidatePrefabContents(contents, prefabPath);

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(
                    contents,
                    prefabPath,
                    out bool success);
                if (!success || saved == null)
                    throw new InvalidOperationException($"Could not save {prefabPath}.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void ValidatePrefabAsset(string prefabPath)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
            if (contents == null)
                throw new InvalidOperationException($"Could not load {prefabPath}.");

            try
            {
                ValidatePrefabContents(contents, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void ValidatePrefabContents(
            GameObject contents,
            string prefabPath)
        {
            CharacterStats stats = RequireSingleStats(contents, prefabPath);
            Health health = RequireSingleHealth(contents, prefabPath);
            TimedStackingStatEffectReceiver[] receivers =
                contents.GetComponentsInChildren<TimedStackingStatEffectReceiver>(true);

            if (receivers.Length != 1)
            {
                throw new InvalidOperationException(
                    $"{prefabPath} must contain exactly one timed stat effect receiver.");
            }

            TimedStackingStatEffectReceiver receiver = receivers[0];
            SerializedProperty statsProperty =
                new SerializedObject(receiver).FindProperty("characterStats");
            SerializedProperty healthStatsProperty =
                new SerializedObject(health).FindProperty("characterStats");
            if (receiver.gameObject != stats.gameObject ||
                statsProperty == null ||
                statsProperty.objectReferenceValue != stats ||
                health.gameObject != stats.gameObject ||
                healthStatsProperty == null ||
                healthStatsProperty.objectReferenceValue != stats)
            {
                throw new InvalidOperationException(
                    $"{prefabPath} has stale timed stat effect receiver wiring.");
            }
        }

        private static CharacterStats RequireOrCreateSingleStats(
            GameObject contents,
            string prefabPath)
        {
            CharacterStats[] stats =
                contents.GetComponentsInChildren<CharacterStats>(true);
            if (stats.Length > 1)
            {
                throw new InvalidOperationException(
                    $"{prefabPath} contains more than one CharacterStats component.");
            }

            if (stats.Length == 1)
                return stats[0];

            Health health = RequireSingleHealth(contents, prefabPath);
            return health.gameObject.AddComponent<CharacterStats>();
        }

        private static CharacterStats RequireSingleStats(
            GameObject contents,
            string prefabPath)
        {
            CharacterStats[] stats =
                contents.GetComponentsInChildren<CharacterStats>(true);
            if (stats.Length != 1)
            {
                throw new InvalidOperationException(
                    $"{prefabPath} must contain exactly one CharacterStats component.");
            }

            return stats[0];
        }

        private static Health RequireSingleHealth(
            GameObject contents,
            string prefabPath)
        {
            Health[] health = contents.GetComponentsInChildren<Health>(true);
            if (health.Length != 1)
            {
                throw new InvalidOperationException(
                    $"{prefabPath} must contain exactly one Health component.");
            }

            return health[0];
        }

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Timed stat effect receiver wiring is available only outside Play Mode.");
            }
        }
    }
}
