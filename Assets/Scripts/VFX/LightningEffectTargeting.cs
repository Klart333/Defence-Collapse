using Unity.Mathematics;
using UnityEngine.VFX;
using UnityEngine;

namespace VFX
{
    public class LightningEffectTargeting : MonoBehaviour, IVisualEffectTarget
    {
        [SerializeField]
        private VisualEffect visualEffect;

        public void SetTarget(Vector3 originPosition, Vector3 targetPosition)
        {
            visualEffect.SetVector3("Size", new float3(math.distance(originPosition, targetPosition) * 2, 1, 1));
        }
    }
}