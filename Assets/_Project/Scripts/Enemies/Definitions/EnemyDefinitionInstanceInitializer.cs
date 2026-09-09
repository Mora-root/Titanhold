using UnityEngine;

namespace Titanhold.Enemies
{
    public static class EnemyDefinitionInstanceInitializer
    {
        public static EnemyDefinitionInitializationResult TryInitialize(
            GameObject enemyObject,
            IEnemyDefinitionResolver resolver)
        {
            if (enemyObject == null)
            {
                return EnemyDefinitionInitializationResult.Failed(
                    EnemyDefinitionInitializationError.MissingEnemyObject);
            }

            EnemyDefinitionBinding binding =
                enemyObject.GetComponentInChildren<EnemyDefinitionBinding>(
                    true);
            if (binding == null)
            {
                return EnemyDefinitionInitializationResult.Failed(
                    EnemyDefinitionInitializationError.MissingBinding);
            }

            return binding.TryInitialize(resolver);
        }
    }
}
