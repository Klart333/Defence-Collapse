using Cysharp.Threading.Tasks;
using Buildings.District;
using UnityEngine.UI;
using UnityEngine;
using System;

namespace UI
{
    public class UIDistrictFlipButton : MonoBehaviour
    {
        private static readonly int LockedSequence = Animator.StringToHash("LockedSequence");
        private static readonly int Interactable = Animator.StringToHash("Interactable");
        private static readonly int Flip = Animator.StringToHash("Flip");
        
        public event Action OnClick;
        
        [SerializeField]
        private Button districtButton;
        
        [SerializeField]
        private Animator animator;

        [SerializeField]
        private AnimationClip unFlipAnimation;
        
        private DistrictPlacer districtPlacer;
        
        private bool isPlacing;

        private void OnEnable()
        {
            districtPlacer = FindFirstObjectByType<DistrictPlacer>();
            districtPlacer.OnPlacingCanceled += OnPlacingCanceled;
        }
        
        private void OnDisable()
        {
            districtPlacer.OnPlacingCanceled -= OnPlacingCanceled;
        }

        public void OnButtonClicked()
        {
            if (!isPlacing)
            {
                animator.SetBool(Interactable, false);
                animator.SetBool(LockedSequence, true);
                animator.SetTrigger(Flip);
                OnClick?.Invoke();
                isPlacing = true;
            }
            else
            {
                isPlacing = false;
                UIEvents.OnFocusChanged?.Invoke();
                
                animator.SetBool(Interactable, true);
                UnlockAfterDelay().Forget();
            }
        }

        private void OnPlacingCanceled()
        {
            if (!isPlacing) return;
            
            isPlacing = false;
            animator.SetBool(Interactable, true); 
            UnlockAfterDelay().Forget();
        }
        
        private async UniTaskVoid UnlockAfterDelay()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(unFlipAnimation.length));
            animator.SetBool(LockedSequence, false);
        }
    }
}