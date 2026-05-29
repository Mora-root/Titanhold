using UnityEngine;

// Temporary prototype helper. Replace with altar/interact/UI flow later.
public sealed class CampDefenseDebugStarter : MonoBehaviour
{
    [SerializeField] private CampDefenseWaveController waveController;
    [SerializeField] private KeyCode startKey = KeyCode.B;

    private void Awake()
    {
        waveController ??= GetComponent<CampDefenseWaveController>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(startKey))
        {
            waveController?.StartWave();
        }
    }
}
