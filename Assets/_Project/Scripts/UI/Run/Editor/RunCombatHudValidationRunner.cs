using System;
using Titanhold.Combat.Abilities;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Titanhold.UI.Run.Editor
{
    public static class RunCombatHudValidationRunner
    {
        [MenuItem("Tools/Titanhold/Validate Run Combat HUD Presentation")]
        public static void Validate()
        {
            GameObject fixture = null;
            Sprite icon = null;
            try
            {
                fixture = new GameObject(
                    "RunCombatHudValidation",
                    typeof(RectTransform));
                RunAbilitySlotView slot =
                    fixture.AddComponent<RunAbilitySlotView>();
                Image iconImage = CreateChild<Image>(fixture, "Icon");
                Image cooldownOverlay =
                    CreateChild<Image>(fixture, "Cooldown");
                TMP_Text keyText = CreateText(fixture, "Key");
                TMP_Text abilityNameText = CreateText(fixture, "Name");
                TMP_Text cooldownText = CreateText(fixture, "Time");
                slot.ConfigureForEditor(
                    iconImage,
                    cooldownOverlay,
                    keyText,
                    abilityNameText,
                    cooldownText);

                Texture2D texture = new(2, 2);
                icon = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, 2f, 2f),
                    new Vector2(0.5f, 0.5f));
                slot.RenderContent(
                    4,
                    "ability:test",
                    "Test Ability",
                    icon);
                Assert(keyText.text == "5" &&
                       abilityNameText.text == "Test Ability" &&
                       iconImage.enabled &&
                       iconImage.sprite == icon &&
                       !cooldownOverlay.gameObject.activeSelf &&
                       !cooldownText.gameObject.activeSelf,
                    "Assigned ability content did not render cleanly.");

                slot.RenderCooldown(new AbilityCooldownSnapshot(
                    "ability:test",
                    5d,
                    2.4d));
                Assert(cooldownOverlay.gameObject.activeSelf &&
                       Mathf.Abs(cooldownOverlay.fillAmount - 0.48f) < 0.001f &&
                       cooldownText.gameObject.activeSelf &&
                       cooldownText.text == "3",
                    "Cooldown state did not render from its read-only snapshot.");

                slot.RenderReady();
                Assert(!cooldownOverlay.gameObject.activeSelf &&
                       cooldownOverlay.fillAmount == 0f &&
                       !cooldownText.gameObject.activeSelf,
                    "Ready state retained cooldown presentation.");

                TMP_Text resourceText = CreateText(fixture, "Resource");
                RunCombatHudView view =
                    fixture.AddComponent<RunCombatHudView>();
                view.ConfigureForEditor(
                    new[] { slot },
                    resourceText);
                view.RenderCombatResource("Rage", 3f, 8f);
                Assert(resourceText.text == "RAGE 3 / 8" &&
                       view.AbilitySlotCount == 1 &&
                       !view.TryRenderReady(1),
                    "Combat resource or bounded slot presentation is invalid.");

                int pressedSlotIndex = -1;
                int pressedCount = 0;
                view.AbilitySlotPressed += slotIndex =>
                {
                    pressedSlotIndex = slotIndex;
                    pressedCount++;
                };
                Assert(slot.TryRequestUse() &&
                       pressedSlotIndex == 4 &&
                       pressedCount == 1,
                    "Assigned ability click did not emit its slot index.");
                slot.RenderContent(4, string.Empty, string.Empty, null);
                Assert(!slot.TryRequestUse() && pressedCount == 1,
                    "Empty ability slot emitted a use request.");

                Debug.Log("Run Combat HUD presentation validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Run Combat HUD presentation validation failed: {exception}");
            }
            finally
            {
                if (icon != null)
                {
                    Texture2D texture = icon.texture;
                    UnityEngine.Object.DestroyImmediate(icon);
                    UnityEngine.Object.DestroyImmediate(texture);
                }

                if (fixture != null)
                    UnityEngine.Object.DestroyImmediate(fixture);
            }
        }

        private static T CreateChild<T>(
            GameObject parent,
            string name)
            where T : Component
        {
            GameObject child = new(name, typeof(RectTransform));
            child.transform.SetParent(parent.transform, false);
            return child.AddComponent<T>();
        }

        private static TMP_Text CreateText(
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
    }
}
