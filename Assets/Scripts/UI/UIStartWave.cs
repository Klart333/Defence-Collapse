using UnityEngine.UI;
using UnityEngine;

public class UIStartWave : MonoBehaviour
{
    [SerializeField]
    private Button waveButton;
    
    private bool inWave = false;

    private void OnEnable()
    {
        Events.OnWaveEnded += OnWaveEnded;
    }

    private void OnDisable()
    {
        Events.OnWaveEnded -= OnWaveEnded;
    }

    private void OnWaveEnded()
    {
        inWave = false;
        waveButton.interactable = true;
    }

    public void StartWave()
    {
        Events.OnWaveStarted?.Invoke();
        inWave = true;
        
        waveButton.interactable = false;
    }
}
