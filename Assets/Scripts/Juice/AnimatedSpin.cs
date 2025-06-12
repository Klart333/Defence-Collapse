using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Juice
{
    public class AnimatedSpin : MonoBehaviour
    {
        [SerializeField]
        private Vector3 rotationAxis = Vector3.forward;
        
        [SerializeField]
        private float totalDegrees = 360;

        [SerializeField]
        private float duration = 0.5f;
        
        [SerializeField]
        private Ease ease = Ease.Linear;
        
        [Button]
        public void Spin()
        {
            transform.DOKill(); 
            transform.DOBlendableLocalRotateBy(rotationAxis * totalDegrees, duration, RotateMode.FastBeyond360).SetEase(ease);
        } 
    }
}