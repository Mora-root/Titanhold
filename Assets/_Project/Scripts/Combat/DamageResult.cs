namespace Titanhold.Combat
{
    public enum DamageResolutionStatus
    {
        Rejected,
        Applied,
        LegacyFallback
    }

    public enum DamageRejectionReason
    {
        None,
        MissingTarget,
        TargetAlreadyDead,
        InvalidExecutionId,
        InvalidAmount,
        FullyMitigated
    }

    public readonly struct DamageResult
    {
        private DamageResult(
            DamageResolutionStatus status,
            DamageRejectionReason rejectionReason,
            DamageRequest request,
            float healthBefore,
            float healthAfter,
            float appliedDamage,
            bool killed,
            DeathContext deathContext)
        {
            Status = status;
            RejectionReason = rejectionReason;
            Request = request;
            HealthBefore = healthBefore;
            HealthAfter = healthAfter;
            AppliedDamage = appliedDamage;
            Killed = killed;
            DeathContext = deathContext;
        }

        public DamageResolutionStatus Status { get; }
        public DamageRejectionReason RejectionReason { get; }
        public DamageRequest Request { get; }
        public float HealthBefore { get; }
        public float HealthAfter { get; }
        public float AppliedDamage { get; }
        public bool Killed { get; }
        public DeathContext DeathContext { get; }
        public bool WasApplied => Status == DamageResolutionStatus.Applied ||
                                  Status == DamageResolutionStatus.LegacyFallback;
        public bool HasDetailedResult => Status == DamageResolutionStatus.Applied;
        public bool HasDeathContext => Killed && DeathContext.IsValid;

        public static DamageResult Applied(
            DamageRequest request,
            float healthBefore,
            float healthAfter,
            float appliedDamage,
            bool killed,
            DeathContext deathContext)
        {
            return new DamageResult(
                DamageResolutionStatus.Applied,
                DamageRejectionReason.None,
                request,
                healthBefore,
                healthAfter,
                appliedDamage,
                killed,
                deathContext);
        }

        public static DamageResult Rejected(DamageRequest request, DamageRejectionReason reason)
        {
            return new DamageResult(
                DamageResolutionStatus.Rejected,
                reason,
                request,
                0f,
                0f,
                0f,
                false,
                default);
        }

        public static DamageResult LegacyFallback(DamageRequest request)
        {
            return new DamageResult(
                DamageResolutionStatus.LegacyFallback,
                DamageRejectionReason.None,
                request,
                0f,
                0f,
                0f,
                false,
                default);
        }
    }
}
