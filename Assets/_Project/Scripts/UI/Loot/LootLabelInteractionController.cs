using UnityEngine;

namespace Titanhold.UI.Loot
{
    public sealed class LootLabelInteractionController : MonoBehaviour
    {
        [SerializeField] private LootLabelManager labelManager;
        [SerializeField] private PlayerBrain playerBrain;
        [SerializeField] private bool selectForInspection = true;

        private bool loggedMissingLabelManager;
        private bool loggedMissingPlayerBrain;

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Configure(LootLabelManager labelManager, PlayerBrain playerBrain)
        {
            Unsubscribe();
            this.labelManager = labelManager;
            this.playerBrain = playerBrain;

            if (isActiveAndEnabled)
                Subscribe();
        }

        private void Subscribe()
        {
            if (labelManager == null)
            {
                LogMissingLabelManager();
                return;
            }

            labelManager.LabelClicked -= HandleLabelClicked;
            labelManager.LabelClicked += HandleLabelClicked;
        }

        private void Unsubscribe()
        {
            if (labelManager != null)
                labelManager.LabelClicked -= HandleLabelClicked;
        }

        private void HandleLabelClicked(LootLabelTarget target)
        {
            if (playerBrain == null)
            {
                LogMissingPlayerBrain();
                return;
            }

            if (target == null || !target.IsLabelVisible)
                return;

            LootPickup pickup = target.Pickup;
            if (pickup == null || !pickup.IsSelectable)
                return;

            if (selectForInspection && playerBrain.TargetSelection != null)
                playerBrain.TargetSelection.Select(pickup);

            playerBrain.SetActionSelection(pickup);
        }

        private void LogMissingLabelManager()
        {
            if (loggedMissingLabelManager)
                return;

            Debug.LogWarning($"{nameof(LootLabelInteractionController)} requires a LootLabelManager reference.", this);
            loggedMissingLabelManager = true;
        }

        private void LogMissingPlayerBrain()
        {
            if (loggedMissingPlayerBrain)
                return;

            Debug.LogWarning($"{nameof(LootLabelInteractionController)} requires a PlayerBrain reference.", this);
            loggedMissingPlayerBrain = true;
        }
    }
}
