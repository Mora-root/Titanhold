using System;
using Titanhold.Combat.Flasks;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Titanhold.UI.Run.Editor
{
    public static class RunFlaskHudValidationRunner
    {
        [MenuItem("Tools/Titanhold/Validate Run Flask HUD Presentation")]
        public static void Validate()
        {
            GameObject fixture = null;
            try
            {
                fixture = new GameObject("RunFlaskHudValidation", typeof(RectTransform));
                RunFlaskSlotView slot = fixture.AddComponent<RunFlaskSlotView>();
                Image icon = CreateChild<Image>(fixture, "Icon");
                Image overlay = CreateChild<Image>(fixture, "Overlay");
                TMP_Text key = CreateText(fixture, "Key");
                TMP_Text name = CreateText(fixture, "Name");
                TMP_Text cooldown = CreateText(fixture, "Cooldown");
                slot.ConfigureForEditor(icon, overlay, key, name, cooldown);
                slot.RenderContent(
                    1,
                    "flask:primary-resource",
                    "Resource",
                    null,
                    "E");
                Assert(key.text == "E" &&
                       name.text == "Resource" &&
                       !icon.enabled &&
                       !overlay.gameObject.activeSelf &&
                       !cooldown.gameObject.activeSelf,
                    "Flask slot content did not render cleanly.");

                slot.RenderCooldown(new FlaskCooldownSnapshot(
                    1,
                    "flask:primary-resource",
                    30d,
                    12.2d));
                Assert(overlay.gameObject.activeSelf &&
                       Mathf.Abs(overlay.fillAmount - 12.2f / 30f) < 0.001f &&
                       cooldown.gameObject.activeSelf &&
                       cooldown.text == "13",
                    "Flask cooldown did not render from its snapshot.");

                RunFlaskHudView view = fixture.AddComponent<RunFlaskHudView>();
                view.ConfigureForEditor(new[] { slot });
                int pressed = -1;
                view.SlotPressed += index => pressed = index;
                Assert(slot.TryRequestUse() && pressed == 1,
                    "Flask click did not emit its slot index.");
                slot.RenderContent(1, string.Empty, string.Empty, null, string.Empty);
                Assert(!slot.TryRequestUse(),
                    "An unassigned flask slot emitted a use command.");

                Debug.Log("Run flask HUD presentation validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Run flask HUD presentation validation failed: {exception}");
            }
            finally
            {
                if (fixture != null)
                    UnityEngine.Object.DestroyImmediate(fixture);
            }
        }

        private static T CreateChild<T>(GameObject parent, string name)
            where T : Component
        {
            GameObject child = new(name, typeof(RectTransform));
            child.transform.SetParent(parent.transform, false);
            return child.AddComponent<T>();
        }

        private static TMP_Text CreateText(GameObject parent, string name)
        {
            return CreateChild<TextMeshProUGUI>(parent, name);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
