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
        
        [SerializeField]
        private VisualEffect poisonParticlesEffect;
        
        [SerializeField]
        private VisualEffect chainLightningEffect;
        
        [SerializeField]
        private VisualEffect lightningTrailEffect;
        
        private void Awake()
        {
            VFXReferences.PoisonParticleGraph = poisonParticlesEffect;
            VFXReferences.ChainLightningGraph = chainLightningEffect;
            VFXReferences.FireParticleGraph = fireParticleEffect;
            VFXReferences.ExplosionsGraph = explosionEffect;
            VFXReferences.TrailGraph = trailEffect;
            VFXReferences.LightningTrailGraph = lightningTrailEffect;
        }
    }
}
