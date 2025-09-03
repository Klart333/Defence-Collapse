using UnityEngine;

namespace Gameplay.Turns
{
    public class TurnHandler : MonoBehaviour
    {
        public int Turn { get; set; }

        private void OnEnable()
        {
            Events.OnTurnIncreased += OnTurnIncreased;
        }

        private void OnDisable()
        {
            Events.OnTurnIncreased -= OnTurnIncreased;
        }

        private void OnTurnIncreased(int turn)
        {
            Turn = turn;
        }
    }
}