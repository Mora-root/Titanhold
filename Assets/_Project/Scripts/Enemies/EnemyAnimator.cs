using UnityEngine;

public class EnemyAnimator : MonoBehaviour
{
    private Animator animator;
    private EnemyCombat combat;

    public bool IsAttacking { get; private set; }

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int DeathHash = Animator.StringToHash("Death");

    private void Awake()
    {
        animator = GetComponent<Animator>();
        combat = GetComponentInParent<EnemyCombat>();
    }

    public void SetSpeed(float speed)
    {
        animator.SetFloat(SpeedHash, speed);
    }

    public void PlayAttack()
    {
        IsAttacking = true;
        animator.SetTrigger(AttackHash);
    }

    public void OnAttackHit()
    {
        combat.DoDamage();
    }

    public void OnAttackFinished()
    {
        IsAttacking = false;
    }

    public void PlayHit()
    {
        animator.SetTrigger(HitHash);
    }

    public void PlayDeath()
    {
        animator.SetTrigger(DeathHash);
    }
}
