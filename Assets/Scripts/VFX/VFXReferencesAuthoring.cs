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

        private void Awake()
        {
            VFXReferences.ExplosionsGraph = explosionEffect;
            VFXReferences.TrailGraph = trailEffect;
        }
    }
}
