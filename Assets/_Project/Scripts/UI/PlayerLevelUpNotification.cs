using System.Collections;
using TMPro;
using UnityEngine;

public sealed class PlayerLevelUpNotification : MonoBehaviour
{
    [SerializeField] private PlayerExperience playerExperience;
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private float visibleDuration = 2f;

    private Coroutine hideCoroutine;

    private void Awake()
    {
        playerExperience ??= FindAnyObjectByType<PlayerExperience>();

        if (root != null)
            root.SetActive(false);
    }

    private void OnEnable()
    {
        if (playerExperience != null)
            playerExperience.OnLevelChanged += HandleLevelChanged;
    }

    private void OnDisable()
    {
        if (playerExperience != null)
            playerExperience.OnLevelChanged -= HandleLevelChanged;

        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);

        hideCoroutine = null;
    }

    private void HandleLevelChanged(int currentLevel)
    {
        if (messageText != null)
            messageText.text = $"Level Up! Lv {currentLevel}";

        if (root != null)
            root.SetActive(true);

        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);

        hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        if (visibleDuration > 0f)
            yield return new WaitForSeconds(visibleDuration);

        if (root != null)
            root.SetActive(false);

        hideCoroutine = null;
    }
}
