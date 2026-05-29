using TMPro;
using UnityEngine;

// Temporary prototype HUD. Do not use in production.
public sealed class CampDefenseDebugHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text outputText;
    [SerializeField] private ThreatMeter threatMeter;
    [SerializeField] private ThreatPendingState pendingState;
    [SerializeField] private CampDefenseWaveController waveController;
    [SerializeField] private CampDefenseResultState resultState;
    [SerializeField] private CampBrokenState brokenState;
    [SerializeField] private CampCore campCore;

    private void Awake()
    {
        threatMeter ??= GetComponent<ThreatMeter>();
        pendingState ??= GetComponent<ThreatPendingState>();
        waveController ??= GetComponent<CampDefenseWaveController>();
        resultState ??= GetComponent<CampDefenseResultState>();
        brokenState ??= GetComponent<CampBrokenState>();
        campCore ??= FindAnyObjectByType<CampCore>();
    }

    private void Update()
    {
        if (outputText == null)
            return;

        outputText.text =
            $"Threat: {GetThreatText()}\n" +
            $"Pending: {GetPendingText()}\n" +
            $"Wave State: {GetWaveStateText()}\n" +
            $"Last Result: {GetLastResultText()}\n" +
            $"Camp Broken: {GetCampBrokenText()}\n" +
            $"CampCore Health: {GetCampCoreHealthText()}";
    }

    private string GetThreatText()
    {
        if (threatMeter == null)
            return "Missing";

        return $"{threatMeter.CurrentThreat:0.#} / {threatMeter.MaxThreat:0.#}";
    }

    private string GetPendingText()
    {
        if (pendingState == null)
            return "Missing";

        return pendingState.IsPending.ToString();
    }

    private string GetWaveStateText()
    {
        if (waveController == null)
            return "Missing";

        return waveController.State.ToString();
    }

    private string GetLastResultText()
    {
        if (resultState == null)
            return "Missing";

        return resultState.LastResult.ToString();
    }

    private string GetCampBrokenText()
    {
        if (brokenState == null)
            return "Missing";

        return brokenState.IsBroken.ToString();
    }

    private string GetCampCoreHealthText()
    {
        if (campCore == null || campCore.Health == null)
            return "Missing";

        return $"{campCore.Health.CurrentHealth:0.#} / {campCore.Health.MaxHealth:0.#}";
    }
}
