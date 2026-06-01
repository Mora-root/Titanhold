using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CampCrystalUIController : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text threatText;
    [SerializeField] private TMP_Text pendingText;
    [SerializeField] private TMP_Text waveStateText;
    [SerializeField] private TMP_Text brokenText;
    [SerializeField] private TMP_Text campCoreHealthText;
    [SerializeField] private Button startWaveButton;
    [SerializeField] private Button restoreCampButton;
    [SerializeField] private Button closeButton;

    [SerializeField] private ThreatMeter threatMeter;
    [SerializeField] private ThreatPendingState pendingState;
    [SerializeField] private CampDefenseWaveController waveController;
    [SerializeField] private CampBrokenState brokenState;
    [SerializeField] private CampCore campCore;

    private void Awake()
    {
        if (root == null)
            root = gameObject;

        threatMeter ??= FindAnyObjectByType<ThreatMeter>();
        pendingState ??= FindAnyObjectByType<ThreatPendingState>();
        waveController ??= FindAnyObjectByType<CampDefenseWaveController>();
        brokenState ??= FindAnyObjectByType<CampBrokenState>();
        campCore ??= FindAnyObjectByType<CampCore>();

        root.SetActive(false);
    }

    private void OnEnable()
    {
        if (startWaveButton != null)
            startWaveButton.onClick.AddListener(HandleStartWaveClicked);

        if (restoreCampButton != null)
            restoreCampButton.onClick.AddListener(HandleRestoreCampClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    private void OnDisable()
    {
        if (startWaveButton != null)
            startWaveButton.onClick.RemoveListener(HandleStartWaveClicked);

        if (restoreCampButton != null)
            restoreCampButton.onClick.RemoveListener(HandleRestoreCampClicked);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);
    }

    public void Open()
    {
        root.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        root.SetActive(false);
    }

    public void Refresh()
    {
        if (threatText != null)
            threatText.text = GetThreatText();

        if (pendingText != null)
            pendingText.text = GetPendingText();

        if (waveStateText != null)
            waveStateText.text = GetWaveStateText();

        if (brokenText != null)
            brokenText.text = GetBrokenText();

        if (campCoreHealthText != null)
            campCoreHealthText.text = GetCampCoreHealthText();

        bool canStartWave = waveController != null && waveController.IsPending;
        if (startWaveButton != null)
        {
            startWaveButton.gameObject.SetActive(canStartWave);
            startWaveButton.interactable = canStartWave;
        }

        bool canRestoreCamp = brokenState != null && brokenState.IsBroken;
        if (restoreCampButton != null)
        {
            restoreCampButton.gameObject.SetActive(canRestoreCamp);
            restoreCampButton.interactable = canRestoreCamp;
        }
    }

    private void HandleStartWaveClicked()
    {
        waveController?.StartWave();
        Refresh();
    }

    private void HandleRestoreCampClicked()
    {
        brokenState?.RestoreCamp();
        Refresh();
    }

    private string GetThreatText()
    {
        if (threatMeter == null)
            return "Threat: Missing";

        return $"Threat: {threatMeter.CurrentThreat:0.#} / {threatMeter.MaxThreat:0.#}";
    }

    private string GetPendingText()
    {
        if (pendingState == null)
            return "Pending Wave: Missing";

        return $"Pending Wave: {pendingState.IsPending}";
    }

    private string GetWaveStateText()
    {
        if (waveController == null)
            return "Wave State: Missing";

        return $"Wave State: {waveController.State}";
    }

    private string GetBrokenText()
    {
        if (brokenState == null)
            return "Camp Broken: Missing";

        return $"Camp Broken: {brokenState.IsBroken}";
    }

    private string GetCampCoreHealthText()
    {
        if (campCore == null || campCore.Health == null)
            return "Camp Core Health: Missing";

        return $"Camp Core Health: {campCore.Health.CurrentHealth:0.#} / {campCore.Health.MaxHealth:0.#}";
    }
}
