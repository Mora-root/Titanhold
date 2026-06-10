using TMPro;
using UnityEngine;

namespace Titanhold.UI.Common
{
    public sealed class GoldAmountView : MonoBehaviour
    {
        [SerializeField] private PlayerGold playerGold;
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private string prefix = "Gold: ";

        private void OnEnable()
        {
            if (playerGold == null)
                playerGold = GetComponentInParent<PlayerGold>();

            if (playerGold != null)
                playerGold.OnChanged += HandleGoldChanged;

            Refresh();
        }

        private void OnDisable()
        {
            if (playerGold != null)
                playerGold.OnChanged -= HandleGoldChanged;
        }

        public void SetPlayerGold(PlayerGold gold)
        {
            if (ReferenceEquals(playerGold, gold))
            {
                Refresh();
                return;
            }

            if (isActiveAndEnabled && playerGold != null)
                playerGold.OnChanged -= HandleGoldChanged;

            playerGold = gold;

            if (isActiveAndEnabled && playerGold != null)
                playerGold.OnChanged += HandleGoldChanged;

            Refresh();
        }

        public void Refresh()
        {
            if (amountText == null)
            {
                Debug.LogWarning($"{nameof(GoldAmountView)} on '{name}' has no amount text assigned.", this);
                return;
            }

            if (playerGold == null)
            {
                Debug.LogWarning($"{nameof(GoldAmountView)} on '{name}' has no PlayerGold assigned or found in parent.", this);
                amountText.text = $"{prefix}Missing";
                return;
            }

            amountText.text = $"{prefix}{playerGold.Amount}";
        }

        private void HandleGoldChanged(int amount)
        {
            Refresh();
        }
    }
}
