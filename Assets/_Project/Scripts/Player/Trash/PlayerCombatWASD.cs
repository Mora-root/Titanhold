using UnityEngine;
using System.Collections;

public class PlayerCombatWASD : MonoBehaviour
{
    [SerializeField] private PlayerConfig playerConfig;
    [SerializeField] private GameObject weaponHitBox;

    private Animator animator;
    private PlayerMovementWASD movement;
    private PlayerInputWASD input;

    private bool isAttacking;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        movement = GetComponent<PlayerMovementWASD>();
        input = GetComponent<PlayerInputWASD>();
        if (weaponHitBox != null) weaponHitBox.SetActive(false);
    }

    private void Update()
    {
        if (isAttacking) return;
        if (input != null && input.AttackPressed)
        {
            StartCoroutine(AttackCoroutine());
        }
    }

    private IEnumerator AttackCoroutine()
    {
        isAttacking = true;
        if (movement != null) movement.BlockMovement(true);

        animator?.SetTrigger("Attack");
        if (weaponHitBox != null)
            weaponHitBox.GetComponent<WeaponHitBox>()?.EnableHitBox();

        yield return new WaitForSeconds(playerConfig.AttackDuration);

        if (weaponHitBox != null)
            weaponHitBox.GetComponent<WeaponHitBox>()?.DisableHitBox();

        if (movement != null) movement.BlockMovement(false);
        isAttacking = false;
    }
    private void OnDisable()
    {
        StopAllCoroutines();
        isAttacking = false;
        if (weaponHitBox != null)
            weaponHitBox.SetActive(false);
        if (movement != null)
            movement.BlockMovement(false);
    }
}
