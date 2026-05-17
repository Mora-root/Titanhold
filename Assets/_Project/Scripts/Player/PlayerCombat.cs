using System.Collections;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private PlayerConfig config;

    private Animator animator;

    private float lastAttackTime;

    public float AttackEnterRange => config.AttackRange;
    public float AttackExitRange => config.AttackRange + 0.3f;

    public float AttackCooldown => config.AttackCooldown;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }

    public bool CanAttack()
    {
        return Time.time >= lastAttackTime + AttackCooldown;
    }

    public void TryAttack(Enemy target)
    {
        if (!CanAttack()) return;

        Attack(target);
    }

    private void Attack(Enemy target)
    {
        lastAttackTime = Time.time;

        animator?.SetTrigger("Attack");

        // 👉 момент урона (упрощённо сразу)
        DealDamage(target);
    }

    private void DealDamage(Enemy target)
    {
        if (target == null || !target.IsTargetable) return;

        target.TakeDamage(config.Damage);
    }
}
