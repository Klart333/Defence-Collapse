using Sirenix.OdinInspector;
using DG.Tweening;
using UnityEngine;

namespace Juice
{
    public class AnimatedPopup : MonoBehaviour
    {
        [Title("Settings")]
        [SerializeField]
        private Ease easeType = Ease.Linear;
        
        [SerializeField, MinMaxRange(0, 5)]
        private RangedFloat lifetime = new RangedFloat(0, 5);

        [SerializeField]
        private bool readScale = false;
        
        private void Start()
        {
            Vector3 endValue = readScale ? transform.localScale : Vector3.one;
            
            transform.localScale = Vector3.zero;
            transform.DOScale(endValue, lifetime.Random()).SetEase(easeType);
        }

        private void OnDisable()
        {
            transform.DOComplete();
        }
    }
}