using TMPro;
using UnityEngine;

public sealed class PlayerGoldHUD : MonoBehaviour
{
    [SerializeField] private PlayerGold playerGold;
    [SerializeField] private TMP_Text goldText;

    private void Awake()
    {
        playerGold ??= FindAnyObjectByType<PlayerGold>();
    }

    private void OnEnable()
    {
        if (playerGold != null)
            playerGold.OnChanged += HandleGoldChanged;

        Refresh();
    }

    private void OnDisable()
    {
        if (playerGold != null)
            playerGold.OnChanged -= HandleGoldChanged;
    }

    public void Refresh()
    {
        if (goldText == null)
            return;

        if (playerGold != null)
        {
            goldText.text = $"Gold: {playerGold.Amount}";
        }
        else
        {
            goldText.text = "Gold: Missing";
        }
    }

    private void HandleGoldChanged(int amount)
    {
        Refresh();
    }
}
