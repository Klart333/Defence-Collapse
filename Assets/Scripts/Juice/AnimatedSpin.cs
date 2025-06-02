using DG.Tweening;
using UnityEngine;

namespace Juice
{
    public class AnimatedSpin : MonoBehaviour
    {
        [SerializeField]
        private float totalDegrees = 360;

        [SerializeField]
        private float duration = 0.5f;
        
        [SerializeField]
        private Ease ease = Ease.Linear;

        public void Spin()
        {
            transform.DOKill();
            transform.DOLocalRotate(new Vector3(0, 0, totalDegrees), duration).SetEase(ease);
        } 
    }
}