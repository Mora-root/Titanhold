namespace Titanhold.Combat
{
    public readonly struct DamageTargetResolution
    {
        public DamageTargetResolution(global::IDamageable target, DamageResult result)
        {
            Target = target;
            Result = result;
        }

        public global::IDamageable Target { get; }
        public DamageResult Result { get; }
    }
}
