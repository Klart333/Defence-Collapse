using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay
{
    public class GameSpeedManager : Singleton<GameSpeedManager>, IGameSpeed
    {
        [Title("Game Speed")]
        [SerializeField]
        private float speedySpeed = 4.0f;
        
        public float GameSpeed { get; private set; } = 1;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                GameSpeed = speedySpeed; // Make into effectors or smth
            }
            else
            {
                GameSpeed = 1;
            }
        }
    }
}