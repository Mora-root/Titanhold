using TMPro;
using UnityEngine;

public sealed class PlayerCurrencyHUD : MonoBehaviour
{
    [SerializeField] private PlayerCurrency playerCurrency;
    [SerializeField] private TMP_Text currencyText;

    private void Awake()
    {
        playerCurrency ??= FindAnyObjectByType<PlayerCurrency>();
    }

    private void OnEnable()
    {
        if (playerCurrency != null)
            playerCurrency.OnChanged += HandleCurrencyChanged;

        Refresh();
    }

    private void OnDisable()
    {
        if (playerCurrency != null)
            playerCurrency.OnChanged -= HandleCurrencyChanged;
    }

    public void Refresh()
    {
        if (currencyText == null)
            return;

        if (playerCurrency != null)
        {
            currencyText.text = $"Shards: {playerCurrency.Amount}";
        }
        else
        {
            currencyText.text = "Shards: Missing";
        }
    }

    private void HandleCurrencyChanged(int amount)
    {
        Refresh();
    }
}
