using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    private Animator animator;
    private PlayerCombat combat;
    private PlayerSkillExecutor skillExecutor;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int DeathHash = Animator.StringToHash("Death");

    private void Awake()
    {
        animator = GetComponent<Animator>();
        combat = GetComponentInParent<PlayerCombat>();
        skillExecutor = GetComponentInParent<PlayerSkillExecutor>();
    }

    // Move speed
    public void SetSpeed(float speed)
    {
        animator.SetFloat(SpeedHash, speed);
    }

    // Start attack
    public void PlayAttack()
    {
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
        combat.OnAttackFinished();
    }

    public void PlaySkill(string triggerName)
    {
        animator.SetTrigger(triggerName);
    }

    public void OnSkillHit()
    {
        skillExecutor.ApplyCurrentSkill();
    }

    public void OnSkillFinished()
    {
        skillExecutor.FinishCurrentSkill();
    }

    // Get damage
    public void PlayHit()
    {
        animator.SetTrigger(HitHash);
    }

    // Die
    public void PlayDeath()
    {
        animator.SetTrigger(DeathHash);
    }
}
