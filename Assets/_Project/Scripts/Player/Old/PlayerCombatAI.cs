using System.Collections;
using UnityEngine;

public class PlayerCombatAI : MonoBehaviour
{
    [SerializeField] private PlayerConfig playerConfig;
    [SerializeField] private GameObject weaponHitBox;

    private Animator animator;
    private PlayerMovementAI movement;
    private PlayerInputAI input;
    private WeaponHitBox hitBox;

    private bool isAttacking;
    private Enemy currentAttackTarget;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        movement = GetComponent<PlayerMovementAI>();
        input = GetComponent<PlayerInputAI>();
        if (weaponHitBox != null)
        {
            hitBox = weaponHitBox.GetComponent<WeaponHitBox>();
            weaponHitBox.SetActive(false);
        }
    }

    private void Update()
    {
        if (isAttacking) return;

        Enemy target = input?.TargetEnemy;

        if (target != null && target.IsTargetable)
        {
            float distance = Vector3.Distance(transform.position, target.GetTransform().position);
            if (distance <= playerConfig.AttackRange)
            {
                StartCoroutine(AttackCoroutine(target));
            }
        }
    }

    private IEnumerator AttackCoroutine(Enemy target)
    {
        isAttacking = true;
        movement.IsMovementBlocked = true;

        animator?.SetTrigger("Attack");
        hitBox?.EnableHitBox();

        yield return new WaitForSeconds(playerConfig.AttackDuration);

        hitBox?.DisableHitBox();
        movement.IsMovementBlocked = false;
        isAttacking = false;
    }
}
