using System;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Run.Editor
{
    public static class AssaultArenaGatewayValidationRunner
    {
        [MenuItem("Tools/Titanhold/Validate Assault Arena Gateway")]
        public static void Validate()
        {
            GameObject gatewayObject = new GameObject(
                "AssaultArenaGateway_Validation");
            GameObject destinationObject = new GameObject(
                "AssaultArenaDestination_Validation");
            GameObject actorObject = new GameObject(
                "AssaultArenaActor_Validation");
            GameObject otherActorObject = new GameObject(
                "AssaultArenaOtherActor_Validation");

            try
            {
                Vector3 originalPosition = new Vector3(2f, 0f, 3f);
                Quaternion originalRotation = Quaternion.Euler(0f, 35f, 0f);
                Vector3 destinationPosition = new Vector3(20f, 1f, -7f);
                Quaternion destinationRotation = Quaternion.Euler(0f, 180f, 0f);
                actorObject.transform.SetPositionAndRotation(
                    originalPosition,
                    originalRotation);
                destinationObject.transform.SetPositionAndRotation(
                    destinationPosition,
                    destinationRotation);

                LocalAssaultArenaGateway gateway =
                    gatewayObject.AddComponent<LocalAssaultArenaGateway>();
                SerializedObject serializedGateway = new SerializedObject(gateway);
                serializedGateway.FindProperty("assaultDestination")
                    .objectReferenceValue = destinationObject.transform;
                serializedGateway.ApplyModifiedPropertiesWithoutUndo();

                IAssaultArenaGateway contract = gateway;
                AssaultArenaTravelResult entered = contract.TryEnter(
                    actorObject.transform);
                Assert(entered.Success && contract.IsOccupied,
                    "Gateway did not retain the arena occupant.");
                AssertPosition(actorObject.transform, destinationPosition,
                    "Gateway did not move the actor to the arena.");
                Assert(
                    contract.TryEnter(otherActorObject.transform).Error ==
                    AssaultArenaTravelError.AlreadyOccupied,
                    "Gateway accepted a second arena occupant.");
                Assert(
                    contract.TryReturn(otherActorObject.transform).Error ==
                    AssaultArenaTravelError.ActorMismatch,
                    "Gateway returned a different actor.");

                AssaultArenaTravelResult returned = contract.TryReturn(
                    actorObject.transform);
                Assert(returned.Success && !contract.IsOccupied,
                    "Gateway did not release the arena occupant.");
                AssertPosition(actorObject.transform, originalPosition,
                    "Gateway did not restore the exploration position.");
                Assert(Quaternion.Angle(
                        actorObject.transform.rotation,
                        originalRotation) <= 0.01f,
                    "Gateway did not restore the exploration rotation.");

                Debug.Log("Assault Arena Gateway validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Assault Arena Gateway validation failed: {exception}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gatewayObject);
                UnityEngine.Object.DestroyImmediate(destinationObject);
                UnityEngine.Object.DestroyImmediate(actorObject);
                UnityEngine.Object.DestroyImmediate(otherActorObject);
            }
        }

        private static void AssertPosition(
            Transform transform,
            Vector3 expected,
            string message)
        {
            Assert(Vector3.Distance(transform.position, expected) <= 0.001f,
                message);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
