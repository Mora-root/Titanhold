using System;
using System.Collections;
using System.Reflection;
using Titanhold.Combat;
using Titanhold.Run;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class SpinAbilityPlayModeSmokeRunner
{
    private const string PendingKey = "Titanhold.SpinAbility.PlayModeSmokePending";
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

    [MenuItem("Tools/Titanhold/Run Spin Ability Play Mode Smoke Test")]
    public static void Start()
    {
        Scene scene = SceneManager.GetActiveScene();
        Require(!EditorApplication.isPlayingOrWillChangePlaymode && !scene.isDirty &&
                scene.path == "Assets/_Project/Scenes/SampleScene.unity",
            "Open the saved SampleScene outside Play Mode before running the Spin smoke test.");
        SessionState.SetBool(PendingKey, true);
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(PendingKey, false))
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
                 (routine != null || SessionState.GetBool(PendingKey, false)))
        {
            Stop();
        }
    }

    private static void Tick()
    {
        try
        {
            Require(EditorApplication.timeSinceStartup < deadline,
                $"Spin smoke test timed out during {stage}; time={Time.timeAsDouble}, scale={Time.timeScale}.");
            EditorApplication.QueuePlayerLoopUpdate();
            if (routine != null && routine.MoveNext()) return;
            Debug.Log("Spin ability Play Mode smoke test passed: commit, pause, snapshot damage, one report, run XP, cooldown and death cancellation.");
        }
        catch (Exception exception)
        {
            Debug.LogError($"Spin ability Play Mode smoke test failed: {exception}");
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
        PlayerAbilityExecutor executor = brain != null ? brain.GetComponent<PlayerAbilityExecutor>() : null;
        Require(executor != null && ReferenceEquals(brain.Skills, executor) &&
                !brain.GetComponent<PlayerSkillExecutor>().enabled, "Scene instance did not inherit Spin wiring.");
        PlayerResource resource = brain.GetComponent<PlayerResource>();
        PlayerAnimator animator = brain.GetComponentInChildren<PlayerAnimator>();
        CharacterStats stats = brain.GetComponent<CharacterStats>();
        RunProgressionCombatAdapter progression = Object.FindAnyObjectByType<RunProgressionCombatAdapter>();
        RunProgressionParticipantGateway gateway = brain.GetComponent<RunProgressionParticipantGateway>();
        Require(progression != null && progression.IsInitialized && gateway != null && gateway.IsBound,
            "Run progression was not ready for the Spin test.");
        brain.Input.SetGameplayInputEnabled(false);
        brain.Stop();
        resource.enabled = false;
        resource.RestoreFull();
        float balanceBefore = resource.CurrentResource;
        float expectedDamage = CombatDamageCalculator.GetGlobalDamage(stats) * 1.5f;
        GameObject first = CreateTarget("SpinSmoke_First", brain.transform.position + Vector3.right, 1f, true);
        GameObject second = CreateTarget("SpinSmoke_Second", brain.transform.position - Vector3.right,
            expectedDamage * 4f, false);
        second.AddComponent<BoxCollider>().isTrigger = true;
        Physics.SyncTransforms();
        Health firstHealth = first.GetComponent<Health>();
        Health secondHealth = second.GetComponent<Health>();
        float secondHealthBefore = secondHealth.CurrentHealth;
        int reports = 0;
        int experienceBatches = 0;
        int experienceApplied = 0;
        CombatExecutionReport lastReport = null;
        executor.ExecutionResolved += report => { reports++; lastReport = report; };
        progression.ExperienceAwarded += (playerId, result) =>
        {
            Require(playerId == gateway.PlayerId, "Spin XP went to another participant.");
            experienceBatches++;
            experienceApplied += result.ExperienceApplied;
        };

        try
        {
            double committedAt = Time.timeAsDouble;
            Require(ExecuteCommand(brain), "Spin command was rejected.");
            Require(executor.IsUsingSkill && brain.StateMachine.CurrentState == brain.SkillState &&
                    Mathf.Approximately(resource.CurrentResource, balanceBefore - 20f),
                "Spin did not commit its state and resource cost together.");
            animator.OnSkillHit();
            animator.OnSkillFinished();
            Require(executor.IsUsingSkill && reports == 0 && firstHealth.IsAlive,
                "Legacy animation events released or finished the new ability.");
            stats.Block.SetBaseValue(StatType.Damage, CombatDamageCalculator.GetGlobalDamage(stats) + 100f);
            Time.timeScale = 0f;
            stage = "paused wind-up";
            double pauseEndsAt = EditorApplication.timeSinceStartup + 0.3d;
            while (EditorApplication.timeSinceStartup < pauseEndsAt) yield return null;
            Require(executor.IsUsingSkill && reports == 0 && firstHealth.IsAlive,
                "Paused simulation released Spin.");
            Time.timeScale = 1f;
            stage = "release and recovery";
            while (executor.IsUsingSkill) yield return null;
            Require(reports == 1 && lastReport.ResolutionCount == 2 && !firstHealth.IsAlive &&
                    Mathf.Approximately(secondHealth.CurrentHealth, secondHealthBefore - expectedDamage),
                "Spin did not apply its committed damage exactly once per target.");
            Require(experienceBatches == 1 && experienceApplied == 10,
                "New Spin did not award one attributed run-XP batch.");
            for (int i = 0; i < lastReport.ResolutionCount; i++)
                Require(lastReport[i].Result.Request.AbilityId == "ability:spin" &&
                        lastReport[i].Result.Request.Source == executor.ActorReference,
                    "Spin report lost its stable ability or actor identity.");
            Require(!ExecuteCommand(brain) && Mathf.Approximately(resource.CurrentResource, balanceBefore - 20f),
                "Cooldown rejection spent resource or started another cast.");
            stage = "cooldown";
            while (Time.timeAsDouble < committedAt + 3.1d) yield return null;
            Require(ExecuteCommand(brain), "Spin did not become available after its cooldown.");
            brain.GetComponent<Health>().TakeDamage(float.MaxValue);
            Require(brain.IsDead && !executor.IsUsingSkill && brain.StateMachine.CurrentState == null &&
                    brain.Movement.IsStopped && !brain.HasQueuedSkillCommand,
                "Death left a pending Spin or player action.");
            double checkAt = Time.timeAsDouble + 0.7d;
            stage = "cancelled release after death";
            while (Time.timeAsDouble < checkAt) yield return null;
            Require(reports == 1 && Mathf.Approximately(resource.CurrentResource, balanceBefore - 40f) &&
                    Mathf.Approximately(secondHealth.CurrentHealth, secondHealthBefore - expectedDamage),
                "Death released pending damage or refunded the committed resource cost.");
        }
        finally
        {
            Object.Destroy(first);
            Object.Destroy(second);
        }
    }

    private static bool ExecuteCommand(PlayerBrain brain)
    {
        MethodInfo method = typeof(PlayerBrain).GetMethod("TryExecuteSkill", BindingFlags.Instance | BindingFlags.NonPublic);
        return (bool)method.Invoke(brain, new object[] { new PlayerSkillCommand(0) });
    }

    private static GameObject CreateTarget(string name, Vector3 position, float maxHealth, bool reward)
    {
        GameObject target = new(name);
        target.layer = 6;
        target.transform.position = position;
        CharacterStats stats = target.AddComponent<CharacterStats>();
        stats.Block.SetBaseValue(StatType.MaxHealth, maxHealth);
        Health health = target.AddComponent<Health>();
        health.RestoreFull();
        target.AddComponent<SphereCollider>().isTrigger = true;
        if (reward) target.AddComponent<EnemyRewardSource>().ConfigureForEditor(10);
        return target;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
