using UnityEngine;

[CreateAssetMenu(fileName = "SkillData", menuName = "Skills/SkillData")]
public class SkillData : ScriptableObject
{
    [Header("Info")]
    public string SkillName;
    public Sprite Icon;

    [Header("Cost")]
    public float ResourceCost = 20f;
    public float Cooldown = 3f;

    [Header("Damage")]
    public float DamageMultiplier = 1.5f;
    public float Radius = 2.5f;
    public LayerMask TargetMask;

    [Header("Animation")]
    public string AnimatorTrigger = "Spin";
}
