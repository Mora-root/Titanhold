using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class PlayerActionCommandValidationRunner
{
    [MenuItem("Tools/Titanhold/Validate Player Action Commands")]
    public static void Validate()
    {
        GameObject fixture = null;
        try
        {
            fixture = new GameObject("PlayerActionCommandValidation");
            PlayerInput input = fixture.AddComponent<PlayerInput>();
            fixture.AddComponent<TargetSelection>();
            PlayerBrain brain = fixture.AddComponent<PlayerBrain>();
            InvokeAwake(brain);

            input.SetMoveTarget(new Vector3(10f, 0f, 5f));
            Assert(brain.HasMoveTarget,
                "Validation move target was not established.");

            ValidationSelectable accepted = new(true);
            Assert(brain.TrySubmitActionSelection(accepted) &&
                   ReferenceEquals(brain.ActionTarget, accepted) &&
                   !brain.HasMoveTarget,
                "Accepted action did not replace the previous move target.");

            input.SetMoveTarget(new Vector3(-4f, 0f, 2f));
            ValidationSelectable rejected = new(false);
            Assert(!brain.TrySubmitActionSelection(rejected) &&
                   ReferenceEquals(brain.ActionTarget, accepted) &&
                   brain.HasMoveTarget,
                "Rejected action mutated the current action or move target.");

            Debug.Log("Player action command validation passed.");
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"Player action command validation failed: {exception}");
        }
        finally
        {
            if (fixture != null)
                UnityEngine.Object.DestroyImmediate(fixture);
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void InvokeAwake(PlayerBrain brain)
    {
        MethodInfo awake = typeof(PlayerBrain).GetMethod(
            "Awake",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (awake == null)
            throw new InvalidOperationException("PlayerBrain.Awake was not found.");

        awake.Invoke(brain, null);
    }

    private sealed class ValidationSelectable : ISelectable
    {
        public ValidationSelectable(bool isSelectable)
        {
            IsSelectable = isSelectable;
        }

        public bool IsSelectable { get; }
        public void OnSelected() { }
        public void OnDeselected() { }
    }
}
