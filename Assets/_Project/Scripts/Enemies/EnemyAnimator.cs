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

    // 🔥 движение
    public void SetSpeed(float speed)
    {
        animator.SetFloat(SpeedHash, speed);
    }

    // 🔥 атака
    public void PlayAttack()
    {
        Debug.Log("Start attack");
        animator.SetTrigger(AttackHash);
    }

    // 🔥 Animation Event (момент удара)
    public void OnAttackHit()
    {
        Debug.Log("Deal damage");
        combat.ApplyDamage();
    }

    // 🔥 Animation Event (конец анимации)
    public void OnAttackFinished()
    {
        Debug.Log("Attack finished");
        combat.OnAttackFinished();
    }

    // 🔥 реакция на урон
    public void PlayHit()
    {
        animator.SetTrigger(HitHash);
    }

    // 🔥 смерть
    public void PlayDeath()
    {
        animator.SetTrigger(DeathHash);
    }
}
