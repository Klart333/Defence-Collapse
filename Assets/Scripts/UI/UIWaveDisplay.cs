using System;
using TMPro;
using UnityEngine;

namespace UI
{
    public class UIWaveDisplay : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI waveCountText;
        
        private int waveCount;

        private void OnEnable()
        {
            Events.OnWaveStarted += OnWaveStarted;
        }

        private void OnDisable()
        {
            Events.OnWaveStarted -= OnWaveStarted;
        }

        private void OnWaveStarted()
        {
            waveCount++;
            waveCountText.text = waveCount.ToString("N0");
        }
    }
}