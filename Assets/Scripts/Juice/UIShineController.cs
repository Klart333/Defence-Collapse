using Plugins.Animate_UI_Materials;
using Sirenix.OdinInspector;
using UnityEngine;
using DG.Tweening;
using Gameplay;

namespace Juice
{
    [RequireComponent(typeof(GraphicPropertyOverrideRange))]
    public class UIShineController : MonoBehaviour
    {
        [Title("Settings")]
        [SerializeField]
        private float waveSpeed = 5;
        
        [SerializeField]
        private Ease easeType = Ease.Linear;
        
        [Title("Additional Settings")]
        [SerializeField]
        private bool useGameSpeed = false;
        
        private GraphicPropertyOverrideRange materialOverride;
        private Tween tween; 
        
        private IGameSpeed gameSpeed;
        
        private bool shining;
        
        private void Awake()
        {
            materialOverride = GetComponent<GraphicPropertyOverrideRange>();
            gameSpeed = GameSpeedManager.Instance;
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
            
            if (!useGameSpeed)
            {
                tween.timeScale = 1.0f / gameSpeed.Value;
            }
        }
    }
}
