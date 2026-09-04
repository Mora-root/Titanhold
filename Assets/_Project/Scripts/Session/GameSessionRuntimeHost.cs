using UnityEngine;
using Titanhold.Run;

namespace Titanhold.Session
{
    [DisallowMultipleComponent]
    public sealed class GameSessionRuntimeHost : MonoBehaviour
    {
        [SerializeField] private ItemDefinitionCatalog itemDefinitions;
        [SerializeField] private RunProgressionDefinition runProgression;

        private static GameSessionRuntimeHost activeHost;

        public GameSessionRuntime Runtime { get; private set; }
        public bool IsInitialized => Runtime != null;
        public ItemDefinitionCatalog ItemDefinitions => itemDefinitions;
        public RunProgressionDefinition RunProgression => runProgression;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            ItemDefinitionCatalog definitions,
            RunProgressionDefinition progression)
        {
            itemDefinitions = definitions;
            runProgression = progression;
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            activeHost = null;
        }

        private void Awake()
        {
            if (!Application.isPlaying)
                return;

            if (activeHost != null && activeHost != this)
            {
                Destroy(gameObject);
                return;
            }

            if (transform.parent != null)
            {
                Debug.LogError(
                    $"{nameof(GameSessionRuntimeHost)} must be placed on a root GameObject.",
                    this);
                enabled = false;
                return;
            }

            if (itemDefinitions == null)
            {
                Debug.LogError(
                    $"{nameof(GameSessionRuntimeHost)} requires an item definition catalog.",
                    this);
                enabled = false;
                return;
            }

            if (!itemDefinitions.IsValid)
            {
                Debug.LogError(itemDefinitions.ValidationError, itemDefinitions);
                enabled = false;
                return;
            }

            if (runProgression == null || !runProgression.IsValid)
            {
                Debug.LogError(
                    $"{nameof(GameSessionRuntimeHost)} requires a valid run progression definition.",
                    this);
                enabled = false;
                return;
            }

            Runtime = new GameSessionRuntime(
                itemDefinitions,
                runExperienceCurve: runProgression.BuildCurve());
            activeHost = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (activeHost == this)
                activeHost = null;
        }
    }
}
