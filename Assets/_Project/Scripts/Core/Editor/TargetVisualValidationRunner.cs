using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Core.Editor
{
    public static class TargetVisualValidationRunner
    {
        private const string MenuPath =
            "Tools/Titanhold/Validate Target Hover Rendering";

        [MenuItem(MenuPath)]
        public static void Validate()
        {
            GameObject root = null;
            Material material = null;

            try
            {
                root = new GameObject("TargetVisualValidation");
                GameObject visualObject =
                    GameObject.CreatePrimitive(PrimitiveType.Cube);
                visualObject.transform.SetParent(root.transform, false);

                Renderer renderer = visualObject.GetComponent<Renderer>();
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                Assert(shader != null, "URP Lit shader was not found.");

                material = new Material(shader);
                Color baseColor = new(0.25f, 0.4f, 0.6f, 0.8f);
                material.SetColor("_BaseColor", baseColor);
                renderer.sharedMaterial = material;

                TargetVisual targetVisual = root.AddComponent<TargetVisual>();
                InvokeAwake(targetVisual);
                targetVisual.SetHover(true);

                MaterialPropertyBlock block = new();
                renderer.GetPropertyBlock(block, 0);
                Color highlighted = block.GetColor("_BaseColor");
                Assert(highlighted.r > baseColor.r &&
                       highlighted.g > baseColor.g &&
                       highlighted.b > baseColor.b,
                    "Hover did not brighten the renderer color.");
                Assert(Mathf.Approximately(highlighted.a, baseColor.a),
                    "Hover changed the renderer alpha.");
                Assert(ReferenceEquals(renderer.sharedMaterial, material),
                    "Hover instantiated or replaced the shared material.");
                Assert(!material.IsKeywordEnabled("_EMISSION"),
                    "Hover enabled the build-strippable emission keyword.");

                targetVisual.SetHover(false);
                block.Clear();
                renderer.GetPropertyBlock(block, 0);
                Color restored = block.GetColor("_BaseColor");
                Assert(AreApproximatelyEqual(restored, baseColor),
                    "Hover exit did not restore the original color.");

                Debug.Log("Target hover rendering validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Target hover rendering validation failed: {exception}");
                throw;
            }
            finally
            {
                if (root != null)
                    UnityEngine.Object.DestroyImmediate(root);
                if (material != null)
                    UnityEngine.Object.DestroyImmediate(material);
            }
        }

        private static void InvokeAwake(TargetVisual targetVisual)
        {
            MethodInfo awake = typeof(TargetVisual).GetMethod(
                "Awake",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert(awake != null, "TargetVisual.Awake was not found.");
            awake.Invoke(targetVisual, null);
        }

        private static bool AreApproximatelyEqual(Color left, Color right)
        {
            return Mathf.Approximately(left.r, right.r) &&
                   Mathf.Approximately(left.g, right.g) &&
                   Mathf.Approximately(left.b, right.b) &&
                   Mathf.Approximately(left.a, right.a);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
