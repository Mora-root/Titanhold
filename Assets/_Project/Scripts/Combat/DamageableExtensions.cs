namespace Titanhold.Combat
{
    public static class DamageableExtensions
    {
        public static DamageResult ApplyDamageRequest(
            this global::IDamageable damageable,
            DamageRequest request)
        {
            if (damageable == null)
                return DamageResult.Rejected(request, DamageRejectionReason.MissingTarget);

            if (!request.ExecutionId.IsValid)
                return DamageResult.Rejected(request, DamageRejectionReason.InvalidExecutionId);

            if (request.RawDamage <= 0f ||
                float.IsNaN(request.RawDamage) ||
                float.IsInfinity(request.RawDamage))
            {
                return DamageResult.Rejected(request, DamageRejectionReason.InvalidAmount);
            }

            if (damageable is IContextualDamageable contextualDamageable)
                return contextualDamageable.ApplyDamage(request);

            damageable.TakeDamage(request.RawDamage);
            return DamageResult.LegacyFallback(request);
        }
    }
}
