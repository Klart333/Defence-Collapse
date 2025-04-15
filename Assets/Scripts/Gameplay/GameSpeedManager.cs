using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay
{
    public class GameSpeedManager : Singleton<GameSpeedManager>, IGameSpeed
    {
        [Title("Game Speed")]
        [SerializeField]
        private float speedySpeed = 4.0f;
        
        public float Value { get; private set; } = 1;

        private void Update()
        {
            if (Input.GetKey(KeyCode.Space))
            {
                Value = speedySpeed; // Make into effectors or smth
            }
            else
            {
                Value = 1;
            }
        }
    }
}