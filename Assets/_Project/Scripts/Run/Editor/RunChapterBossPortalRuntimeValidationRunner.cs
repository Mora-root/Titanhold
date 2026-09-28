using System;
using Titanhold.Session;
using UnityEditor;
using UnityEngine;

namespace Titanhold.Run.Editor
{
    public static class RunChapterBossPortalRuntimeValidationRunner
    {
        [MenuItem("Tools/Titanhold/Validate Run Chapter Boss Portal Runtime")]
        public static void Validate()
        {
            GameObject participantObject = new("Participant");
            GameObject entryObject = new("SessionEntry");
            GameObject runtimeObject = new("ChapterRuntime");
            GameObject portalObject = new("ChapterPortal");
            RunChapterFlowDefinition definition =
                ScriptableObject.CreateInstance<RunChapterFlowDefinition>();
            try
            {
                PlayerInventory inventory =
                    participantObject.AddComponent<PlayerInventory>();
                PlayerEquipmentRuntime equipment =
                    participantObject.AddComponent<PlayerEquipmentRuntime>();
                PlayerExperience experience =
                    participantObject.AddComponent<PlayerExperience>();
                PlayerGold gold =
                    participantObject.AddComponent<PlayerGold>();
                RunSceneParticipantBinding participant = new(
                    "player:validation",
                    "character:validation",
                    inventory,
                    equipment,
                    experience,
                    gold);
                Assert(participant.IsValid,
                    "Validation participant binding is invalid.");

                RunSceneSessionEntryPoint entryPoint =
                    entryObject.AddComponent<RunSceneSessionEntryPoint>();
                entryPoint.ConfigureForEditor(new[] { participant });
                definition.ConfigureForEditor(
                    configuredChapterNumber: 1,
                    configuredMaximumProgress: 100f,
                    configuredNormalizedEscalationThresholds: new[]
                    {
                        0f,
                        0.3f,
                        0.6f,
                        0.85f
                    },
                    configuredCollapseDurationSeconds: 120f,
                    configuredInstabilityStackIntervalSeconds: 20f,
                    configuredBossHealthBonusPerStack: 0.1f,
                    configuredBossDamageBonusPerStack: 0.05f);

                RunChapterFlowRuntime runtime =
                    runtimeObject.AddComponent<RunChapterFlowRuntime>();
                runtime.ConfigureForEditor(definition, entryPoint);
                runtime.EnsureInitialized();
                Assert(runtime.SessionEntryPoint == entryPoint &&
                       runtime.RunId.StartsWith(
                           "run:direct:",
                           StringComparison.Ordinal),
                    "Runtime did not initialize its scene boundary.");
                Assert(runtime.TryResolveParticipantId(
                           participantObject,
                           out string participantId) &&
                       participantId == "player:validation",
                    "Runtime could not resolve a registered participant.");

                Assert(runtime.TryFillChapterProgressForDebug(
                           out RunChapterProgressApplicationResult fillResult) &&
                       fillResult.AcceptedContributionCount == 1 &&
                       fillResult.ParticipantId == "player:validation" &&
                       runtime.State.IsProgressFull &&
                       runtime.State.Phase == RunChapterPhase.RiftCollapse,
                    "Debug progress command did not begin Rift Collapse " +
                    "through the application boundary.");
                RunChapterBossPortalInteractable portal =
                    portalObject.AddComponent<
                        RunChapterBossPortalInteractable>();
                portal.Initialize(runtime, chapterNumber: 1);
                RunChapterBossTransitionApplicationResult observed = default;
                int publicationCount = 0;
                runtime.BossTransitionRequested += _ => publicationCount++;
                portal.EntryResolved += result => observed = result;
                portal.Interact(participantObject);

                Assert(observed.Success &&
                       observed.HasTransitionRequest &&
                       !observed.TransitionRequest.WasForced &&
                       publicationCount == 1 &&
                       runtime.TransitionApplication.HasTransitionRequest,
                    "Portal did not publish one stored manual transition.");
                Assert(!portal.IsInteractable,
                    "Portal remained interactable after transition.");

                Debug.Log(
                    "Run Chapter Boss Portal Runtime validation passed.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(portalObject);
                UnityEngine.Object.DestroyImmediate(runtimeObject);
                UnityEngine.Object.DestroyImmediate(entryObject);
                UnityEngine.Object.DestroyImmediate(participantObject);
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
