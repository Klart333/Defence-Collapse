using UnityEngine.VFX;
using UnityEngine;

namespace VFX
{
    public class VFXReferencesAuthoring : MonoBehaviour
    {
        [SerializeField]
        private VisualEffect explosionEffect;
        
        [SerializeField]
        private VisualEffect trailEffect;

        [SerializeField]
        private VisualEffect fireParticleEffect;
        
        private void Awake()
        {
            VFXReferences.FireParticleGraph = fireParticleEffect;
            VFXReferences.ExplosionsGraph = explosionEffect;
            VFXReferences.TrailGraph = trailEffect;
        }
    }
}
