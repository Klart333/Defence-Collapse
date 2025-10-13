using CameraShake;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Utility
{
    public class PlayCameraShake : MonoBehaviour
    {
        [Title("Settings")]
        [SerializeField]
        private float strength = 0.3f;

        [SerializeField]
        private float frequency = 25f;
        
        [SerializeField]
        private int bounceAmount = 5;
        
        public void Shake()
        {
            BounceShake.Params pars = new BounceShake.Params
            {
                axesMultiplier = new Displacement(Vector3.zero, new Vector3(1, 1, 0.4f)),
                rotationStrength = strength,
                freq = frequency,
                numBounces = bounceAmount
            };
            CameraShaker.Instance.RegisterShake(new BounceShake(pars));
        }
    }
}