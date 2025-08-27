using UnityEngine;
using UnityEngine.UI;

namespace Variables
{
    public class ReferenceImage : Image
    {
        [SerializeField]
        private SpriteReference spriteReference;
        
        [SerializeField]
        private ColorReference colorReference;

#if UNITY_EDITOR
        
        protected override void OnValidate()
        {
            base.OnValidate();

            if (!Application.isPlaying && spriteReference != null && colorReference != null)
            {
                sprite = spriteReference.Value;
                color = colorReference.Value;
            }
        }
#endif
    }
}