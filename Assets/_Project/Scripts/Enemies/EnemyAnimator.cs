using UnityEngine;

public class EnemyAnimator : MonoBehaviour
{
    private Animator animator;
    private EnemyCombat combat;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int DeathHash = Animator.StringToHash("Death");

    private void Awake()
    {
        animator = GetComponent<Animator>();
        combat = GetComponentInParent<EnemyCombat>();
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
