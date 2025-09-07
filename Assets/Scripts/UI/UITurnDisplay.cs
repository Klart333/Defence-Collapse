using Gameplay.Event;
using UnityEngine;
using TMPro;

namespace UI
{
    public class UITurnDisplay : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI turnCountText;
        
        private void OnEnable()
        {
            Events.OnTurnIncreased += OnTurnIncreased;
        }

        private void OnDisable()
        {
            Events.OnTurnIncreased -= OnTurnIncreased;
        }

        private void OnTurnIncreased(int increase, int total)
        {
            turnCountText.text = total.ToString("N0");
        }
    }
}