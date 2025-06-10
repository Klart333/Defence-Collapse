using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using Gameplay;

namespace Utility
{
    public class ContinousSpin : MonoBehaviour
    {
        [Title("Settings")]
        [SerializeField]
        private Vector3 spinAxis = Vector3.up;
        
        [SerializeField]
        private Space space = Space.World;

        [SerializeField]
        private float spinSpeed = 1;

        [SerializeField]
        private bool useGameSpeed = true;
        
        private IGameSpeed gameSpeed;

        private void OnEnable()
        {
            GetGameSpeed().Forget();
        }

        private async UniTaskVoid GetGameSpeed()
        {
            gameSpeed = await GameSpeedManager.Get();
        }

        private void Update()
        {
            float spinAmount = Time.deltaTime * spinSpeed;
            if (useGameSpeed) spinAmount *= gameSpeed.Value;
            
            transform.Rotate(spinAxis, spinAmount, Space.World);
        }
    }
}