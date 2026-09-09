using System;
using Titanhold.Combat;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Enemies.Editor
{
    public static class ExplorationTargetingValidationRunner
    {
        [MenuItem("Tools/Titanhold/Validate Exploration Target Selection")]
        public static void Validate()
        {
            GameObject runtimeObject = null;
            GameObject enemyObject = null;
            GameObject firstAimPoint = null;
            GameObject secondAimPoint = null;

            try
            {
                runtimeObject = new GameObject(
                    "ExplorationTargetRegistry_Validation");
                ExplorationTargetRegistry registry =
                    runtimeObject.AddComponent<ExplorationTargetRegistry>();
                enemyObject = new GameObject("ExplorationEnemy_Validation");
                EnemySensor sensor = enemyObject.AddComponent<EnemySensor>();
                ExplorationAggroTargetProvider provider =
                    enemyObject.AddComponent<ExplorationAggroTargetProvider>();

                SerializedObject serializedSensor = new(sensor);
                serializedSensor.FindProperty("aggroRange").floatValue = 5f;
                serializedSensor.ApplyModifiedPropertiesWithoutUndo();
                SerializedObject serializedProvider = new(provider);
                serializedProvider.FindProperty("localAggroSensor")
                    .objectReferenceValue = sensor;
                serializedProvider.ApplyModifiedPropertiesWithoutUndo();

                firstAimPoint = new GameObject("FirstPlayer_Validation");
                secondAimPoint = new GameObject("SecondPlayer_Validation");
                firstAimPoint.transform.position = Vector3.right * 8f;
                secondAimPoint.transform.position = Vector3.right * 2f;

                MutableTarget firstTarget =
                    new(firstAimPoint.transform);
                MutableTarget secondTarget =
                    new(secondAimPoint.transform);
                CombatActorReference firstActor = new(
                    "player:validation:first",
                    CombatActorKind.Player);
                CombatActorReference secondActor = new(
                    "player:validation:second",
                    CombatActorKind.Player);

                Assert(registry.TryRegister(firstActor, firstTarget),
                    "First participant registration failed.");
                Assert(registry.TryRegister(secondActor, secondTarget),
                    "Second participant registration failed.");
                Assert(!registry.TryRegister(secondActor, secondTarget),
                    "Duplicate participant registration was accepted.");

                provider.Bind(registry);
                Assert(ReferenceEquals(provider.GetTarget(), secondTarget),
                    "Provider did not select the nearest in-range participant.");
                Assert(provider.CurrentTargetActor == secondActor,
                    "Selected participant identity was not retained.");

                Assert(provider.TrySetCurrentTarget(firstActor),
                    "Explicit target assignment failed.");
                Assert(ReferenceEquals(provider.GetTarget(), secondTarget),
                    "An out-of-range explicit target was retained.");

                firstAimPoint.transform.position = Vector3.right;
                Assert(provider.TrySetCurrentTarget(firstActor),
                    "In-range explicit target assignment failed.");
                Assert(ReferenceEquals(provider.GetTarget(), firstTarget),
                    "In-range explicit target was not retained.");

                firstTarget.IsAvailable = false;
                Assert(ReferenceEquals(provider.GetTarget(), secondTarget),
                    "Invalid target did not trigger reselection.");
                Assert(registry.Unregister(secondActor),
                    "Participant removal failed.");
                Assert(provider.GetTarget() == null,
                    "Provider retained a removed participant.");

                Debug.Log("Exploration target selection validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Exploration target selection validation failed: {exception}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(firstAimPoint);
                UnityEngine.Object.DestroyImmediate(secondAimPoint);
                UnityEngine.Object.DestroyImmediate(enemyObject);
                UnityEngine.Object.DestroyImmediate(runtimeObject);
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private sealed class MutableTarget : ITargetable
        {
            public MutableTarget(Transform aimPoint)
            {
                AimPoint = aimPoint;
                IsAvailable = true;
            }

            public Transform AimPoint { get; }
            public bool IsAvailable { get; set; }
            public bool IsTargetable => IsAvailable;
        }
    }
}
