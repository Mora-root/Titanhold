using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    private Animator animator;
    private PlayerCombat combat;
    private PlayerSkillExecutor skillExecutor;
    private float defaultAnimatorSpeed = 1f;
    private bool hasPlaybackOverride;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int HitHash = Animator.StringToHash("TakeDamage");
    private static readonly int DeathHash = Animator.StringToHash("Die");

    private void Awake()
    {
        animator = GetComponent<Animator>();
        defaultAnimatorSpeed = animator != null ? animator.speed : 1f;
        combat = GetComponentInParent<PlayerCombat>();
        skillExecutor = GetComponentInParent<PlayerSkillExecutor>();
    }

    // Move speed
    public void SetSpeed(float speed)
    {
        animator.SetFloat(SpeedHash, speed);
    }

    public void SetLocomotionPlaybackSpeed(float playbackSpeed)
    {
        if (animator == null || hasPlaybackOverride)
            return;

        animator.speed = defaultAnimatorSpeed * Mathf.Max(0.01f, playbackSpeed);
    }

    // Start attack
    public void PlayAttack(float playbackSpeed = 1f)
    {
        SetPlaybackSpeed(playbackSpeed);
        animator.SetTrigger(AttackHash);
    }

    // Deal damage
    public void OnAttackHit()
    {
        combat.ApplyDamage();
    }

    // End animation
    public void OnAttackFinished()
    {
        ResetPlaybackSpeed();
        combat.OnAttackFinished();
    }

    public void PlaySkill(string triggerName)
    {
        ResetPlaybackSpeed();
        animator.SetTrigger(triggerName);
    }

    public bool CanPlaySkill(string triggerName)
    {
        if (animator == null || !animator.isActiveAndEnabled ||
            animator.runtimeAnimatorController == null || string.IsNullOrWhiteSpace(triggerName))
            return false;

        int triggerHash = Animator.StringToHash(triggerName);
        foreach (AnimatorControllerParameter parameter in animator.parameters)
            if (parameter.nameHash == triggerHash && parameter.type == AnimatorControllerParameterType.Trigger)
                return true;
        return false;
    }

    public void OnSkillHit()
    {
        if (skillExecutor != null && skillExecutor.isActiveAndEnabled)
            skillExecutor.ApplyCurrentSkill();
    }

    public void OnSkillFinished()
    {
        if (skillExecutor != null && skillExecutor.isActiveAndEnabled)
            skillExecutor.FinishCurrentSkill();
    }

    // Get damage
    public void PlayHit()
    {
        ResetPlaybackSpeed();
        animator.SetTrigger(HitHash);
    }

    // Die
    public void PlayDeath()
    {
        ResetPlaybackSpeed();
        animator.SetTrigger(DeathHash);
    }

    public void ResetPlaybackSpeed()
    {
        if (animator == null)
            return;

        hasPlaybackOverride = false;
        animator.speed = defaultAnimatorSpeed;
    }

    private void SetPlaybackSpeed(float playbackSpeed)
    {
        if (animator == null)
            return;

        hasPlaybackOverride = true;
        animator.speed = defaultAnimatorSpeed * Mathf.Max(0.01f, playbackSpeed);
    }
}
