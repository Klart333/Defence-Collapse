using UnityEngine.UI;
using UnityEngine;

public class UIStartWave : MonoBehaviour
{
    [SerializeField]
    private Button waveButton;
    
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
        waveButton.interactable = true;
    }

    public void StartWave()
    {
        Events.OnWaveStarted?.Invoke();
        
        waveButton.interactable = false;
    }
}
