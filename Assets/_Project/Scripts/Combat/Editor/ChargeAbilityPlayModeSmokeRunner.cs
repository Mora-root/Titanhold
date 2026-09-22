using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Titanhold.Combat.Abilities;
using Titanhold.Combat.Effects;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class ChargeAbilityPlayModeSmokeRunner
{
    private const string PendingKey =
        "Titanhold.ChargeAbility.PlayModeSmokePending";
    private const string ChargePath =
        "Assets/_Project/ScriptableObjects/Abilities/Charge.asset";
    private static IEnumerator routine;
    private static double deadline;
    private static string stage;

    [InitializeOnLoadMethod]
    private static void Initialize()
    {
#pragma warning disable UDR0001
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
#pragma warning restore UDR0001
    }

    [MenuItem("Tools/Titanhold/Run Charge Ability Play Mode Smoke Test")]
    public static void Start()
    {
        Scene scene = SceneManager.GetActiveScene();
        Require(
            !EditorApplication.isPlayingOrWillChangePlaymode &&
            !scene.isDirty &&
            scene.path == "Assets/_Project/Scenes/SampleScene.unity",
            "Open the saved SampleScene outside Play Mode before running the Charge smoke test.");
        SessionState.SetBool(PendingKey, true);
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode &&
            SessionState.GetBool(PendingKey, false))
        {
            routine = Run();
            stage = "scene initialization";
            Application.runInBackground = true;
            EditorApplication.isPaused = false;
            deadline = EditorApplication.timeSinceStartup + 30d;
#pragma warning disable UDR0001
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
#pragma warning restore UDR0001
        }
        else if (state == PlayModeStateChange.ExitingPlayMode &&
                 (routine != null ||
                  SessionState.GetBool(PendingKey, false)))
        {
            Stop();
        }
    }

    private static void Tick()
    {
        try
        {
            Require(
                EditorApplication.timeSinceStartup < deadline,
                $"Charge smoke test timed out during {stage}; " +
                $"time={Time.timeAsDouble}, scale={Time.timeScale}.");
            EditorApplication.QueuePlayerLoopUpdate();
            if (routine != null && routine.MoveNext())
                return;

            Debug.Log(
                "Charge ability Play Mode smoke test passed: target commit, " +
                "rapid movement, cooldown, post-action and timed speed buff.");
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"Charge ability Play Mode smoke test failed: {exception}");
        }

        Stop();
        EditorApplication.isPlaying = false;
    }

    private static void Stop()
    {
        (routine as IDisposable)?.Dispose();
        routine = null;
        Time.timeScale = 1f;
        SessionState.SetBool(PendingKey, false);
        EditorApplication.update -= Tick;
    }

    private static IEnumerator Run()
    {
        yield return null;
        PlayerBrain brain = Object.FindAnyObjectByType<PlayerBrain>();
        PlayerAbilityExecutor executor = brain != null
            ? brain.GetComponent<PlayerAbilityExecutor>()
            : null;
        PlayerResource resource = brain != null
            ? brain.GetComponent<PlayerResource>()
            : null;
        CharacterStats stats = brain != null
            ? brain.GetComponent<CharacterStats>()
            : null;
        TimedStackingStatEffectReceiver receiver = brain != null
            ? brain.GetComponent<TimedStackingStatEffectReceiver>()
            : null;
        Require(
            brain != null && executor != null && resource != null &&
            stats != null && receiver != null &&
            ReferenceEquals(brain.Skills, executor),
            "Scene player is missing Charge runtime dependencies.");

        TargetedMovementAbilityDefinition charge =
            AssetDatabase.LoadAssetAtPath<
                TargetedMovementAbilityDefinition>(ChargePath);
        Require(charge != null, "Charge definition is missing.");
        Require(AbilityDefinitionRegistry.TryCreate(
                new IAbilityDefinition[] { charge },
                out AbilityDefinitionRegistry definitions,
                out string registryError),
            $"Charge registry is invalid: {registryError}");
        Require(executor.TryBindAbilitySlots(
                new SingleSlotSource(charge.AbilityId),
                definitions),
            "Charge could not replace the temporary smoke-test loadout.");

        brain.Input.SetGameplayInputEnabled(false);
        brain.ClearQueuedAction();
        brain.ClearAllSelections();
        brain.Combat.CancelAttack();
        brain.Stop();
        resource.enabled = false;
        resource.RestoreFull();
        float resourceBefore = resource.CurrentResource;
        float moveSpeedBefore = stats.GetValue(StatType.MoveSpeed);
        Vector3 startPosition = brain.transform.position;
        Vector3 targetPosition = FindReachableTarget(startPosition);
        GameObject targetObject = CreateTarget(targetPosition);
        EnemyTarget target = targetObject.GetComponent<EnemyTarget>();
        Vector3 facing = targetPosition - startPosition;
        facing.y = 0f;
        brain.transform.rotation = Quaternion.LookRotation(facing);
        Physics.SyncTransforms();

        try
        {
            stage = "commit and movement";
            double committedAt = Time.timeAsDouble;
            Require(ExecuteCommand(brain, target),
                "Charge command was rejected.");
            Require(
                executor.IsUsingSkill &&
                brain.StateMachine.CurrentState == brain.SkillState &&
                Mathf.Approximately(
                    resource.CurrentResource,
                    resourceBefore - 20f),
                "Charge did not commit its resource and skill state together.");

            float furthestTravel = 0f;
            while (executor.IsUsingSkill)
            {
                furthestTravel = Mathf.Max(
                    furthestTravel,
                    Vector3.Distance(
                        startPosition,
                        brain.transform.position));
                yield return null;
            }

            float targetDistance = Vector3.Distance(
                brain.transform.position,
                targetPosition);
            Require(
                furthestTravel >= 2f && targetDistance <= 2.25f,
                $"Charge did not move rapidly toward its target " +
                $"(travel={furthestTravel}, remaining={targetDistance}).");
            Require(
                stats.GetValue(StatType.MoveSpeed) >=
                    moveSpeedBefore * 1.199f &&
                receiver.ActiveEffectCount == 1,
                "Charge did not apply its three-second speed buff.");
            Require(
                ReferenceEquals(brain.ActionTarget, target) &&
                (brain.StateMachine.CurrentState == brain.ApproachState ||
                 brain.StateMachine.CurrentState == brain.AttackState),
                "Charge did not continue combat against its primary target.");

            float resourceAfterCommit = resource.CurrentResource;
            Require(!ExecuteCommand(brain, target) &&
                    Mathf.Approximately(
                        resource.CurrentResource,
                        resourceAfterCommit),
                "Charge cooldown rejection spent resource or restarted movement.");

            brain.Combat.CancelAttack();
            brain.ClearAllSelections();
            brain.ChangeToIdle();
            stage = "speed buff expiry";
            while (Time.timeAsDouble < committedAt + 3.6d)
                yield return null;
            Require(
                receiver.ActiveEffectCount == 0 &&
                Mathf.Approximately(
                    stats.GetValue(StatType.MoveSpeed),
                    moveSpeedBefore),
                "Charge speed buff did not expire cleanly.");
        }
        finally
        {
            Object.Destroy(targetObject);
        }
    }

    private static Vector3 FindReachableTarget(Vector3 origin)
    {
        Vector3[] directions =
        {
            Vector3.right,
            Vector3.forward,
            Vector3.left,
            Vector3.back
        };
        for (int i = 0; i < directions.Length; i++)
        {
            Vector3 candidate = origin + directions[i] * 6f;
            if (NavMesh.SamplePosition(
                    candidate,
                    out NavMeshHit hit,
                    2f,
                    NavMesh.AllAreas) &&
                Vector3.Distance(origin, hit.position) >= 4f)
            {
                return hit.position;
            }
        }

        throw new InvalidOperationException(
            "No reachable Charge target point was found near the player.");
    }

    private static GameObject CreateTarget(Vector3 position)
    {
        GameObject target = new("ChargeSmoke_Target");
        target.layer = 6;
        target.transform.position = position;
        CharacterStats stats = target.AddComponent<CharacterStats>();
        stats.Block.SetBaseValue(StatType.MaxHealth, 10000f);
        Health health = target.AddComponent<Health>();
        health.RestoreFull();
        target.AddComponent<EnemyTarget>();
        target.AddComponent<SphereCollider>().isTrigger = true;
        return target;
    }

    private static bool ExecuteCommand(
        PlayerBrain brain,
        ITargetable target)
    {
        MethodInfo method = typeof(PlayerBrain).GetMethod(
            "TryExecuteSkill",
            BindingFlags.Instance | BindingFlags.NonPublic);
        return (bool)method.Invoke(
            brain,
            new object[] { new PlayerSkillCommand(0, target) });
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private sealed class SingleSlotSource : IAbilitySlotSource
    {
        private readonly string abilityId;

        public SingleSlotSource(string abilityId)
        {
            this.abilityId = abilityId;
        }

        public int SlotCount => 1;

        public bool TryGetAbilitySlot(
            int slotIndex,
            out string resolvedAbilityId)
        {
            resolvedAbilityId = slotIndex == 0
                ? abilityId
                : string.Empty;
            return slotIndex == 0;
        }
    }
}
