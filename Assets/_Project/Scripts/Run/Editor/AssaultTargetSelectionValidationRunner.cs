using System;
using Titanhold.Combat;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Run.Editor
{
    public static class AssaultTargetSelectionValidationRunner
    {
        [MenuItem("Tools/Titanhold/Validate Assault Target Selection")]
        public static void Validate()
        {
            GameObject runtimeObject = null;
            GameObject enemyObject = null;
            GameObject firstAimPoint = null;
            GameObject secondAimPoint = null;

            try
            {
                runtimeObject = new GameObject("AssaultTargetRegistry_Validation");
                AssaultTargetRegistry registry =
                    runtimeObject.AddComponent<AssaultTargetRegistry>();
                enemyObject = new GameObject("AssaultEnemy_Validation");
                AssaultAggroTargetProvider provider =
                    enemyObject.AddComponent<AssaultAggroTargetProvider>();
                firstAimPoint = new GameObject("FirstPlayer_Validation");
                secondAimPoint = new GameObject("SecondPlayer_Validation");
                firstAimPoint.transform.position = Vector3.right * 10f;
                secondAimPoint.transform.position = Vector3.right * 2f;

                MutableTarget firstTarget =
                    new MutableTarget(firstAimPoint.transform);
                MutableTarget secondTarget =
                    new MutableTarget(secondAimPoint.transform);
                CombatActorReference firstActor = new CombatActorReference(
                    "player:validation:first",
                    CombatActorKind.Player);
                CombatActorReference secondActor = new CombatActorReference(
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
                    "Initial target is not the nearest participant.");
                Assert(provider.CurrentTargetActor == secondActor,
                    "Initial target actor was not retained.");

                Assert(provider.TrySetCurrentTarget(firstActor),
                    "Explicit aggro target change failed.");
                Assert(ReferenceEquals(provider.GetTarget(), firstTarget),
                    "Explicit aggro target was not retained.");

                firstTarget.IsAvailable = false;
                Assert(ReferenceEquals(provider.GetTarget(), secondTarget),
                    "Invalid target did not trigger target reselection.");

                Assert(registry.Unregister(secondActor),
                    "Participant removal failed.");
                Assert(provider.GetTarget() == null,
                    "Provider retained a removed participant.");

                Debug.Log("Assault target selection validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Assault target selection validation failed: {exception}");
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
