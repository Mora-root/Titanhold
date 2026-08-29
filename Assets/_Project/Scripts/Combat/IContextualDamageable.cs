namespace Titanhold.Combat
{
    public interface IContextualDamageable : global::IDamageable
    {
        DamageResult ApplyDamage(DamageRequest request);
    }
}
