using System;

namespace Titanhold.Enemies
{
    public enum EnemyDefinitionInitializationError
    {
        None,
        InvalidEnemyId,
        MissingResolver,
        DefinitionNotFound,
        MissingEnemyObject,
        MissingBinding,
        MissingReceiver,
        BaseStatsApplicationFailed
    }

    public readonly struct EnemyDefinitionInitializationResult
    {
        private EnemyDefinitionInitializationResult(
            bool success,
            EnemyDefinitionInitializationError error,
            string enemyId,
            EnemyBaseStatsApplicationError applicationError)
        {
            Success = success;
            Error = error;
            EnemyId = enemyId ?? string.Empty;
            ApplicationError = applicationError;
        }

        public bool Success { get; }
        public EnemyDefinitionInitializationError Error { get; }
        public string EnemyId { get; }
        public EnemyBaseStatsApplicationError ApplicationError { get; }

        public static EnemyDefinitionInitializationResult Succeeded(
            string enemyId)
        {
            return new EnemyDefinitionInitializationResult(
                true,
                EnemyDefinitionInitializationError.None,
                enemyId,
                EnemyBaseStatsApplicationError.None);
        }

        public static EnemyDefinitionInitializationResult Failed(
            EnemyDefinitionInitializationError error,
            string enemyId = "",
            EnemyBaseStatsApplicationError applicationError =
                EnemyBaseStatsApplicationError.None)
        {
            return new EnemyDefinitionInitializationResult(
                false,
                error,
                enemyId,
                applicationError);
        }
    }

    public sealed class EnemyDefinitionInitializationService
    {
        public EnemyDefinitionInitializationResult TryInitialize(
            string enemyId,
            IEnemyDefinitionResolver resolver,
            IEnemyArchetypeReceiver receiver)
        {
            if (!HasStrictId(enemyId))
            {
                return EnemyDefinitionInitializationResult.Failed(
                    EnemyDefinitionInitializationError.InvalidEnemyId);
            }

            if (resolver == null)
            {
                return EnemyDefinitionInitializationResult.Failed(
                    EnemyDefinitionInitializationError.MissingResolver,
                    enemyId);
            }

            if (!resolver.TryResolve(
                    enemyId,
                    out EnemyArchetype archetype) ||
                archetype == null)
            {
                return EnemyDefinitionInitializationResult.Failed(
                    EnemyDefinitionInitializationError.DefinitionNotFound,
                    enemyId);
            }

            if (receiver == null)
            {
                return EnemyDefinitionInitializationResult.Failed(
                    EnemyDefinitionInitializationError.MissingReceiver,
                    enemyId);
            }

            EnemyBaseStatsApplicationResult applicationResult =
                receiver.TryApply(archetype);
            if (!applicationResult.Success)
            {
                return EnemyDefinitionInitializationResult.Failed(
                    EnemyDefinitionInitializationError.BaseStatsApplicationFailed,
                    enemyId,
                    applicationResult.Error);
            }

            return EnemyDefinitionInitializationResult.Succeeded(
                archetype.EnemyId);
        }

        private static bool HasStrictId(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   string.Equals(
                       value,
                       value.Trim(),
                       StringComparison.Ordinal);
        }
    }
}
