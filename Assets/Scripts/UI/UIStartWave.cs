using UnityEngine.UI;
using UnityEngine;
using Gameplay;

public class UIStartWave : MonoBehaviour
{
    [SerializeField]
    private Button waveButton;

    private bool inWave;
    
    private void OnEnable()
    {
        Events.OnWaveEnded += OnWaveEnded;
    }

    private void OnDisable()
    {
        Events.OnWaveEnded -= OnWaveEnded;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            Events.OnWaveEnded?.Invoke();
        }
    }

    private void OnWaveEnded()
    {
        SetInteractable(true);
        PersistantGameStats.CurrentPersistantGameStats.WaveCount++;

        inWave = false;
    }

    private void SetInteractable(bool value)
    {
        waveButton.interactable = value;
    }
    
    public void StartWave()
    {
        if (inWave) 
        {
            return;
        }

        Events.OnWaveStarted?.Invoke();
        
        inWave = true;
        SetInteractable(false);
    }
}
