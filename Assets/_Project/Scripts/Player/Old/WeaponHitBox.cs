using System.Collections.Generic;
using UnityEngine;

public class WeaponHitBox : MonoBehaviour
{
    [SerializeField] private float damage = 25f;

    private HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();

    public void EnableHitBox()
    {
        Debug.Log("HitBox enabled");
        hitTargets.Clear();
        gameObject.SetActive(true);
    }

    public void DisableHitBox()
    {
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"HitBox triggered by {other.name}");

        if (other.TryGetComponent<IDamageable>(out var damageable))
        {
            if (hitTargets.Contains(damageable))
                return;

            damageable.TakeDamage(damage);
            hitTargets.Add(damageable);
        }
    }
}
