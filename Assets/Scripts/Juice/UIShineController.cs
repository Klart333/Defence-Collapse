using Plugins.Animate_UI_Materials;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using System;
using DG.Tweening;

namespace Juice
{
    [RequireComponent(typeof(GraphicPropertyOverrideRange))]
    public class UIShineController : MonoBehaviour
    {
        private static readonly int CycleTime = Shader.PropertyToID("_CycleTime");

        [Title("Settings")]
        [SerializeField]
        private float waveSpeed = 5;
        
        [SerializeField]
        private Ease easeType = Ease.Linear;
        
        private GraphicPropertyOverrideRange materialOverride;
        private Tween tween; 
        
        private bool shining;
        
        private void Awake()
        {
            materialOverride = GetComponent<GraphicPropertyOverrideRange>();
        }

        private void OnDisable()
        {
            shining = false;

            if (tween != null)
            {
                tween.Complete();
                tween = null;                
            }
        }

        public void Shine()
        {
            if (shining) return;
            
            shining = true;
            
            float duration = 1.0f / waveSpeed;
            tween = DOTween.To(x => materialOverride.PropertyValue = x, 0.0f, 1.0f, duration).SetEase(easeType);
            tween.OnComplete(() => shining = false);
        }
    }
}
