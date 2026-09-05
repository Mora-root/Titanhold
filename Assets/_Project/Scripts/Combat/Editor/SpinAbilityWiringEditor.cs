using System;
using Titanhold.Combat.Abilities;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class SpinAbilityWiringEditor
{
    public const string PlayerPath = "Assets/_Project/Prefabs/Player.prefab";
    public const string DefinitionPath = "Assets/_Project/ScriptableObjects/Configs/SpinAbility.asset";

    [MenuItem("Tools/Titanhold/Install Spin Ability Wiring")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Install Spin wiring outside Play Mode.");

        GameObject player = PrefabUtility.LoadPrefabContents(PlayerPath);
        try
        {
            PlayerBrain brain = player.GetComponent<PlayerBrain>();
            PlayerSkillExecutor legacy = player.GetComponent<PlayerSkillExecutor>();
            Require(brain != null && legacy != null, "Player prefab is missing its existing skill components.");

            AreaDamageAbilityDefinition definition =
                AssetDatabase.LoadAssetAtPath<AreaDamageAbilityDefinition>(DefinitionPath);
            if (definition == null)
            {
                Require(AssetDatabase.LoadMainAssetAtPath(DefinitionPath) == null,
                    "Spin ability path is occupied by another asset.");
                definition = ScriptableObject.CreateInstance<AreaDamageAbilityDefinition>();
                SerializedObject data = new(definition);
                data.FindProperty("abilityId").stringValue = "ability:spin";
                data.FindProperty("resourceCost").floatValue = 20f;
                data.FindProperty("cooldown").floatValue = 3f;
                data.FindProperty("windUp").floatValue = 0.23333333f;
                data.FindProperty("recovery").floatValue = 0.30000003f;
                data.FindProperty("damageMultiplier").floatValue = 1.5f;
                data.FindProperty("radius").floatValue = 2.5f;
                data.FindProperty("targetMask").intValue = 64;
                data.FindProperty("animatorTrigger").stringValue = "Spin";
                data.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(definition, DefinitionPath);
            }

            PlayerAbilityExecutor executor = player.GetComponent<PlayerAbilityExecutor>();
            if (executor == null) executor = player.AddComponent<PlayerAbilityExecutor>();
            SerializedObject executorData = new(executor);
            executorData.FindProperty("skill1").objectReferenceValue = definition;
            executorData.ApplyModifiedPropertiesWithoutUndo();
            SerializedObject brainData = new(brain);
            brainData.FindProperty("skillExecutorOverride").objectReferenceValue = executor;
            brainData.ApplyModifiedPropertiesWithoutUndo();
            executor.enabled = true;
            legacy.enabled = false;
            ValidatePlayer(player, definition);
            PrefabUtility.SaveAsPrefabAsset(player, PlayerPath, out bool success);
            Require(success, "Could not save the player prefab.");
            AssetDatabase.SaveAssetIfDirty(definition);
            Debug.Log("Spin ability wiring installed; legacy component and SkillData reference retained.");
        }
        finally { PrefabUtility.UnloadPrefabContents(player); }
    }

    [MenuItem("Tools/Titanhold/Validate Spin Ability Wiring")]
    public static void Validate()
    {
        GameObject player = PrefabUtility.LoadPrefabContents(PlayerPath);
        try
        {
            ValidatePlayer(player, AssetDatabase.LoadAssetAtPath<AreaDamageAbilityDefinition>(DefinitionPath));
            Debug.Log("Spin ability wiring validation passed.");
        }
        finally { PrefabUtility.UnloadPrefabContents(player); }
    }

    private static void ValidatePlayer(GameObject player, AreaDamageAbilityDefinition definition)
    {
        Require(definition != null && definition.TryCreateSnapshot(20f, out _), "Spin definition is invalid.");
        definition.TryCreateSnapshot(20f, out var snapshot);
        Require(snapshot.Execution.AbilityId == "ability:spin" && snapshot.Execution.ResourceCost == 20f &&
                snapshot.Execution.Cooldown == 3d && snapshot.Damage == 30f && snapshot.Radius == 2.5f &&
                snapshot.TargetMask == 64 && snapshot.AnimatorTrigger == "Spin",
            "Spin balance or stable identity differs from the approved migration.");
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
            "Assets/_Project/Art/Animations/PlayerSpinSkill.anim");
        Require(clip != null && Math.Abs(snapshot.Execution.WindUp + snapshot.Execution.Recovery - clip.length) < 0.001d,
            "Spin lifecycle no longer matches the animation duration.");
        PlayerAbilityExecutor executor = player.GetComponent<PlayerAbilityExecutor>();
        PlayerSkillExecutor legacy = player.GetComponent<PlayerSkillExecutor>();
        Require(executor != null && executor.enabled && legacy != null && !legacy.enabled,
            "Exactly one skill executor must be enabled.");
        Require(new SerializedObject(executor).FindProperty("skill1").objectReferenceValue == definition &&
                new SerializedObject(legacy).FindProperty("skill1").objectReferenceValue != null,
            "New definition is not connected, or the legacy reference was removed.");
        Require(ReferenceEquals(PlayerSkillCommands.Resolve(player), executor),
            "Player brain and combat adapters do not select the replacement executor.");
        Require(player.GetComponent<PlayerResource>() != null && player.GetComponent<CharacterStats>() != null &&
                player.GetComponent<Health>() != null, "Player ability dependencies are missing.");
        Animator animator = player.GetComponentInChildren<Animator>(true);
        AnimatorController controller = animator != null ? animator.runtimeAnimatorController as AnimatorController : null;
        Require(controller != null && Array.Exists(controller.parameters,
            parameter => parameter.name == "Spin" && parameter.type == AnimatorControllerParameterType.Trigger),
            "Player animator has no Spin trigger.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
