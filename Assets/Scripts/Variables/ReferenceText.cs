using TMPro;
using UnityEngine;

namespace Variables
{
    public class ReferenceText : TextMeshProUGUI
    {
        [SerializeField]
        private FontReference fontReference;
        
        [SerializeField]
        private ColorReference colorReference;

#if UNITY_EDITOR
        
        protected override void OnValidate()
        {
            base.OnValidate();

            if (!Application.isPlaying && fontReference != null && colorReference != null)
            {
                color = colorReference.Value;
                font = fontReference.Value;
            }
        }
#endif
    }
}
